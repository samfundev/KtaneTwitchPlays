using Assets.Scripts.Props;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class HoldableFactory
{
	public static bool SilentMode = false;
	private static void DebugLog(string format, params object[] args)
	{
		if (SilentMode) return;
		DebugHelper.Log(format, args);
	}

	private delegate HoldableHandler ModHoldableHandlerDelegate(KMHoldableCommander commander, FloatingHoldable holdable);
	private static readonly Dictionary<string, ModHoldableHandlerDelegate> ModHoldableCreators = new Dictionary<string, ModHoldableHandlerDelegate>();
	private static readonly List<Type> ModHoldableTypes = new List<Type>();

	static HoldableFactory()
	{
		ModHoldableCreators[typeof(AlarmClock).FullName] = (commander, holdable) => new AlarmClockHoldableHandler(commander, holdable);
		ModHoldableTypes.Add(typeof(AlarmClock));

		ModHoldableCreators[typeof(IRCConnectionManagerHoldable).FullName] = (commander, holdable) => new IRCConnectionManagerHandler(commander, holdable);
		ModHoldableTypes.Add(typeof(IRCConnectionManagerHoldable));
	}

	public static HoldableHandler CreateHandler(KMHoldableCommander commander, FloatingHoldable holdable)
	{
		if (commander != null)
			commander.ID = holdable.name.ToLowerInvariant().Replace("(clone)", "");

		foreach (Type type in ModHoldableTypes)
		{
			if (type?.FullName == null) continue;
			if (holdable.GetComponent(type) == null || !ModHoldableCreators.ContainsKey(type.FullName)) continue;
			return ModHoldableCreators[type.FullName](commander, holdable);
		}

		return CreateModComponentSolver(commander, holdable);
	}

	private static HoldableHandler CreateModComponentSolver(KMHoldableCommander commander, FloatingHoldable holdable)
	{
		DebugLog("Attempting to find a valid process command method to respond with on holdable {0}...", holdable.name);

		ModHoldableHandlerDelegate modComponentSolverCreator = GenerateModComponentSolverCreator(holdable, out Type holdableType);

		if (holdableType?.FullName == null || modComponentSolverCreator == null)
			return new UnsupportedHoldableHandler(commander, holdable);

		ModHoldableCreators[holdableType.FullName] = modComponentSolverCreator;

		return ModHoldableCreators[holdableType.FullName](commander, holdable);
	}

	private static ModHoldableHandlerDelegate GenerateModComponentSolverCreator(FloatingHoldable holdable, out Type holdableType)
	{
		MethodInfo method = FindProcessCommandMethod(holdable, out ModCommandType commandType, out Type commandComponentType);
		holdableType = commandComponentType;

		if (method != null)
		{
			bool helpResult = FindHelpMessage(holdable, commandComponentType, out string helpText);
			FindCancelBool(holdable, commandComponentType, out FieldInfo cancelField);
			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (commandType)
			{
				case ModCommandType.Simple when helpResult:
					return SimpleHandlerDelegate(method, holdable.GetComponentInChildren(commandComponentType), helpText);
				case ModCommandType.Coroutine when helpResult:
					return CoroutineHandlerDelegate(method, holdable.GetComponentInChildren(commandComponentType), helpText, cancelField);
				default:
					DebugLog("Valid Handler was found, however, no Help message was defined. This help message is mandatory, even if it is just an empty string. Falling back to unsupported Handler solver");
					return null;
			}
		}
		LogAllComponentTypes(holdable);
		DebugLog("No Valid Handler found. Falling back to unsupported Handler solver");
		return null;
	}

	private static ModHoldableHandlerDelegate SimpleHandlerDelegate(MethodInfo method, Component component, string helpText) => (commander, flholdable) => new SimpleHoldableHandler(commander, flholdable, component, method, helpText);

	private static ModHoldableHandlerDelegate CoroutineHandlerDelegate(MethodInfo method, Component component, string helpText, FieldInfo cancelBool) => (commander, flholdable) => new CoroutineHoldableHandler(commander, flholdable, component, method, helpText, cancelBool);

	private static readonly List<string> FullNamesLogged = new List<string>();
	private static void LogAllComponentTypes(MonoBehaviour holdable)
	{
		//If and when there is a potential conflict between multiple assemblies, this will help to find these conflicts so that
		//ReflectionHelper.FindType(fullName, assemblyName) can be used instead.

		Component[] allComponents = holdable.GetComponentsInChildren<Component>(true);
		foreach (Component component in allComponents)
		{
			string fullName = component.GetType().FullName;
			if (FullNamesLogged.Contains(fullName)) continue;
			FullNamesLogged.Add(fullName);

			Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetSafeTypes()).Where(t => t.FullName?.Equals(fullName) ?? false).ToArray();
			if (types.Length < 2)
				continue;

			DebugLog("Found {0} types with fullName = \"{1}\"", types.Length, fullName);
			foreach (Type type in types)
			{
				DebugLog("\ttype.FullName=\"{0}\" type.Assembly.GetName().Name=\"{1}\"", type.FullName, type.Assembly.GetName().Name);
			}
		}
	}

	private static bool FindHelpMessage(MonoBehaviour holdable, Type holdableType, out string helpText)
	{
		FieldInfo candidateString = holdableType.GetField("TwitchHelpMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		if (!(candidateString?.GetValue(holdable.GetComponent(holdableType)) is string))
		{
			helpText = null;
			return false;
		}
		helpText = (string) candidateString.GetValue(holdable.GetComponent(holdableType));
		return true;
	}

	private static bool FindCancelBool(FloatingHoldable holdable, Type holdableType, out FieldInfo cancelField)
	{
		cancelField = holdableType.GetField("TwitchShouldCancelCommand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		return cancelField?.GetValue(holdable.GetComponent(holdableType)) is bool;
	}

	private static MethodInfo FindProcessCommandMethod(MonoBehaviour floadingHoldable, out ModCommandType commandType, out Type commandComponentType)
	{
		Component[] allComponents = floadingHoldable.GetComponentsInChildren<Component>(true);
		foreach (Component component in allComponents)
		{
			Type type = component.GetType();
			MethodInfo candidateMethod = type.GetMethod("ProcessTwitchCommand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (candidateMethod == null)
			{
				continue;
			}

			if (ValidateMethodCommandMethod(type, candidateMethod, out commandType))
			{
				commandComponentType = type;
				return candidateMethod;
			}
		}

		commandType = ModCommandType.Unsupported;
		commandComponentType = null;
		return null;
	}

	private static bool ValidateMethodCommandMethod(Type type, MethodInfo candidateMethod, out ModCommandType commandType)
	{
		commandType = ModCommandType.Unsupported;

		ParameterInfo[] parameters = candidateMethod.GetParameters();
		if (parameters.Length == 0)
		{
			DebugLog("Found a potential candidate ProcessCommand method in {0}, but the parameter list does not match the expected parameter list (too few parameters).", type.FullName);
			return false;
		}

		if (parameters.Length > 1)
		{
			DebugLog("Found a potential candidate ProcessCommand method in {0}, but the parameter list does not match the expected parameter list (too many parameters).", type.FullName);
			return false;
		}

		if (parameters[0].ParameterType != typeof(string))
		{
			DebugLog("Found a potential candidate ProcessCommand method in {0}, but the parameter list does not match the expected parameter list (expected a single string parameter, got a single {1} parameter).", type.FullName, parameters[0].ParameterType.FullName);
			return false;
		}

		if (candidateMethod.ReturnType == typeof(KMSelectable[]))
		{
			DebugLog("Found a valid candidate ProcessCommand method in {0} (using easy/simple API).", type.FullName);
			commandType = ModCommandType.Simple;
			return true;
		}

		if (candidateMethod.ReturnType == typeof(IEnumerator))
		{
			DebugLog("Found a valid candidate ProcessCommand method in {0} (using advanced/coroutine API).", type.FullName);
			commandType = ModCommandType.Coroutine;
			return true;
		}

		return false;
	}
}
