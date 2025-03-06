using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[ModuleID("theSwan")]
public class SwanShim : ComponentSolverShim
{
	public SwanShim(TwitchModule module)
		: base(module)
	{
		_component = Module.BombComponent.GetComponent(ComponentType);
		_keypad = new KMSelectable[] { (KMSelectable) Keypad0ButtonField.GetValue(_component), (KMSelectable) Keypad1ButtonField.GetValue(_component), (KMSelectable) Keypad2ButtonField.GetValue(_component), (KMSelectable) Keypad3ButtonField.GetValue(_component), (KMSelectable) Keypad4ButtonField.GetValue(_component), (KMSelectable) Keypad5ButtonField.GetValue(_component), (KMSelectable) Keypad6ButtonField.GetValue(_component), (KMSelectable) Keypad7ButtonField.GetValue(_component), (KMSelectable) Keypad8ButtonField.GetValue(_component), (KMSelectable) Keypad9ButtonField.GetValue(_component), (KMSelectable) Keypad10ButtonField.GetValue(_component), (KMSelectable) Keypad11ButtonField.GetValue(_component) };
		_execute = (KMSelectable) ExecuteButtonField.GetValue(_component);
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		int resetsPreCommand = _component.GetValue<int>("systemResetCounter");

		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;

		// Award a point upon a successful system reset.
		if (_component.GetValue<int>("systemResetCounter") != resetsPreCommand)
			yield return "awardpoints 1";
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		if (!_component.GetValue<bool>("solved") && _component.GetValue<bool>("executeLock"))
		{
			if (Unshimmed.ForcedSolveMethod == null) yield break;
			var coroutine = (IEnumerator) Unshimmed.ForcedSolveMethod.Invoke(Unshimmed.CommandComponent, null);
			while (coroutine.MoveNext())
				yield return coroutine.Current;
			yield break;
		}
		Module.BombComponent.StartCoroutine(HandleSolve());
		while (!_component.GetValue<bool>("solved") || _component.GetValue<int>("timeUpCounter") <= 8) yield return true;
	}

	IEnumerator HandleSolve()
	{
		reset:
		while (_component.GetValue<bool>("keyboardLock")) yield return null;
		string input = _component.GetValue<string>("computerText").Replace(">:", "").Trim();
		string ans = GetAnswer(_component.GetValue<int>("systemResetCounter"), _component.GetValue<int>("solveRange") <= 0);
		if (input.Length > ans.Length)
		{
			if (Unshimmed.ForcedSolveMethod == null) yield break;
			var coroutine = (IEnumerator) Unshimmed.ForcedSolveMethod.Invoke(Unshimmed.CommandComponent, null);
			while (coroutine.MoveNext())
				yield return coroutine.Current;
			yield break;
		}
		else
		{
			for (int i = 0; i < input.Length; i++)
			{
				if (i == ans.Length)
					break;
				else if (input[i] != ans[i])
				{
					if (Unshimmed.ForcedSolveMethod == null) yield break;
					var coroutine = (IEnumerator) Unshimmed.ForcedSolveMethod.Invoke(Unshimmed.CommandComponent, null);
					while (coroutine.MoveNext())
						yield return coroutine.Current;
					yield break;
				}
			}
		}
		if (ans == "7 7")
		{
			if (input == "77")
				input = "7 7";
		}
		string[] inputSplit = input.Split(' ');
		if (input?.Length == 0)
			inputSplit = new string[0];
		string[] ansSplit = ans.Split(' ');
		List<TextMesh> masterLabels = _component.GetValue<List<TextMesh>>("masterLabels");
		while (_component.GetValue<float>("digit3Time") == 4) yield return null;
		for (int i = inputSplit.Length; i < ansSplit.Length; i++)
		{
			for (int j = 0; j < masterLabels.Count; j++)
			{
				if (masterLabels[j].text == ansSplit[i])
				{
					yield return DoInteractionClick(_keypad[j]);
					break;
				}
			}
		}
		yield return DoInteractionClick(_execute);
		if (!_component.GetValue<bool>("solved"))
		{
			while (!_component.GetValue<bool>("beepReady")) yield return null;
		}
		if (ans == "4 8 15 16 23 42")
			goto reset;
	}

	string GetAnswer(int systemResetCounter, bool ready)
	{
		if (!ready)
		{
			return "4 8 15 16 23 42";
		}
		else if (systemResetCounter.EqualsAny(8, 10, 14, 16, 24))
		{
			return "D H A R M A";
		}
		else if (systemResetCounter.EqualsAny(1, 2, 5, 11, 18, 20))
		{
			return "H A T C H";
		}
		else if (systemResetCounter.EqualsAny(3, 9))
		{
			return "S W N";
		}
		else if (systemResetCounter.EqualsAny(7, 13))
		{
			return "D A R M A";
		}
		else if (systemResetCounter.EqualsAny(0, 4, 6, 12, 17, 23))
		{
			return "S W A N";
		}
		else if (systemResetCounter.EqualsAny(15, 19, 21, 22))
		{
			return "H T C H";
		}
		else
		{
			return "7 7";
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("theSwanScript");
	private static readonly FieldInfo Keypad0ButtonField = ComponentType.GetField("button0", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad1ButtonField = ComponentType.GetField("button1", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad2ButtonField = ComponentType.GetField("button2", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad3ButtonField = ComponentType.GetField("button3", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad4ButtonField = ComponentType.GetField("button4", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad5ButtonField = ComponentType.GetField("button5", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad6ButtonField = ComponentType.GetField("button6", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad7ButtonField = ComponentType.GetField("button7", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad8ButtonField = ComponentType.GetField("button8", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad9ButtonField = ComponentType.GetField("button9", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad10ButtonField = ComponentType.GetField("button10", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo Keypad11ButtonField = ComponentType.GetField("button11", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo ExecuteButtonField = ComponentType.GetField("execute", BindingFlags.Public | BindingFlags.Instance);
	private readonly object _component;
	private readonly KMSelectable[] _keypad;
	private readonly KMSelectable _execute;
}
