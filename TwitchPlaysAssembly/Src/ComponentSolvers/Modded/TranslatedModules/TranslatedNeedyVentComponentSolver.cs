using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class TranslatedNeedyVentComponentSolver : ComponentSolver
{
	public TranslatedNeedyVentComponentSolver(TwitchModule module) :
		base(module)
	{
		_yesButton = (MonoBehaviour) YesButtonField.GetValue(module.BombComponent.GetComponent(NeedyVentComponentSolverType));
		_noButton = (MonoBehaviour) NoButtonField.GetValue(module.BombComponent.GetComponent(NeedyVentComponentSolverType));
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} yes, !{0} y [answer yes] | !{0} no, !{0} n [answer no]");

		string language = TranslatedModuleHelper.GetManualCodeAddOn(module.BombComponent, module.BombComponent.GetComponent(NeedyVentComponentSolverType), NeedyVentComponentSolverType);
		if (language != null) ModInfo.manualCode = "Venting%20Gas";
		//if (language != null) modInfo.manualCode = $"Venting%20Gas{language}";
		ModInfo.moduleDisplayName = $"Needy Vent Gas Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(module.BombComponent, module.BombComponent.GetComponent(NeedyVentComponentSolverType), NeedyVentComponentSolverType)}";
		Module.Bomb.Bomb.StartCoroutine(SetHeaderText());
	}

	private IEnumerator SetHeaderText()
	{
		yield return new WaitUntil(() => Module != null);
		Module.HeaderText = ModInfo.moduleDisplayName;
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim();
		if (inputCommand.EqualsAny("y", "yes", "press y", "press yes"))
		{
			yield return "yes";
			yield return DoInteractionClick(_yesButton);
		}
		else if (inputCommand.EqualsAny("n", "no", "press n", "press no"))
		{
			yield return "no";
			yield return DoInteractionClick(_noButton);
		}
	}

	static TranslatedNeedyVentComponentSolver()
	{
		NeedyVentComponentSolverType = ReflectionHelper.FindType("VentGasTranslatedModule");
		YesButtonField = NeedyVentComponentSolverType.GetField("YesButton", BindingFlags.Public | BindingFlags.Instance);
		NoButtonField = NeedyVentComponentSolverType.GetField("NoButton", BindingFlags.Public | BindingFlags.Instance);
	}

	private static readonly Type NeedyVentComponentSolverType;
	private static readonly FieldInfo YesButtonField;
	private static readonly FieldInfo NoButtonField;

	private readonly MonoBehaviour _yesButton;
	private readonly MonoBehaviour _noButton;
}
