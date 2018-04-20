using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TwoBitsComponentSolver : ComponentSolver
{
	private Component c;

	protected enum TwoBitsState
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

	public TwoBitsComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		c = bombComponent.GetComponent(_componentSolverType);

		_submit = (MonoBehaviour)_submitButtonField.GetValue(c);
		_query = (MonoBehaviour)_queryButtonField.GetValue(c);
		_buttons = (MonoBehaviour[])_buttonsField.GetValue(c);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Query the answer with !{0} press K T query. Submit the answer with !{0} press G Z submit.");
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var split = inputCommand.ToLowerInvariant().Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);

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

	private string CorrectResponse => ((string)_calculateCorrectSubmissionMethod.Invoke(c, null)).ToLowerInvariant();
	private string CurrentQuery => ((string)_getCurrentQueryStringMethod.Invoke(c, null)).ToLowerInvariant();
	private TwoBitsState State => (TwoBitsState) _stateField.GetValue(c);

	static TwoBitsComponentSolver()
	{
		_componentSolverType = ReflectionHelper.FindType("TwoBitsModule");
		_submitButtonField = _componentSolverType.GetField("SubmitButton", BindingFlags.Public | BindingFlags.Instance);
		_queryButtonField = _componentSolverType.GetField("QueryButton", BindingFlags.Public | BindingFlags.Instance);
		_buttonsField = _componentSolverType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);
		_stateField = _componentSolverType.GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance);
		_calculateCorrectSubmissionMethod = _componentSolverType.GetMethod("CalculateCorrectSubmission",
			BindingFlags.NonPublic | BindingFlags.Instance);
		_getCurrentQueryStringMethod = _componentSolverType.GetMethod("GetCurrentQueryString",
			BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentSolverType = null;
	private static FieldInfo _submitButtonField = null;
	private static FieldInfo _queryButtonField = null;
	private static FieldInfo _buttonsField = null;
	private static MethodInfo _calculateCorrectSubmissionMethod = null;
	private static MethodInfo _getCurrentQueryStringMethod = null;
	private static FieldInfo _stateField = null;

	private MonoBehaviour[] _buttons = null;
	private MonoBehaviour _query = null;
	private MonoBehaviour _submit = null;
	
}
