using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class PlungerButtonComponentSolver : ComponentSolver
{
	public PlungerButtonComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = Module.BombComponent.GetComponent(ComponentType);
		selectable = Module.BombComponent.GetComponent<KMSelectable>().Children[0];
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Hold the button by using !{0} hold on 0, and release the button by using !{0} release on 0");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant();
		var match = Regex.Match(inputCommand, "^(hold|release) on ([0-9])$");
		if (!match.Success)
			yield break;
		yield return null;
		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		while (Mathf.FloorToInt(timerComponent.TimeRemaining % 60 % 10) != int.Parse(match.Groups[2].Value))
			yield return $"trycancel The Plunger button was not {(_component.GetValue<bool>("pressed") ? "released" : "pressed")} due to a request to cancel.";
		if (match.Groups[1].Value == "hold")
			DoInteractionStart(selectable);
		else
			DoInteractionEnd(selectable);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var timer = Module.Bomb.Bomb.GetTimer();
		if (_component.GetValue<bool>("pressed"))
		{
			_component.SetValue("pressed", false);
			var animator = _component.GetValue<Animator>("buttonAnimation");
			Module.GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, Module.transform);
			selectable.AddInteractionPunch();
			animator.SetBool("press", false);
			animator.SetBool("release", true);
		}
		yield return null;
		yield return RespondToCommandInternal("hold on " + _component.GetValue<int>("targetPressTime"));
		while (!_component.GetValue<bool>("pressed")) ;
		yield return RespondToCommandInternal("release on " + _component.GetValue<int>("targetReleaseTime"));
	}

	static PlungerButtonComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("plungerButtonScript");
	}

	private static readonly Type ComponentType;
	private readonly object _component;

	private readonly KMSelectable selectable;
}
