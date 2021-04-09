using System;
using System.Collections;

public class SymbolicCoordinatesShim : ComponentSolverShim
{
	public SymbolicCoordinatesShim(TwitchModule module)
		: base(module, "symbolicCoordinates")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_upLet = _component.GetValue<KMSelectable>("lettersUp");
		_downLet = _component.GetValue<KMSelectable>("lettersDown");
		_upDig = _component.GetValue<KMSelectable>("digitsUp");
		_downDig = _component.GetValue<KMSelectable>("digitsDown");
		_submit = _component.GetValue<KMSelectable>("submitBut");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		yield return RespondToCommandUnshimmed(inputCommand);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		char[] letters = _component.GetValue<char[]>("lettersEntries");
		char[] digits = _component.GetValue<char[]>("digitsEntries");
		char[] correctLets = { _component.GetValue<string>("correctLetter1")[0], _component.GetValue<string>("correctLetter2")[0], _component.GetValue<string>("correctLetter3")[0] };
		string[] correctDigNames = { "correctDigit1", "correctDigit2", "correctDigit3" };
		int start = _component.GetValue<int>("stage");
		for (int i = start; i < 4; i++)
		{
			char correctDigit = _component.GetValue<string>(correctDigNames[i - 1])[0];
			int letInd = _component.GetValue<int>("lettersIndex");
			int digInd = _component.GetValue<int>("digitsIndex");
			yield return SelectIndex(letInd, Array.IndexOf(letters, correctLets[i - 1]), letters.Length, _downLet, _upLet);
			yield return SelectIndex(digInd, Array.IndexOf(digits, correctDigit), digits.Length, _downDig, _upDig);
			yield return DoInteractionClick(_submit);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("symbolicCoordinatesScript", "symbolicCoordinates");

	private readonly object _component;
	private readonly KMSelectable _upLet;
	private readonly KMSelectable _downLet;
	private readonly KMSelectable _upDig;
	private readonly KMSelectable _downDig;
	private readonly KMSelectable _submit;
}
