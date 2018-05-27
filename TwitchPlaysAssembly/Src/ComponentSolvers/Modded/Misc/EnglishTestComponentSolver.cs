using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class EnglishTestComponentSolver : ComponentSolver
{
	public EnglishTestComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_englishTestComponent = bombComponent.GetComponent(_componentType);
		selectButton = FindChildGameObjectByName(bombComponent.gameObject, "Left Button").GetComponent<KMSelectable>();
		submitButton = FindChildGameObjectByName(bombComponent.gameObject, "Submit Button").GetComponent<KMSelectable>();
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Answer the displayed question with !{0} submit 2 or !{0} answer 2. (Answers are numbered from 1-4 starting from left to right.)");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.ToLowerInvariant().Trim().Split(' ');
		if (split.Length != 2 || !split[0].EqualsAny("submit", "answer"))
		{
			yield break;
		}

		if (!int.TryParse(split[1], out int desiredIndex) || desiredIndex < 1)
		{
			yield break;
		}
		desiredIndex--;
		yield return null;
		int currentIndex = (int) _indexField.GetValue(_englishTestComponent);

		while ((int)_indexField.GetValue(_englishTestComponent) != desiredIndex)
		{
			yield return DoInteractionClick(selectButton);
			if ((int) _indexField.GetValue(_englishTestComponent) == currentIndex)
			{
				yield return string.Format("sendtochaterror I can't select answer #{0} because that answer doesn't exist.", desiredIndex + 1);
				yield break;
			}
		}
		yield return DoInteractionClick(submitButton);
	}

	private GameObject FindChildGameObjectByName(GameObject parent, string name)
	{
		foreach (Transform child in parent.transform)
		{
			if (child.gameObject.name == name)
				return child.gameObject;
			GameObject childGo = FindChildGameObjectByName(child.gameObject, name);
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
	
	private readonly Component _englishTestComponent;
	private readonly KMSelectable selectButton;
	private readonly KMSelectable submitButton;
}
