using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class EnglishTestComponentSolver : ComponentSolver
{
	public EnglishTestComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_englishTestComponent = bombComponent.GetComponent(ComponentType);
		_selectButton = FindChildGameObjectByName(bombComponent.gameObject, "Left Button").GetComponent<KMSelectable>();
		_submitButton = FindChildGameObjectByName(bombComponent.gameObject, "Submit Button").GetComponent<KMSelectable>();
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Answer the displayed question with !{0} submit 2 or !{0} answer 2. (Answers are numbered from 1-4 starting from left to right.)");
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
		int currentIndex = (int) IndexField.GetValue(_englishTestComponent);

		while ((int) IndexField.GetValue(_englishTestComponent) != desiredIndex)
		{
			yield return DoInteractionClick(_selectButton);
			if ((int) IndexField.GetValue(_englishTestComponent) != currentIndex) continue;
			yield return
				$"sendtochaterror I can't select answer #{desiredIndex + 1} because that answer doesn't exist.";
			yield break;
		}
		yield return DoInteractionClick(_submitButton);
	}

	private static GameObject FindChildGameObjectByName(GameObject parent, string name)
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
		ComponentType = ReflectionHelper.FindType("EnglishTestModule");
		IndexField = ComponentType.GetField("selectedAnswerIndex", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo IndexField;

	private readonly Component _englishTestComponent;
	private readonly KMSelectable _selectButton;
	private readonly KMSelectable _submitButton;
}
