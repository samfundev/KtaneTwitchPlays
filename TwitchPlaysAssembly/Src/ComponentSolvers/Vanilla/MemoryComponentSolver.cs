using System.Collections;

public class MemoryComponentSolver : ComponentSolver
{
    public MemoryComponentSolver(BombCommander bombCommander, MemoryComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
		_buttons = bombComponent.Buttons;
        modInfo = ComponentSolverFactory.GetModuleInfo("MemoryComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        string[] commandParts = inputCommand.ToLowerInvariant().Split(' ');

        if (commandParts.Length != 2)
        {
            yield break;
        }

        int buttonNumber = 0;
        if (!int.TryParse(commandParts[1], out buttonNumber))
        {
            yield break;
        }

        if (buttonNumber >= 1 && buttonNumber <= 4)
        {
            if (commandParts[0].EqualsAny("position", "pos", "p"))
            {
                yield return "position";
				
                yield return DoInteractionClick(_buttons[buttonNumber - 1]);
            }
            else if (commandParts[0].EqualsAny("label", "lab", "l"))
            {
                foreach(KeypadButton button in _buttons)
                {
                    if (button.GetText().Equals(buttonNumber.ToString()))
                    {
                        yield return "label";
                        yield return DoInteractionClick(button);
                        break;
                    }
                }
            }
        }
    }

    private KeypadButton[] _buttons = null;
}
