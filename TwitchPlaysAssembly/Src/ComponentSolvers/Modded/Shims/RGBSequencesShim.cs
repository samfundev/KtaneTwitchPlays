using System;
using System.Collections;

public class RGBSequencesShim : ComponentSolverShim
{
	public RGBSequencesShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_btns = _component.GetValue<KMSelectable[]>("LEDses");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		int[] answer = _component.GetValue<int[]>("Random");
		bool Vwl = _component.GetValue<bool>("Vowel");
		bool Const = _component.GetValue<bool>("Consonant");
		for (int i = 0; i < 10; i++)
		{
			if ((i == (((((answer[0] + 1) * (answer[1] + 1) * (answer[2] + 1)) - 1) % 9) + 1) && Vwl == true && Const == true) || (i == (((answer[0] + 1) * (answer[1] + 1) * (answer[2] + 1)) % 10) && ((Vwl == true && Const == false) || (Vwl == false && Const == true) || (Vwl == false && Const == false))))
			{
				yield return DoInteractionClick(_btns[i]);
				yield break;
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("RGBSequences");

	private readonly object _component;

	private readonly KMSelectable[] _btns;
}
