using System;
using System.Collections;
using System.Linq;
using KModkit;

public class VexillologyShim : ComponentSolverShim
{
	public VexillologyShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_horizFlag = _component.GetValue<KMSelectable[]>("HorizontalFlag");
		_vertFlag = _component.GetValue<KMSelectable[]>("VerticalFlag");
		_sweFlag = _component.GetValue<KMSelectable[]>("SwedishFlag");
		_norFlag = _component.GetValue<KMSelectable[]>("NorwegianFlag");
		_cButtons = _component.GetValue<KMSelectable[]>("ColourButtons");
		_submit = _component.GetValue<KMSelectable>("FlagTopSubmit");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		var edgework = Module.BombComponent.GetComponent<KMBombInfo>();
		int ActiveFlag = _component.GetValue<int>("ActiveFlag");
		int AnswerColour1 = _component.GetValue<int>("AnswerColour1");
		int AnswerColour2 = _component.GetValue<int>("AnswerColour2");
		int AnswerColour3 = _component.GetValue<int>("AnswerColour3");
		int FlagColour1 = _component.GetValue<int>("FlagColour1");
		int FlagColour2 = _component.GetValue<int>("FlagColour2");
		int FlagColour3 = _component.GetValue<int>("FlagColour3");
		int ActiveColour = _component.GetValue<int>("ActiveColour");
		int SubmitTime = _component.GetValue<int>("SubmitTime");
		if (AnswerColour1 != FlagColour1)
		{
			if (ActiveColour != AnswerColour1)
			{
				ActiveColour = AnswerColour1;
				yield return DoInteractionClick(_cButtons[ActiveColour]);
			}
			switch (ActiveFlag)
			{
				case 0:
					yield return DoInteractionClick(_horizFlag[0]);
					break;
				case 1:
					yield return DoInteractionClick(_vertFlag[0]);
					break;
				case 2:
					yield return DoInteractionClick(_sweFlag[UnityEngine.Random.Range(0, 4)]);
					break;
				default:
					yield return DoInteractionClick(_norFlag[UnityEngine.Random.Range(0, 4)]);
					break;
			}
		}
		if (AnswerColour2 != FlagColour2)
		{
			if (ActiveColour != AnswerColour2)
			{
				ActiveColour = AnswerColour2;
				yield return DoInteractionClick(_cButtons[ActiveColour]);
			}
			switch (ActiveFlag)
			{
				case 0:
					yield return DoInteractionClick(_horizFlag[1]);
					break;
				case 1:
					yield return DoInteractionClick(_vertFlag[1]);
					break;
				case 2:
					yield return DoInteractionClick(_sweFlag[UnityEngine.Random.Range(4, 6)]);
					break;
				default:
					yield return DoInteractionClick(_norFlag[UnityEngine.Random.Range(4, 6)]);
					break;
			}
		}
		if (AnswerColour3 != FlagColour3 && ActiveFlag != 2)
		{
			if (ActiveColour != AnswerColour3)
			{
				ActiveColour = AnswerColour3;
				yield return DoInteractionClick(_cButtons[ActiveColour]);
			}
			switch (ActiveFlag)
			{
				case 0:
					yield return DoInteractionClick(_horizFlag[2]);
					break;
				case 1:
					yield return DoInteractionClick(_vertFlag[2]);
					break;
				default:
					yield return DoInteractionClick(_norFlag[UnityEngine.Random.Range(6, 14)]);
					break;
			}
		}
		char[] digits = null;
		if (_component.GetValue<bool>("_ChadRomania"))
			digits = new[] { '0', '5' };
		else if (SubmitTime != 10)
			digits = new[] { SubmitTime.ToString()[0] };
		else
			digits = new[] { edgework.GetSerialNumberNumbers().Last().ToString()[0] };
		while (true)
		{
			var time = edgework.GetTime() >= 60 ? edgework.GetFormattedTime() : edgework.GetFormattedTime().Remove(2);
			if (digits.Any(time.Contains))
				break;
			yield return true;
		}
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("vexillologyScript", "vexillology");

	private readonly object _component;

	private readonly KMSelectable[] _horizFlag;
	private readonly KMSelectable[] _vertFlag;
	private readonly KMSelectable[] _sweFlag;
	private readonly KMSelectable[] _norFlag;
	private readonly KMSelectable[] _cButtons;
	private readonly KMSelectable _submit;
}
