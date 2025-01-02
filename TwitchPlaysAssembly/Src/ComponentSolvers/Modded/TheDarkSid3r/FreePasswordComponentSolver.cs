using System;
using System.Collections;
using System.Collections.Generic;

public class FreePasswordComponentSolver : ComponentSolver
{
	public FreePasswordComponentSolver(TwitchModule module) :
		base(module)
	{
		_modType = GetModuleType();
		SetHelpMessage(_modType == "FreePassword" ? "!{0} submit [Presses the submit button] | !{0} WAHOO [Sets the display to \"WAHOO\"]" : "!{0} submit [Presses the submit button] | !{0} THEREEGGSONBOMBWAHOO [Sets the display to \"THEREEGGSONBOMBWAHOO\"]");
		_buttons.AddRange(module.BombComponent.GetComponent<KMSelectable>().Children);
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().Equals("submit"))
		{
			yield return null;
			yield return DoInteractionClick(_buttons[_modType == "FreePassword" ? 10 : 40], 0);
		}
		else if (_modType == "FreePassword" && inputCommand.Length == 5)
		{
			yield return null;
			int[] spinnerPos = Module.BombComponent.GetComponent(_modType).GetValue<int[]>("spinnerpositions");
			for (int i = 0; i < 5; i++)
				yield return SelectIndex(spinnerPos[i], Array.IndexOf(_spinnerChars, inputCommand.ToUpperInvariant()[i]), _spinnerChars.Length, _buttons[i + 5], _buttons[i]);
		}
		else if (_modType == "LargeFreePassword" && inputCommand.Length == 20)
		{
			yield return null;
			int[] spinnerPos = Module.BombComponent.GetComponent(_modType).GetValue<int[]>("spinnerpositions");
			for (int i = 0; i < 20; i++)
				yield return SelectIndex(spinnerPos[i], Array.IndexOf(_spinnerChars, inputCommand.ToUpperInvariant()[i]), _spinnerChars.Length, _buttons[i > 9 ? i + 20 : i + 10], _buttons[i > 9 ? i + 10 : i]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		yield return DoInteractionClick(_buttons[_modType == "FreePassword" ? 10 : 40]);
	}

	private List<KMSelectable> _buttons = new List<KMSelectable>();
	private readonly char[] _spinnerChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!?$%&@#'\"_+-=[{([{(^*<>.,`~\\/ ".ToCharArray();
	private readonly string _modType;
}