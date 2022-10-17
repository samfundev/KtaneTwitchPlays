using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class ShapesAndBombsShim : ComponentSolverShim
{
	public ShapesAndBombsShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("ModuleButtons");
		_display = _component.GetValue<KMSelectable>("NumScreen");
		_submit = _component.GetValue<KMSelectable>("SubmitButton");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		Match modulesMatch = Regex.Match(inputCommand, "^(display|disp|d) (([0-1])?[0-9])$", RegexOptions.IgnoreCase);
		if (modulesMatch.Success)
		{
			int number = int.Parse(modulesMatch.Groups[2].Value);
			if (number < 0 || number > 14) yield break;

			yield return null;
			while (_display.transform.GetChild(0).GetComponent<TextMesh>().text != number.ToString())
				yield return DoInteractionClick(_display);
		}
		else
		{
			IEnumerator command = RespondToCommandUnshimmed(inputCommand);
			while (command.MoveNext())
				yield return command.Current;
		}
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (_component.GetValue<Coroutine>("nowCoroutine") != null || _component.GetValue<Coroutine>("subCoroutine") != null) yield return true;
		redo:
		string shapeSolution = _component.GetValue<string>("shapeSolution");
		string myShape = _component.GetValue<string>("myShape");
		for (int i = 0; i < shapeSolution.Length; i++)
		{
			if (myShape[i] != shapeSolution[i])
			{
				yield return DoInteractionClick(_buttons[i]);
				if (shapeSolution != _component.GetValue<string>("shapeSolution"))
					goto redo;
			}
		}
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ShapesBombs", "ShapesBombs");

	private readonly object _component;

	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _display;
	private readonly KMSelectable _submit;
}
