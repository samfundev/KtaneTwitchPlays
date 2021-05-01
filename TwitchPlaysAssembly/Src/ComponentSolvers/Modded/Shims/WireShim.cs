using System;
using System.Collections;

public class WireShim : ComponentSolverShim
{
	public WireShim(TwitchModule module)
		: base(module, "wire")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_switches = new KMSelectable[] { _component.GetValue<KMSelectable>("switch1"), _component.GetValue<KMSelectable>("switch2"), _component.GetValue<KMSelectable>("switch3") };
		_startButton = _component.GetValue<KMSelectable>("startButton");
		_wire = _component.GetValue<KMSelectable>("intWire");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		if (!_component.GetValue<bool>("startLock"))
			yield return DoInteractionClick(_startButton);
		while (_component.GetValue<bool>("turnLock1") || _component.GetValue<bool>("turnLock2") || _component.GetValue<bool>("turnLock3"))
			yield return null;
		for (int i = 0; i < 3; i++)
		{
			string dialans = _component.GetValue<string>("dial" + (i + 1) + "Answer");
			while (_component.GetValue<string>("switch" + (i + 1) + "Set") != dialans)
			{
				yield return DoInteractionClick(_switches[i]);
				while (_component.GetValue<bool>("turnLock" + (i + 1)))
					yield return null;
			}
		}
		while (_component.GetValue<bool>("wireLock"))
			yield return null;
		int time = int.Parse(_component.GetValue<string>("stopTime"));
		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		while ((int) timerComponent.TimeRemaining % 10 != time)
			yield return null;
		yield return DoInteractionClick(_wire);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("wireScript", "wire");

	private readonly object _component;

	private readonly KMSelectable[] _switches;
	private readonly KMSelectable _startButton;
	private readonly KMSelectable _wire;
}
