using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class ForgetMeNotComponentSolver : ComponentSolver
{
	public ForgetMeNotComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_buttons = (Array)_buttonsField.GetValue(bombComponent.GetComponent(_componentType));
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), (string)_twitchPlaysHelpField.GetValue(BombComponent.GetComponent(_componentType)));
	}

	protected override bool HandleForcedSolve()
	{
		CoroutineQueue.AddForcedSolve(HandleForcedSolveIEnumerator());
		return true;
	}

	public IEnumerator HandleForcedSolveIEnumerator()
	{
		TextMesh displayMesh = (TextMesh)_stageMeshField.GetValue(BombComponent.GetComponent(_componentType));
		int[] solution = (int[])_solutionField.GetValue(BombComponent.GetComponent(_componentType));
		while (!displayMesh.text.Equals("--"))
			yield return true;

		while (Position < solution.Length)
		{
			MonoBehaviour button = (MonoBehaviour)_buttons.GetValue(solution[Position]);
			yield return DoInteractionClick(button);
		}
	}

	private int Position => (int)_positionField.GetValue(BombComponent.GetComponent(_componentType));


	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (!inputCommand.RegexMatch(out Match match, "^(?:press|submit) ([0-9 ]+)$"))
		{
			yield break;
		}
		IEnumerator processTwitchCommand = (IEnumerator)_twitchPlaysMethod.Invoke(BombComponent.GetComponent(_componentType), new object[] { $"submit {match.Groups[1].Value}" });
		while (processTwitchCommand.MoveNext())
		{
			yield return processTwitchCommand.Current;
		}
	}

	static ForgetMeNotComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("AdvancedMemory");
		_buttonsField = _componentType.GetField("Buttons", BindingFlags.NonPublic | BindingFlags.Instance);
		_solutionField = _componentType.GetField("Solution", BindingFlags.NonPublic | BindingFlags.Instance);
		_stageMeshField = _componentType.GetField("StageMesh", BindingFlags.Public | BindingFlags.Instance);
		_positionField = _componentType.GetField("Position", BindingFlags.NonPublic | BindingFlags.Instance);

		_twitchPlaysMethod = _componentType.GetMethod("ProcessTwitchCommand", BindingFlags.Public | BindingFlags.Instance);
		_twitchPlaysHelpField = _componentType.GetField("TwitchHelpMessage", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentType = null;

	private static FieldInfo _buttonsField = null;
	private static FieldInfo _solutionField = null;
	private static FieldInfo _stageMeshField = null;
	private static FieldInfo _positionField = null;
	private static MethodInfo _twitchPlaysMethod = null;
	private static FieldInfo _twitchPlaysHelpField = null;

	private Array _buttons = null;
}