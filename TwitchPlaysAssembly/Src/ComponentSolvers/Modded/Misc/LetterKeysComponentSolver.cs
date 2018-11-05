using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class LetterKeysComponentSolver : ComponentSolver
{
	public LetterKeysComponentSolver(TwitchModule module) :
		base(module)
	{
		_buttons = module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} press b", "Letter%20Keys");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (!inputCommand.StartsWith("press ", System.StringComparison.InvariantCultureIgnoreCase)) yield break;
		Match match = Regex.Match(inputCommand, "[1-4a-d]", RegexOptions.IgnoreCase);
		if (!match.Success)
			yield break;

		if (int.TryParse(match.Value, out int buttonID))
		{
			yield return null;
			yield return DoInteractionClick(_buttons[buttonID - 1]);
			yield break;
		}

		foreach (KMSelectable button in _buttons)
		{
			if (!match.Value.Equals(button.GetComponentInChildren<TextMesh>().text,
				System.StringComparison.InvariantCultureIgnoreCase)) continue;
			yield return null;
			yield return DoInteractionClick(button);
			yield break;
		}
	}

	private readonly KMSelectable[] _buttons;
}
