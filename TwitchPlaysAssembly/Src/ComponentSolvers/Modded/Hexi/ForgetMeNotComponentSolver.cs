using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class ForgetMeNotComponentSolver : ComponentSolver
{
    public ForgetMeNotComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
        _buttons = (Array)_buttonsField.GetValue(bombComponent.GetComponent(_componentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.RegexMatch(out Match match, "^press ([0-9 ]+)$"))
        {
            yield break;
        }

	    yield return null;
        foreach (char buttonString in match.Groups[1].Value)
        {
            int val = buttonString - '0';
	        if (val < 0 || val > 9) continue;
	        MonoBehaviour button = (MonoBehaviour)_buttons.GetValue(val);

	        yield return $"trycancel The entry of Forget me not sequence was cancelled.";
			yield return DoInteractionClick(button);
		}
    }

    static ForgetMeNotComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("AdvancedMemory");
        _buttonsField = _componentType.GetField("Buttons", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _buttonsField = null;

    private Array _buttons = null;
}
