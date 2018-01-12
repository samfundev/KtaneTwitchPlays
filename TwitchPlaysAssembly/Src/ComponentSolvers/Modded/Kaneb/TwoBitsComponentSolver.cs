using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TwoBitsComponentSolver : ComponentSolver
{
    private Component c;

    protected enum State
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

    public TwoBitsComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        c = bombComponent.GetComponent(_componentSolverType);

        _submit = (MonoBehaviour)_submitButtonField.GetValue(c);
        _query = (MonoBehaviour)_queryButtonField.GetValue(c);
        _buttons = (MonoBehaviour[])_buttonsField.GetValue(c);
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
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
                        _state = (State)_stateField.GetValue(c);
                        if (_state == State.ShowingError || _state == State.Inactive)
                            yield break;
                    }
                    break;
            }
            yield return new WaitForSeconds(0.1f);
        }

        _state = (State)_stateField.GetValue(c);
        if (_state == State.SubmittingResult)
        {
            string correctresponse = ((string)_calculateCorrectSubmissionMethod.Invoke(c, null)).ToLowerInvariant();
            string currentQuery = ((string) _getCurrentQueryStringMethod.Invoke(c, null)).ToLowerInvariant();
            yield return correctresponse.Equals(currentQuery) ? "solve" : "strike";
        }
    }

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
    private State _state;
}
