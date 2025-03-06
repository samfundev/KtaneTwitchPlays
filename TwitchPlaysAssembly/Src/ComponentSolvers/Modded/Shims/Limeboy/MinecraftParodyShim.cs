using System;
using System.Collections;

[ModuleID("minecraftParody")]
public class MinecraftParodyShim : ComponentSolverShim
{
	public MinecraftParodyShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("Button");
		_submit = _component.GetValue<KMSelectable>("submit");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		int[] correctIndexes = _component.GetValue<int[]>("answer");
		for (int i = 0; i < 4; i++)
		{
			while (correctIndexes[i] != _component.GetValue<int[]>("currentchosenuser")[i])
				yield return DoInteractionClick(_buttons[i], 0.05f);
		}
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("MinecraftParody", "minecraftParody");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _submit;
}
