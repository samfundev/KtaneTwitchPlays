using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class TranslatedNeedyVentComponentSolver : ComponentSolver
{
	public TranslatedNeedyVentComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_yesButton = (MonoBehaviour)_yesButtonField.GetValue(bombComponent.GetComponent(_needyVentComponentSolverType));
		_noButton = (MonoBehaviour)_noButtonField.GetValue(bombComponent.GetComponent(_needyVentComponentSolverType));
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} yes, !{0} y [answer yes] | !{0} no, !{0} n [answer no]");
		
		if (bombCommander != null)
		{
			string language = TranslatedModuleHelper.GetManualCodeAddOn(bombComponent);
			if (language != null) modInfo.manualCode = "Venting%20Gas";
			//if (language != null) modInfo.manualCode = $"Venting%20Gas{language}";
			modInfo.moduleDisplayName = $"Needy Vent Gas Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(bombComponent)}";
			bombComponent.StartCoroutine(SetHeaderText());
		}
	}

	private IEnumerator SetHeaderText()
	{
		yield return new WaitUntil(() => ComponentHandle != null);
		ComponentHandle.HeaderText = modInfo.moduleDisplayName;
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant();
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
		_needyVentComponentSolverType = ReflectionHelper.FindType("VentGasTranslatedModule");
		_yesButtonField = _needyVentComponentSolverType.GetField("YesButton", BindingFlags.Public | BindingFlags.Instance);
		_noButtonField = _needyVentComponentSolverType.GetField("NoButton", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _needyVentComponentSolverType = null;
	private static FieldInfo _yesButtonField = null;
	private static FieldInfo _noButtonField = null;

	private MonoBehaviour _yesButton = null;
	private MonoBehaviour _noButton = null;
}
