using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ModuleID("simonsOnFirst")]
public class SimonsOnFirstComponentSolver : ComponentSolver
{
	public SimonsOnFirstComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		objects = Module.BombComponent.GetComponent<KMSelectable>().Children.ToList();

		// module.enabled is set to false when TP is testing a module for TP support.
		// Both of the functions below depend on actually having the module or they'll throw an exception.
		if (module.enabled)
		{
			AssignNumbers();
		}
		SetHelpMessage("Use '!{0} press <buttons>' to press the buttons. Valid button formats are: Directional: T, TR, R, BR, B, BL, L, TL; Colours (Use lime for light green and green for dark green.)");
	}

	private void GetValues()
	{
		numbers.Clear();
		for (int i = 0; i < 8; i++)
		{
			numbers.Add(objects[0]);
		}

		object[] buttons = _component.GetValue<object[]>("buttons");

		if (realNumberList.Count > 0)
		{
			realNumberList.Clear();
		}

		foreach (object item in buttons)
		{
			realColourList.Add(item.GetValue<string>("colour"));
			realNumberList.Add(item.GetValue<string>("number"));
		}
	}

	private void AssignNumbers()
	{
		GetValues();
		for (int i = 0; i < 8; i++)
		{
			numbers[int.Parse(realNumberList[i]) - 1] = objects[i];
		}
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] chars = inputCommand.ToLowerInvariant().Replace("press ", "").SplitFull(' ', ';', ',');
		List<KMSelectable> buttons = new List<KMSelectable>();
		buttons.Clear();
		foreach (string item in chars)
		{
			var index = realColourList.IndexOf(item.Replace("green", "dark green").Replace("lime", "light green"));
			if (index != -1)
			{
				buttons.Add(objects[index]);
				continue;
			}

			switch (item)
			{
				case "t":
					buttons.Add(objects[4]);
					break;
				case "tr":
					buttons.Add(objects[5]);
					break;
				case "r":
					buttons.Add(objects[6]);
					break;
				case "br":
					buttons.Add(objects[7]);
					break;
				case "b":
					buttons.Add(objects[0]);
					break;
				case "bl":
					buttons.Add(objects[1]);
					break;
				case "l":
					buttons.Add(objects[2]);
					break;
				case "tl":
					buttons.Add(objects[3]);
					break;
				default:
					yield return "sendtochaterror Button not valid!";
					yield break;
			}
		}

		yield return null;
		foreach (KMSelectable btntopress in buttons)
		{
			yield return DoInteractionClick(btntopress, .25f);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!_component.GetValue<bool>("moduleSolved"))
		{
			while (_component.GetValue<bool>("checking"))
				yield return true;

			AssignNumbers();
			char[] correctSequence = _component.GetValue<string>("correctSequence").ToCharArray();
			int start = _component.GetValue<int>("numberOfPresses");
			for (int i = start; i < correctSequence.Length; i++)
				yield return DoInteractionClick(numbers[correctSequence[i] - '0' - 1], .25f);
		}
	}

	private readonly List<KMSelectable> objects = new List<KMSelectable>();
	private readonly List<KMSelectable> numbers = new List<KMSelectable>();
	private readonly static Type ComponentType = ReflectionHelper.FindType("SimonsOnFirstScript");
	private readonly Component _component;
	private readonly List<string> realColourList = new List<string>();
	private readonly List<string> realNumberList = new List<string>();
}