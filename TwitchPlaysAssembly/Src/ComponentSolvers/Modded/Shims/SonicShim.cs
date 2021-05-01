using System;
using System.Collections;

public class SonicShim : ComponentSolverShim
{
	public SonicShim(TwitchModule module)
		: base(module, "sonic")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_monitors = new KMSelectable[] { _component.GetValue<KMSelectable>("boots"), _component.GetValue<KMSelectable>("invincible"), _component.GetValue<KMSelectable>("life"), _component.GetValue<KMSelectable>("rings") };
		_startButton = _component.GetValue<KMSelectable>("startButton");
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

		int stage = _component.GetValue<int>("stage");
		if (stage == 1)
		{
			yield return DoInteractionClick(_startButton);
			stage++;
		}
		string[] answers = { _component.GetValue<string>("level1"), _component.GetValue<string>("level2"), _component.GetValue<string>("level3") };
		for (int i = stage; i < 5; i++)
			yield return DoInteractionClick(_monitors[Array.IndexOf(_names, answers[i - 2])]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("sonicScript", "sonic");

	private readonly object _component;
	private readonly string[] _names = { "boots", "invincible", "life", "rings" };

	private readonly KMSelectable[] _monitors;
	private readonly KMSelectable _startButton;
}
