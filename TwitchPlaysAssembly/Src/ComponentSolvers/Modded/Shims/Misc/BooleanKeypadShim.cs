using System;
using System.Collections;

[ModuleID("BooleanKeypad")]
public class BooleanKeypadShim : ComponentSolverShim
{
	public BooleanKeypadShim(TwitchModule module)
		: base(module)
	{
		SetHelpMessage("Use '!{0} press 2 4' to press buttons 2 and 4. | Buttons are indexed 1-4 in reading order.");
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<object[]>("Buttons");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim().Replace("press", "solve").Replace("submit", "solve");
		IEnumerator command = RespondToCommandUnshimmed(inputCommand.ToLowerInvariant().Trim());
		while (command.MoveNext())
			yield return command.Current;
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		bool[] pressed = _component.GetValue<bool[]>("pressedButtons");
		bool[] answer = _component.GetValue<bool[]>("buttonTruths");
		for (int i = 0; i < 4; i++)
		{
			if (!pressed[i] && answer[i])
				yield return DoInteractionClick(_buttons[i].GetValue<KMSelectable>("button"));
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("BooleanKeypad");

	private readonly object _component;
	private readonly object[] _buttons;
}
