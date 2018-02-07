using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlphabetComponentSolver : ComponentSolver
{
	public AlphabetComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_buttons = bombComponent.GetComponent<KMSelectable>().Children;
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length < 2 || !commands[0].EqualsAny("submit", "press")) yield break;
		string buttonLabels = _buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToLower()).Join(string.Empty);

		List<int> buttons = commands.Skip(1).Join(string.Empty).ToCharArray().Select(x => buttonLabels.IndexOf(x)).ToList();
	    if (buttons.Any(x => x < 0)) yield break;

		yield return null;

		foreach (int button in buttons)
		{
			yield return DoInteractionClick(_buttons[button]);
		}
	}

	private KMSelectable[] _buttons = null;
}