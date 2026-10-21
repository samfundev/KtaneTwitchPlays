using System.Collections;
using System.Collections.Generic;
using System.Linq;

[ModuleID("borderedKeys")]
internal class BorderedKeysShim : ReflectionComponentSolverShim
{
	private readonly KMSelectable display;
	private readonly List<KMSelectable> keys;

	public BorderedKeysShim(TwitchModule module) : base(module, "BorderedKeysScript")
	{
		List<KMSelectable> buttons = _component.GetValue<List<KMSelectable>>("keys");
		keys = buttons.Take(6).ToList();
		display = buttons.Last();
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (_component.GetValue<bool[]>("alreadypressed").Take(6).Count(b => b) < 5)
		{
			int pressCount = _component.GetValue<int>("pressCount");
			int currentCount = _component.GetValue<int>("currentCount");
			List<string> answers = _component.GetValue<List<string>>("answer");
			IEnumerable<KMSelectable> validKeys = keys.Where(key => answers[keys.IndexOf(key)] == (pressCount - currentCount + 1).ToString());
			validKeys.ToList().ForEach(key => key.OnInteract());
			display.OnInteract();

			while (!_component.GetValue<bool>("pressable"))
			{
				yield return true;
			}
		}

		while (!_component.GetValue<bool>("moduleSolved"))
		{
			yield return true;
		}
	}
}