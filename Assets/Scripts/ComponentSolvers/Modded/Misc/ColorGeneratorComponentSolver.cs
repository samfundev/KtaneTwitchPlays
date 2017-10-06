using System;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class ColorGeneratorComponentSolver : ComponentSolver
{
	public ColorGeneratorComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
	    _redButton = (KMSelectable)_redButtonField.GetValue(bombComponent.GetComponent(_componentType));
	    _greenButton = (KMSelectable)_greenButtonField.GetValue(bombComponent.GetComponent(_componentType));
	    _blueButton = (KMSelectable)_blueButtonField.GetValue(bombComponent.GetComponent(_componentType));
	    _multiplyButton = (KMSelectable)_multiplyButtonField.GetValue(bombComponent.GetComponent(_componentType));
	    _submitButton = (KMSelectable)_submitButtonField.GetValue(bombComponent.GetComponent(_componentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        var commands = inputCommand.ToLowerInvariant().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

        int red = 0;
        int green = 0;
        int blue = 0;
        if (commands.Length == 4 && commands[0] == "submit" && int.TryParse(commands[1], out red) && int.TryParse(commands[2], out green) && int.TryParse(commands[3], out blue) && red >= 0 && green >= 0 && blue >= 0)
        {
            yield return inputCommand;
            for (int i = 1; i < 10; i++)
            {
                if ((red % 10) == i)
                {
                    yield return DoInteractionClick(_redButton);
                }
                if ((blue % 10) == i)
                {
                    yield return DoInteractionClick(_blueButton);
                }
                if ((green % 10) == i)
                {
                    yield return DoInteractionClick(_greenButton);
                }
                yield return DoInteractionClick(_multiplyButton);
            }

            red /= 10;
            green /= 10;
            blue /= 10;
            for (int i = 0; i < red && i < 26; i++)
            {
                yield return DoInteractionClick(_redButton);
            }
            for (int i = 0; i < green && i < 26; i++)
            {
                yield return DoInteractionClick(_greenButton);
            }
            for (int i = 0; i < blue && i < 26; i++)
            {
                yield return DoInteractionClick(_blueButton);
            }
        }
        yield return DoInteractionClick(_multiplyButton);
        yield return DoInteractionClick(_submitButton);

    }

    static ColorGeneratorComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("ButtonMasherModule", "ESColorGenerator");
	    _redButtonField = _componentType.GetField("Red", BindingFlags.Public | BindingFlags.Instance);
	    _greenButtonField = _componentType.GetField("Green", BindingFlags.Public | BindingFlags.Instance);
	    _blueButtonField = _componentType.GetField("Blue", BindingFlags.Public | BindingFlags.Instance);
	    _multiplyButtonField = _componentType.GetField("Multiply", BindingFlags.Public | BindingFlags.Instance);
	    _submitButtonField = _componentType.GetField("Submit", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _redButtonField = null;
    private static FieldInfo _greenButtonField = null;
    private static FieldInfo _blueButtonField = null;
    private static FieldInfo _multiplyButtonField = null;
    private static FieldInfo _submitButtonField = null;

    private KMSelectable _redButton = null;
    private KMSelectable _greenButton = null;
    private KMSelectable _blueButton = null;
    private KMSelectable _multiplyButton = null;
    private KMSelectable _submitButton = null;
}