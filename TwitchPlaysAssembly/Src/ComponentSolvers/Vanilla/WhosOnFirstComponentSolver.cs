using System;
using System.Linq;
using System.Collections;
using UnityEngine;

public class WhosOnFirstComponentSolver : ComponentSolver
{
    public WhosOnFirstComponentSolver(BombCommander bombCommander, WhosOnFirstComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
		_buttons = bombComponent.Buttons;
        modInfo = ComponentSolverFactory.GetModuleInfo("WhosOnFirstComponentSolver");
    }

	static string[] phrases = new[] { "ready", "first", "no", "blank", "nothing", "yes", "what", "uhhh", "left", "right", "middle", "okay", "wait", "press", "you", "you are", "your", "you're", "ur", "u", "uh huh", "uh uh", "what?", "done", "next", "hold", "sure", "like" };

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
		string word = inputCommand.ToLowerInvariant();
		if (!phrases.Contains(word))
		{
			yield return null;
			yield return string.Format("sendtochaterror The word \"{0}\" isn't a valid word.", word);
			yield break;
		}

        foreach (KeypadButton button in _buttons)
        {
            if (inputCommand.Equals(button.GetText(), StringComparison.InvariantCultureIgnoreCase))
            {
                yield return null;
				button.Interact();
				yield return new WaitForSeconds(0.1f);
                yield break;
            }
        }

		yield return null;
		yield return "unsubmittablepenalty";
	}

    private KeypadButton[] _buttons = null;
}
