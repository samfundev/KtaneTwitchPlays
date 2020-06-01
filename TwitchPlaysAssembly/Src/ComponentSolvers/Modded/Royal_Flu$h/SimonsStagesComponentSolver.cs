using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class SimonsStagesComponentSolver : ComponentSolver
{
	public SimonsStagesComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = Module.BombComponent.GetComponent(ComponentType);
		lightDevices = _component.GetValue<object[]>("lightDevices");
		colorOrder = lightDevices.Select(device => device.GetValue<TextMesh>("lightText").text[0]).ToArray();
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} press <letters> [press a sequence of colors based on their first letter]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToUpperInvariant().Trim(), "^(press|hit|enter|push) ", "", RegexOptions.IgnoreCase);

		if (!inputCommand.RegexMatch("^[RBYOMGPLCW ]+$"))
			yield break;

		yield return null;
		foreach (char character in inputCommand.Replace(" ", ""))
		{
			while (_component.GetValue<bool>("moduleLocked"))
				yield return true;

			yield return DoInteractionClick(selectables[Array.IndexOf(colorOrder, character)]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
        yield return null;
        while (!_component.GetValue<bool>("readyToSolve"))
            yield return true;

        // If any past stages were incorrect, we can't solve the module.
        List<bool> lightsSolved = _component.GetValue<List<bool>>("lightsSolved");
        if (Enumerable.Range(0, _component.GetValue<int>("totalPresses")).Any(index => !lightsSolved[index]))
			yield break;

		yield return RespondToCommandInternal(_component.GetValue<List<string>>("solutionNames").Select(color => color[0]).Join());
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("SimonsStagesScript", "simonsStages");
	private readonly object _component;

	private readonly object[] lightDevices;
	private readonly char[] colorOrder;

	private readonly KMSelectable[] selectables;
}
