using System;
using System.Collections;

public class ColoredKeysShim : ComponentSolverShim
{
	public ColoredKeysShim(TwitchModule module)
		: base(module, "lgndColoredKeys")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("buttons");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (_component.GetValue<bool>("moduleSolved")) yield return true;
		bool[] corBtns = new bool[] { _component.GetValue<bool>("TLcorrect"), _component.GetValue<bool>("TRcorrect"), _component.GetValue<bool>("BLcorrect"), _component.GetValue<bool>("BRcorrect") };
		for (int i = 0; i < 4; i++)
		{
			if (corBtns[i])
			{
				yield return DoInteractionClick(_buttons[i]);
				break;
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ColoredKeysScript");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
}
