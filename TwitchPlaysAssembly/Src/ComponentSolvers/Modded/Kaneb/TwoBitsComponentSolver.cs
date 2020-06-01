using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TwoBitsComponentSolver : ComponentSolver
{
	private readonly Component _c;

	[SuppressMessage("ReSharper", "UnusedMember.Local")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	private enum TwoBitsState
	{
		Inactive,
		Idle,
		Working,
		ShowingResult,
		ShowingError,
		SubmittingResult,
		IncorrectSubmission,
		Complete
	}

	private const string ButtonLabels = "bcdegkptvz";

	public TwoBitsComponentSolver(TwitchModule module) :
		base(module)
	{
		_c = module.BombComponent.GetComponent(ComponentSolverType);

		_submit = (MonoBehaviour) SubmitButtonField.GetValue(_c);
		_query = (MonoBehaviour) QueryButtonField.GetValue(_c);
		_buttons = (MonoBehaviour[]) ButtonsField.GetValue(_c);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Query the answer with !{0} press K T query. Submit the answer with !{0} press G Z submit.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var split = inputCommand.ToLowerInvariant().Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		if (split.Length < 2 || split[0] != "press")
			yield break;

		foreach (string x in split.Skip(1))
		{
			switch (x)
			{
				case "query":
				case "submit":
					break;
				default:
					foreach (char y in x)
						if (!ButtonLabels.Contains(y))
							yield break;
					break;
			}
		}

		yield return "Two Bits Solve Attempt";
		foreach (string x in split.Skip(1))
		{
			switch (x)
			{
				case "query":
					yield return DoInteractionClick(_query);
					break;
				case "submit":
					yield return DoInteractionClick(_submit);
					break;
				default:
					foreach (char y in x)
					{
						yield return DoInteractionClick(_buttons[ButtonLabels.IndexOf(y)]);
						if (State == TwoBitsState.ShowingError || State == TwoBitsState.Inactive)
							yield break;
					}
					break;
			}
			yield return new WaitForSeconds(0.1f);
		}

		if (State == TwoBitsState.SubmittingResult)
		{
			yield return CorrectResponse.Equals(CurrentQuery) ? "solve" : "strike";
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		while (State != TwoBitsState.Complete)
		{
			if (CurrentQuery.StartsWith("_") && State == TwoBitsState.Idle) yield return DoInteractionClick(_buttons[ButtonLabels.IndexOf(CorrectResponse.Substring(0, 1), StringComparison.Ordinal)]);
			if (CurrentQuery.EndsWith("_") && State == TwoBitsState.Idle) yield return DoInteractionClick(_buttons[ButtonLabels.IndexOf(CorrectResponse.Substring(1, 1), StringComparison.Ordinal)]);
			if (State == TwoBitsState.Idle) yield return CorrectResponse.Equals(CurrentQuery) ? DoInteractionClick(_submit) : DoInteractionClick(_query);

			yield return true;
		}
	}

	private string CorrectResponse => ((string) CalculateCorrectSubmissionMethod.Invoke(_c, null)).ToLowerInvariant();
	private string CurrentQuery => ((string) GetCurrentQueryStringMethod.Invoke(_c, null)).ToLowerInvariant();
	private TwoBitsState State => (TwoBitsState) StateField.GetValue(_c);

	private static readonly Type ComponentSolverType = ReflectionHelper.FindType("TwoBitsModule");
	private static readonly FieldInfo SubmitButtonField = ComponentSolverType.GetField("SubmitButton", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo QueryButtonField = ComponentSolverType.GetField("QueryButton", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo ButtonsField = ComponentSolverType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);
	private static readonly MethodInfo CalculateCorrectSubmissionMethod = ComponentSolverType.GetMethod("CalculateCorrectSubmission", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly MethodInfo GetCurrentQueryStringMethod = ComponentSolverType.GetMethod("GetCurrentQueryString", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo StateField = ComponentSolverType.GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance);

	private readonly MonoBehaviour[] _buttons;
	private readonly MonoBehaviour _query;
	private readonly MonoBehaviour _submit;
}
