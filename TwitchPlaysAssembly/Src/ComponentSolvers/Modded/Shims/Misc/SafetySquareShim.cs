using System.Collections;

[ModuleID("safetySquare")]
public class SafetySquareShim : ReflectionComponentSolverShim
{
	public SafetySquareShim(TwitchModule module)
		: base(module, "SafetySquareScript", "safetySquare")
	{
		_buttons = _component.GetValue<KMSelectable[]>("buttons");
		_hazardButtons = new KMSelectable[] { _component.GetValue<KMSelectable>("whiteButton"), _component.GetValue<KMSelectable>("yellowButton"), _component.GetValue<KMSelectable>("redButton"), _component.GetValue<KMSelectable>("blueButton") };
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		if (!_component.GetValue<bool>("stageTwo"))
		{
			int answer = _component.GetValue<int>("answer");
			if (answer < 4)
				yield return DoInteractionClick(_buttons[answer - 1]);
			else if (answer == 4)
				yield return DoInteractionClick(_buttons[answer]);
			else
				yield return DoInteractionClick(_buttons[answer - 2]);
		}
		string[] answers = new string[] { _component.GetValue<string>("ans1"), _component.GetValue<string>("ans2"), _component.GetValue<string>("ans3"), _component.GetValue<string>("ans4") };
		int start = _component.GetValue<int>("stage") - 1;
		for (int i = start; i < 4; i++)
			yield return DoInteractionClick(_hazardButtons["WYRB".IndexOf(answers[i][0])]);
	}

	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable[] _hazardButtons;
}
