using System;
using System.Collections;
using UnityEngine;

public class ShapesAndBombsShim : ComponentSolverShim
{
	public ShapesAndBombsShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("ModuleButtons");
		_submit = _component.GetValue<KMSelectable>("SubmitButton");
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
	private readonly KMSelectable _submit;
}
