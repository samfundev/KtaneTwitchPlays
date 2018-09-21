using System;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class TranslatedButtonComponentSolver : ComponentSolver
{
	public TranslatedButtonComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_button = (KMSelectable) _buttonField.GetValue(bombComponent.GetComponent(_componentType));
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} tap [tap the button] | !{0} hold [hold the button] | !{0} release 7 [release when the digit shows 7] | (Important - Take note of the strip color on hold, it will change as other translated buttons get held, and the answer retains original color.)");
		Selectable selectable = bombComponent.GetComponent<Selectable>();
		selectable.OnCancel += () => { _selectedField.SetValue(bombComponent.GetComponent(_componentType), false); return true; };

		if (bombCommander == null) return;
		string language = TranslatedModuleHelper.GetManualCodeAddOn(bombComponent);
		if (language != null) modInfo.manualCode = $"The%20Button{language}";
		modInfo.moduleDisplayName = $"Big Button Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(bombComponent)}";
		bombComponent.StartCoroutine(SetHeaderText());
	}

	private IEnumerator SetHeaderText()
	{
		yield return new WaitUntil(() => ComponentHandle != null);
		ComponentHandle.HeaderText = modInfo.moduleDisplayName;
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

		TimerComponent timerComponent = BombCommander.Bomb.GetTimer();

		string secondString = second.ToString();

		float timeRemaining = float.PositiveInfinity;
		while (timeRemaining > 0.0f && _held)
		{
			timeRemaining = timerComponent.TimeRemaining;

			if (BombCommander.CurrentTimerFormatted.Contains(secondString))
			{
				DoInteractionEnd(_button);
				_held = false;
			}

			yield return "trycancel";
		}
	}

	static TranslatedButtonComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("BigButtonTranslatedModule");
		_buttonField = _componentType.GetField("Button", BindingFlags.Public | BindingFlags.Instance);
		_selectedField = _componentType.GetField("isSelected", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonField = null;
	private static FieldInfo _selectedField = null;

	private readonly KMSelectable _button = null;
	private bool _held = false;
}
