using System;
using System.Collections;
using System.Collections.Generic;

[ModuleID("CrypticPassword")]
public class CrypticPasswordComponentSolver : ComponentSolver
{
	public CrypticPasswordComponentSolver(TwitchModule module) :
		base(module)
	{
		component = Module.BombComponent.GetComponent(componentType);
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		SetHelpMessage("!{0} submit answer [submit an answer] | !{0} toggle [move all columns down one character]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.ToLowerInvariant().SplitFull(" ,;");

		if (split.Length == 1 && split[0] == "toggle")
		{
			yield return null;
			for (int i = 0; i < 6; i++)
			{
				yield return DoInteractionClick(selectables[i + 6]);
			}
		}
		else if (split.Length == 2 && split[0].EqualsAny("press", "hit", "enter", "push", "submit") && split[1].Length == 6)
		{
			yield return null;

			var word = split[1].ToUpperInvariant();
			var displayLetters = component.GetValue<List<char>[]>("displayLetters");
			for (int i = 0; i < 6; i++)
			{
				if (!displayLetters[i].Contains(word[i]))
				{
					yield return "unsubmittablepenalty";
					yield break;
				}
			}

			var displayIndices = component.GetValue<int[]>("displayIndices");
			for (int i = 0; i < 6; i++)
			{
				yield return SelectIndex(displayIndices[i], displayLetters[i].IndexOf(word[i]), 5, selectables[i], selectables[i + 6]);
			}

			yield return DoInteractionClick(selectables[12]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		yield return RespondToCommandInternal(component.GetValue<string>("solutionWord"));
	}

	private static readonly Type componentType = ReflectionHelper.FindType("CrypticPassword");

	private readonly object component;
	private readonly KMSelectable[] selectables;
}
