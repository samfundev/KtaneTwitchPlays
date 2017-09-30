using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class EnglishTestComponentSolver : ComponentSolver
{
	public EnglishTestComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
	    _englishTestCompoent = bombComponent.GetComponent(_componentType);
	    selectButton = findChildGameObjectByName(bombComponent.gameObject, "Left Button").GetComponent<KMSelectable>();
	    submitButton = findChildGameObjectByName(bombComponent.gameObject, "Submit Button").GetComponent<KMSelectable>();
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
	    string[] split = inputCommand.ToLowerInvariant().Split(' ');
	    if (split.Length != 2 || (split[0] != "submit" && split[0] != "answer"))
	    {
	        yield break;
	    }
	    
	    int desiredIndex;
	    if (!int.TryParse(split[1], out desiredIndex) || desiredIndex < 1)
	    {
	        yield break;
	    }
	    desiredIndex--;
	    yield return null;
	    int currentIndex = (int) _indexField.GetValue(_englishTestCompoent);

        while ((int)_indexField.GetValue(_englishTestCompoent) != desiredIndex)
	    {
	        yield return DoInteractionClick(selectButton);
	        if ((int) _indexField.GetValue(_englishTestCompoent) == currentIndex)
	        {
	            yield return string.Format("sendtochaterror I can't select answer #{0} because that answer doesn't exist.", desiredIndex + 1);
	            yield break;
	        }
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
        _indexField = _componentType.GetField("selectedAnswerIndex", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _indexField = null;


    private FieldInfo _answerField;
    private Component _englishTestCompoent;
    private KMSelectable selectButton;
    private KMSelectable submitButton;
}