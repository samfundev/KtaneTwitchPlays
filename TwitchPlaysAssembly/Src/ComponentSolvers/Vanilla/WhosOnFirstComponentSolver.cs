using System;
using System.Collections;
using UnityEngine;

public class WhosOnFirstComponentSolver : ComponentSolver
{
    public WhosOnFirstComponentSolver(BombCommander bombCommander, WhosOnFirstComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
		_buttons = bombComponent.Buttons;
        modInfo = ComponentSolverFactory.GetModuleInfo("WhosOnFirstComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        foreach (KeypadButton button in _buttons)
        {
            if (inputCommand.Equals(button.GetText(), StringComparison.InvariantCultureIgnoreCase))
            {
                yield return null;
				button.Interact();
				yield return new WaitForSeconds(0.1f);
                break;
            }
        }
    }

    private KeypadButton[] _buttons = null;
}
