using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace TwitchPlaysAssembly.ComponentSolvers.Modded.Shims
{
	public abstract class ComponentSolverShim : ComponentSolver
	{
		protected ComponentSolverShim(BombCommander bombCommander, BombComponent bombComponent) : base(bombCommander, bombComponent)
		{
			string modType = GetModuleType();
			if (!SolverShims.TryGetValue(modType, out ShimData))
			{
				ShimData = new ShimData();
				ShimData.ProcessMethod = ComponentSolverFactory.FindProcessCommandMethod(bombComponent, out ShimData.ModCommandType, out ShimData.ComponentType);
				if (ShimData.ComponentType == null) throw new NotSupportedException(string.Format("Currently {0} is not supported by 'Twitch Plays'.", bombComponent.GetModuleDisplayName()));
				ShimData.ForcedSolveMethod = ComponentSolverFactory.FindSolveMethod(ShimData.ComponentType);
				ShimData.TryCancelField = ComponentSolverFactory.FindCancelBool(ShimData.ComponentType);
				ShimData.ZenModeField = ComponentSolverFactory.FindZenModeBool(ShimData.ComponentType);
				ShimData.HelpMessageFound = ComponentSolverFactory.FindHelpMessage(bombComponent, ShimData.ComponentType, out ShimData.HelpMessage);
				ComponentSolverFactory.FindManualCode(bombComponent, ShimData.ComponentType, out ShimData.ManualCode);
				SolverShims[modType] = ShimData;
			}

			ProcessMethod = ShimData.ProcessMethod;
			CommandComponent = bombComponent.GetComponent(ShimData.ComponentType);
			ForcedSolveMethod = ShimData.ForcedSolveMethod;
			TryCancelField = ShimData.TryCancelField;
			ZenModeField = ShimData.ZenModeField;
			ZenMode = OtherModes.ZenModeOn;
			modInfo = !ShimData.HelpMessageFound
				? ComponentSolverFactory.GetModuleInfo(modType)
				: ComponentSolverFactory.GetModuleInfo(modType, ShimData.HelpMessage, ShimData.ManualCode);
		}

		protected override IEnumerator RespondToCommandInternal(string inputCommand)
		{
			string exception = null;
			switch (ShimData.ModCommandType)
			{
				case ModCommandType.Simple:
					KMSelectable[] selectables = null;
					try
					{
						selectables = (KMSelectable[]) ProcessMethod.Invoke(CommandComponent, new object[] {inputCommand});
					}
					catch (FormatException ex)
					{
						DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invocation will not continue.", ProcessMethod?.DeclaringType?.FullName, ProcessMethod?.Name));
						exception = ex.Message;
					}
					catch (Exception ex)
					{
						DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invocation will not continue.", ProcessMethod?.DeclaringType?.FullName, ProcessMethod?.Name));
						throw;
					}
					if (selectables != null && selectables.Length > 0)
					{
						yield return "modsimple";
						yield return "trycancelsequence";
						yield return selectables;
					}
					break;
				case ModCommandType.Coroutine:
					IEnumerator handler;
					bool result;
					try
					{
						handler = (IEnumerator) ProcessMethod.Invoke(CommandComponent, new object[] {inputCommand});
						result = handler != null;
					}
					catch (Exception ex)
					{
						DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", ProcessMethod?.DeclaringType?.FullName, ProcessMethod?.Name));
						yield break;
					}

					while (result)
					{
						try
						{
							result = handler.MoveNext();
						}
						catch (FormatException ex)
						{
							DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invocation will not continue.", ProcessMethod?.DeclaringType?.FullName, ProcessMethod.Name));
							result = false;
							exception = ex.Message;
						}
						catch (Exception ex)
						{
							DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invocation will not continue.", ProcessMethod?.DeclaringType?.FullName, ProcessMethod.Name));
							throw;
						}
						if (result)
							yield return handler.Current;
					}
					break;
			}
			if (exception != null)
				yield return $"sendtochaterror {exception}";
		}

		protected ShimData ShimData;
		private static readonly Dictionary<string, ShimData> SolverShims = new Dictionary<string, ShimData>();
	}

	public class ShimData
	{
		public Type ComponentType;
		public MethodInfo ProcessMethod;
		public ModCommandType ModCommandType;
		public MethodInfo ForcedSolveMethod;
		public FieldInfo TryCancelField;
		public FieldInfo ZenModeField;
		public bool HelpMessageFound;
		public string HelpMessage;
		public string ManualCode;
		public bool StatusLightLeft;
		public bool StatusLightDown;
	}
}
