using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TurnTheKeyAdvancedComponentSolver : ComponentSolver
{
	public TurnTheKeyAdvancedComponentSolver(TwitchModule module) :
		base(module)
	{
		_leftKey = (MonoBehaviour) LeftKeyField.GetValue(module.BombComponent.GetComponent(ComponentType));
		_rightKey = (MonoBehaviour) RightKeyField.GetValue(module.BombComponent.GetComponent(ComponentType));
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Turn the left key with !{0} turn left. Turn the right key with !{0} turn right.");

		((KMSelectable) _leftKey).OnInteract = () => HandleKey(LeftBeforeA, LeftAfterA, LeftKeyTurnedField, RightKeyTurnedField, BeforeLeftKeyField, OnLeftKeyTurnMethod, LeftKeyAnimatorField);
		((KMSelectable) _rightKey).OnInteract = () => HandleKey(RightBeforeA, RightAfterA, RightKeyTurnedField, LeftKeyTurnedField, BeforeRightKeyField, OnRightKeyTurnMethod, RightKeyAnimatorField);
	}

	private bool HandleKey(string[] modulesBefore, IEnumerable<string> modulesAfter, FieldInfo keyTurned, FieldInfo otherKeyTurned, FieldInfo beforeKeyField, MethodInfo onKeyTurn, FieldInfo animatorField)
	{
		if (!GetValue(ActivatedField) || GetValue(keyTurned)) return false;
		KMBombInfo bombInfo = Module.BombComponent.GetComponent<KMBombInfo>();
		KMBombModule bombModule = Module.BombComponent.GetComponent<KMBombModule>();
		KMAudio bombAudio = Module.BombComponent.GetComponent<KMAudio>();
		Animator keyAnimator = (Animator) animatorField.GetValue(Module.BombComponent.GetComponent(ComponentType));

		if (TwitchPlaySettings.data.EnforceSolveAllBeforeTurningKeys &&
			modulesAfter.Any(x => bombInfo.GetSolvedModuleNames().Count(x.Equals) != bombInfo.GetSolvableModuleNames().Count(x.Equals)))
		{
			keyAnimator.SetTrigger("WrongTurn");
			bombAudio.PlaySoundAtTransform("WrongKeyTurnFK", Module.transform);
			bombModule.HandleStrike();
			return false;
		}

		beforeKeyField.SetValue(null, TwitchPlaySettings.data.DisableTurnTheKeysSoftLock ? new string[0] : modulesBefore);
		onKeyTurn.Invoke(Module.BombComponent.GetComponent(ComponentType), null);
		if (GetValue(keyTurned))
		{
			//Check to see if any forbidden modules for this key were solved.
			if (TwitchPlaySettings.data.DisableTurnTheKeysSoftLock && bombInfo.GetSolvedModuleNames().Any(modulesBefore.Contains))
				bombModule.HandleStrike(); //If so, Award a strike for it.

			if (!GetValue(otherKeyTurned)) return false;
			int modules = bombInfo.GetSolvedModuleNames().Count(x => RightAfterA.Contains(x) || LeftAfterA.Contains(x));
			TwitchPlaySettings.AddRewardBonus((2 * modules * OtherModes.ScoreMultiplier).RoundToInt());
			IRCConnection.SendMessage($"Reward increased by {modules * 2} for defusing module !{Code} ({bombModule.ModuleDisplayName}).");
		}
		else
		{
			keyAnimator.SetTrigger("WrongTurn");
			bombAudio.PlaySoundAtTransform("WrongKeyTurnFK", Module.transform);
		}
		return false;
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		Component self = Module.BombComponent.GetComponent(ComponentType);
		Animator leftKeyAnimator = (Animator) LeftKeyAnimatorField.GetValue(self);
		Animator rightKeyAnimator = (Animator) RightKeyAnimatorField.GetValue(self);

		LeftKeyTurnedField.SetValue(self, true);
		RightKeyTurnedField.SetValue(self, true);
		rightKeyAnimator.SetBool("IsUnlocked", true);
		Module.BombComponent.GetComponent<KMAudio>().PlaySoundAtTransform("TurnTheKeyFX", Module.transform);
		yield return new WaitForSeconds(0.1f);
		leftKeyAnimator.SetBool("IsUnlocked", true);
		Module.BombComponent.GetComponent<KMAudio>().PlaySoundAtTransform("TurnTheKeyFX", Module.transform);
		yield return new WaitForSeconds(0.1f);
		Module.BombComponent.GetComponent<KMBombModule>().HandlePass();
	}

	private bool GetValue(FieldInfo field) => (bool) field.GetValue(Module.BombComponent.GetComponent(ComponentType));

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length != 2 || commands[0] != "turn")
			yield break;

		MonoBehaviour key;
		switch (commands[1])
		{
			case "l":
			case "left":
				key = _leftKey;
				break;
			case "r":
			case "right":
				key = _rightKey;
				break;
			default:
				yield break;
		}
		yield return "Turning the key";
		yield return DoInteractionClick(key);
	}

	static TurnTheKeyAdvancedComponentSolver()
	{
		AfterLeftKeyField?.SetValue(null, LeftAfterA);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("TurnKeyAdvancedModule");
	private static readonly FieldInfo LeftKeyField = ComponentType.GetField("LeftKey", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo RightKeyField = ComponentType.GetField("RightKey", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo ActivatedField = ComponentType.GetField("bActivated", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo BeforeLeftKeyField = ComponentType.GetField("LeftBeforeA", BindingFlags.NonPublic | BindingFlags.Static);
	private static readonly FieldInfo BeforeRightKeyField = ComponentType.GetField("RightBeforeA", BindingFlags.NonPublic | BindingFlags.Static);
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	private static readonly FieldInfo AfterLeftKeyField = ComponentType.GetField("LeftAfterA", BindingFlags.NonPublic | BindingFlags.Static);
	private static readonly FieldInfo LeftKeyTurnedField = ComponentType.GetField("bLeftKeyTurned", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo RightKeyTurnedField = ComponentType.GetField("bRightKeyTurned", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo RightKeyAnimatorField = ComponentType.GetField("RightKeyAnim", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo LeftKeyAnimatorField = ComponentType.GetField("LeftKeyAnim", BindingFlags.Public | BindingFlags.Instance);

	private static readonly MethodInfo OnLeftKeyTurnMethod = ComponentType.GetMethod("OnLeftKeyTurn", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly MethodInfo OnRightKeyTurnMethod = ComponentType.GetMethod("OnRightKeyTurn", BindingFlags.NonPublic | BindingFlags.Instance);

	private readonly MonoBehaviour _leftKey;
	private readonly MonoBehaviour _rightKey;

	private static readonly string[] LeftAfterA = {
		"Password",
		"Crazy Talk",
		"Who's on First",
		"Keypad",
		"Listening",
		"Orientation Cube"
	};

	private static readonly string[] LeftBeforeA = {
		"Maze",
		"Memory",
		"Complicated Wires",
		"Wire Sequence",
		"Cryptography"
	};

	private static readonly string[] RightAfterA = {
		"Morse Code",
		"Wires",
		"Two Bits",
		"The Button",
		"Colour Flash",
		"Round Keypad"
	};

	private static readonly string[] RightBeforeA = {
		"Semaphore",
		"Combination Lock",
		"Simon Says",
		"Astrology",
		"Switches",
		"Plumbing"
	};
}
