using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class LetteredKeysComponentSolver : ComponentSolver
{
	public LetteredKeysComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_buttons = bombComponent.GetComponent<KMSelectable>().Children;
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} press b");
    }

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
	    if (inputCommand.StartsWith("press ", System.StringComparison.InvariantCultureIgnoreCase))
	    {
	        Match match = Regex.Match(inputCommand, "[1-4a-d]", RegexOptions.IgnoreCase);
	        if (!match.Success)
	        {
	            yield break;
	        }

	        if (int.TryParse(match.Value, out int buttonID))
	        {
	            yield return null;
	            yield return DoInteractionClick(_buttons[buttonID - 1]);
	            yield break;
	        }

	        foreach (KMSelectable button in _buttons)
	        {
	            if (match.Value.Equals(button.GetComponentInChildren<TextMesh>().text, System.StringComparison.InvariantCultureIgnoreCase))
	            {
	                yield return null;
	                yield return DoInteractionClick(button);
	                yield break;
	            }
	        }
	    }
    }

	private KMSelectable[] _buttons = null;
}