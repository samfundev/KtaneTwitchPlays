using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class BabaIsWhoComponentSolver : ComponentSolver
{
	public BabaIsWhoComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		SetHelpMessage("Press a character using using !{0} press <character>. Characters: baba, keke, me, rock, flag and wall.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "(^(press|hit|enter|push) ?| is (pressed|entered|hit|pushed)$)", "");

		foreach (KMSelectable selectable in selectables)
		{
			if (selectable.GetComponent<Renderer>().material.name.Replace(" (Instance)", "") == inputCommand)
			{
				yield return null;
				yield return DoInteractionClick(selectable);
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type babaIsWhoType = ReflectionHelper.FindType("babaIsWhoScript");
		if (babaIsWhoType == null) yield break;

		object component = Module.BombComponent.GetComponent(babaIsWhoType);

		yield return RespondToCommandInternal(component.GetValue<string>("correctCharacter"));
	}

	private readonly KMSelectable[] selectables;
}
