using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnagramsComponentSolver : ComponentSolver
{
	public AnagramsComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		_buttons = bombComponent.GetComponent<KMSelectable>().Children;
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
	    List<KMSelectable> buttons = new List<KMSelectable>();
	    List<string> buttonLabels = _buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToLowerInvariant()).ToList();
        if (inputCommand.StartsWith("submit ", System.StringComparison.InvariantCultureIgnoreCase))
	    {
	        inputCommand = inputCommand.Substring(7).ToLowerInvariant();
	        foreach (char c in inputCommand)
	        {
	            int index = buttonLabels.IndexOf(c.ToString());
	            if (index < 0)
	                yield break;
                buttons.Add(_buttons[index]);
	        }
	        yield return null;
	        yield return DoInteractionClick(_buttons[3]);
	        foreach (KMSelectable b in buttons)
	        {
	            yield return DoInteractionClick(b);
	        }
	        yield return DoInteractionClick(_buttons[7]);
	    }
	    
	}

	private KMSelectable[] _buttons = null;
}