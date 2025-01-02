using System;
using System.Collections;

public class PokerShim : ComponentSolverShim
{
	public PokerShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_foldButton = _component.GetValue<KMSelectable>("FoldBut");
		_checkButton = _component.GetValue<KMSelectable>("CheckBut");
		_minButton = _component.GetValue<KMSelectable>("MinBut");
		_maxButton = _component.GetValue<KMSelectable>("MaxBut");
		_allInButton = _component.GetValue<KMSelectable>("AllInBut");
		_bluffButton = _component.GetValue<KMSelectable>("BluffBut");
		_truthButton = _component.GetValue<KMSelectable>("TruthBut");
		_card1Button = _component.GetValue<KMSelectable>("Card1But");
		_card2Button = _component.GetValue<KMSelectable>("Card2But");
		_card3Button = _component.GetValue<KMSelectable>("Card3But");
		_card4Button = _component.GetValue<KMSelectable>("Card4But");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		int stage = _component.GetValue<int>("stage");
		if (stage == 1)
		{
			KMSelectable[] stage1Buttons = { _foldButton, _checkButton, _minButton, _maxButton, _allInButton };
			string[] stage1Names = { "Fold", "Check", "MinRaise", "MaxRaise", "AllIn" };
			string corrAns = _component.GetValue<string>("correctButton");
			yield return DoInteractionClick(stage1Buttons[Array.IndexOf(stage1Names, corrAns)]);
			stage++;
		}
		if (stage == 2)
		{
			KMSelectable[] stage2Buttons = { _truthButton, _bluffButton };
			string[] stage2Names = { "Truth", "Bluff" };
			string corrAns = _component.GetValue<string>("bluffTruth");
			yield return DoInteractionClick(stage2Buttons[Array.IndexOf(stage2Names, corrAns)]);
			stage++;
		}
		if (stage == 3)
		{
			KMSelectable[] stage3Buttons = { _card1Button, _card2Button, _card3Button, _card4Button };
			string[] stage3Names = { "Card1", "Card2", "Card3", "Card4" };
			string corrAns = _component.GetValue<string>("correctCard");
			yield return DoInteractionClick(stage3Buttons[Array.IndexOf(stage3Names, corrAns)]);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("PokerScript", "poker");

	private readonly object _component;

	private readonly KMSelectable _foldButton;
	private readonly KMSelectable _checkButton;
	private readonly KMSelectable _minButton;
	private readonly KMSelectable _maxButton;
	private readonly KMSelectable _allInButton;
	private readonly KMSelectable _bluffButton;
	private readonly KMSelectable _truthButton;
	private readonly KMSelectable _card1Button;
	private readonly KMSelectable _card2Button;
	private readonly KMSelectable _card3Button;
	private readonly KMSelectable _card4Button;
}
