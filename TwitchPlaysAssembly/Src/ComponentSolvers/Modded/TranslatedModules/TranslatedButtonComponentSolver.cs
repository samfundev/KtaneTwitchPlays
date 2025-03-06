using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

[ModuleID("BigButtonTranslated")]
public class TranslatedButtonComponentSolver : ComponentSolver
{
	public TranslatedButtonComponentSolver(TwitchModule module) :
		base(module)
	{
		var component = module.BombComponent.GetComponent(ComponentType);
		_button = (KMSelectable) ButtonField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} tap [tap the button] | !{0} hold [hold the button] | !{0} release 7 [release when the digit shows 7]").Clone();
		Selectable selectable = module.BombComponent.GetComponent<Selectable>();
		selectable.OnCancel += () => { SelectedField.SetValue(component, false); return true; };

		LanguageCode = TranslatedModuleHelper.GetLanguageCode(component, ComponentType);
		ModInfo.moduleDisplayName = $"Big Button Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(component, ComponentType)}";
		Module.HeaderText = ModInfo.moduleDisplayName;

		var mat = (Material) StripMaterialField.GetValue(component);
		StripMaterialField.SetValue(component, Object.Instantiate(mat));
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim();
		if (!_held && inputCommand.EqualsAny("tap", "click"))
		{
			yield return "tap";
			yield return DoInteractionClick(_button);
		}
		else if (!_held && inputCommand.Equals("hold"))
		{
			yield return "hold";

			_held = true;
			DoInteractionStart(_button);
			yield return new WaitForSeconds(2.0f);
		}
		else if (_held)
		{
			string[] commandParts = inputCommand.Split(' ');
			if (commandParts.Length != 2 || !commandParts[0].Equals("release")) yield break;
			if (!int.TryParse(commandParts[1], out int second))
			{
				yield break;
			}

			if (second < 0 || second > 9) yield break;
			IEnumerator releaseCoroutine = ReleaseCoroutine(second);
			while (releaseCoroutine.MoveNext())
			{
				yield return releaseCoroutine.Current;
			}
		}
	}

	private IEnumerator ReleaseCoroutine(int second)
	{
		yield return "release";

		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();

		string secondString = second.ToString();

		float timeRemaining = float.PositiveInfinity;
		while (timeRemaining > 0.0f && _held)
		{
			timeRemaining = timerComponent.TimeRemaining;

			if (Module.Bomb.CurrentTimerFormatted.Contains(secondString))
			{
				DoInteractionEnd(_button);
				_held = false;
			}

			yield return "trycancel";
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("BigButtonTranslatedModule");
	private static readonly FieldInfo ButtonField = ComponentType.GetField("Button", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo SelectedField = ComponentType.GetField("isModuleSelected", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo StripMaterialField = ComponentType.GetField("StripMatColor", BindingFlags.Public | BindingFlags.Instance);

	private readonly KMSelectable _button;
	private bool _held;
}
