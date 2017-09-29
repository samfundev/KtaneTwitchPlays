using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class EnglishTestComponentSolver : ComponentSolver
{
	public EnglishTestComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
	    selectButton = findChildGameObjectByName(bombComponent.gameObject, "Left Button").GetComponent<KMSelectable>();
	    submitButton = findChildGameObjectByName(bombComponent.gameObject, "Submit Button").GetComponent<KMSelectable>();
        _englishTestCompoent = bombComponent.GetComponent(_componentType);
	    _questionComponent = bombComponent.GetComponent("Question");
	    Type questionType = _questionComponent.GetType();
	    _answerField = questionType.GetField("answers", BindingFlags.NonPublic | BindingFlags.Instance);
	    _bombComponent = bombComponent;
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
	    yield return null;
        object _question = _currentQuestionField.GetValue(_englishTestCompoent);
	    if (_question == null)
	    {
	        yield return "sendtochaterror I am not ready for your answer yet.";
	        yield break;
	    }
	    List<string> Answers = (List<string>)_answerField.GetValue(_question);

	    int answerIndex = Answers.FindIndex(x => x.Equals(inputCommand, StringComparison.InvariantCultureIgnoreCase));
	    if (answerIndex < 0)
	    {;
	        yield return "sendtochaterror The answer you gave is not one of the choices. The choices are " + string.Join(", ", Answers.ToArray());
	        yield break;
	    }
	    while ((int)_indexField.GetValue(_englishTestCompoent) != answerIndex)
	    {
	        yield return DoInteractionClick(selectButton);
	    }
	    yield return DoInteractionClick(submitButton);
	}

    private GameObject findChildGameObjectByName(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.name == name)
                return child.gameObject;
            GameObject childGo = findChildGameObjectByName(child.gameObject, name);
            if (childGo != null)
                return childGo;
        }
        return null;
    }

    static EnglishTestComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("EnglishTestModule");
        _currentQuestionField = _componentType.GetField("currentQuestion", BindingFlags.NonPublic | BindingFlags.Instance);
        _indexField = _componentType.GetField("selectedAnswerIndex", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _currentQuestionField = null;
    private static FieldInfo _indexField = null;


    private FieldInfo _answerField;
    private Component _bombComponent;
    private Component _englishTestCompoent;
    private Component _questionComponent;
    private KMSelectable selectButton;
    private KMSelectable submitButton;
}