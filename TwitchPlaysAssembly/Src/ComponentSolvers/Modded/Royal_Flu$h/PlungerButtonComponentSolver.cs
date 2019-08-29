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
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Hold and release the button by using !{0} hold on 0; release on 0");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant();
		var match = Regex.Match(inputCommand, "^(hold|release) on ([0-9])$");
		var chainMatch = Regex.Match(inputCommand, "^(hold on|submit) ([0-9])(?:;|,|)( |)(?:release on |)([0-9])$");
		if (!match.Success && !chainMatch.Success)
			yield break;
		yield return null;
		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		var time = match.Success ? match.Groups[2].Value : chainMatch.Groups[2].Value;
		while (Mathf.FloorToInt(timerComponent.TimeRemaining % 60 % 10) != int.Parse(time))
			yield return $"trycancel The Plunger button was not {(_component.GetValue<bool>("pressed") ? "released" : "pressed")} due to a request to cancel.";
		if (match.Groups[1].Value == "hold")
			DoInteractionStart(selectable);
		else if (chainMatch.Success)
		{
			DoInteractionStart(selectable);
			time = chainMatch.Groups[3].Value;
			while (Mathf.FloorToInt(timerComponent.TimeRemaining % 60 % 10) != int.Parse(time))
				yield return $"trycancel The Punger button was not released due to a request to cancel.";
			DoInteractionEnd(selectable);
		}
		else
			DoInteractionEnd(selectable);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		forcedSolve = true;
		idColor = Module.ClaimedUserMultiDecker.color;
		var disco = Disco();
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
		Module.StartCoroutine(disco);
		yield return RespondToCommandInternal("hold on " + _component.GetValue<int>("targetPressTime"));
		Module.StopCoroutine(disco);
		Module.SetBannerColor(idColor);
		yield return RespondToCommandInternal("release on " + _component.GetValue<int>("targetReleaseTime"));
	}

	IEnumerator Disco()
	{
		while (true)
		{
			int index = UnityEngine.Random.Range(0, colors.Length);
			Module.SetBannerColor(colors[index]);
			yield return new WaitForSeconds(0.125f);
		}
	}

	static PlungerButtonComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("plungerButtonScript");
	}

	private static readonly Type ComponentType;
	private readonly object _component;

	private readonly KMSelectable selectable;
	private Color[] colors = new[] { Color.blue, brown, Color.green, Color.grey, lime, orange, Color.magenta, Color.red, Color.white, Color.yellow  };
	static Color brown = new Color(165 / 255f, 42 / 255f, 42 / 255f), lime = new Color(50/255f, 205/255f, 50/255f), orange = new Color(1, 0.5f, 0);
	private bool forcedSolve = false;
	private Color idColor;
}
