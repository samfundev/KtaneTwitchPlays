using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class InvisibleWallsComponentSolver : ComponentSolver
{
    public InvisibleWallsComponentSolver(BombCommander bombCommander, InvisibleWallsComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
		_buttons = bombComponent.Buttons;
        modInfo = ComponentSolverFactory.GetModuleInfo("InvisibleWallsComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {     
        if (!inputCommand.StartsWith("move ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }

        inputCommand = inputCommand.Substring(5);
        MatchCollection matches = Regex.Matches(inputCommand, @"[udlr]", RegexOptions.IgnoreCase);
        if (matches.Count > 35)
        {
	        yield return null;
            yield return "elevator music";
        }

        foreach (Match move in matches)
        {
            KeypadButton button = _buttons[ buttonIndex[ move.Value.ToLowerInvariant() ] ];
            
            if (button != null)
            {
                yield return move.Value;

                if (CoroutineCanceller.ShouldCancel)
                {
	                CoroutineCanceller.ResetCancel();
                    yield break;
                }

                yield return DoInteractionClick(button);
            }            
        }
    }
	
    private static readonly Dictionary<string, int> buttonIndex = new Dictionary<string, int>
    {
        {"u", 0}, {"l", 1}, {"r", 2}, {"d", 3}
    };

    private List<KeypadButton> _buttons = null;
}
