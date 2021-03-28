using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
			GetValues();
			AssignNumbers();
		}
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Use '!{0} press <buttons>' to press the buttons. Valid button formats are: Directional: T, TR, R, BR, B, BL, L, TL; The numbers associated with each button; Colours (Use lime for light green and green for dark green.)");
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
		AssignNumbers();
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
				case "1":
					buttons.Add(numbers[0]);
					break;
				case "2":
					buttons.Add(numbers[1]);
					break;
				case "3":
					buttons.Add(numbers[2]);
					break;
				case "4":
					buttons.Add(numbers[3]);
					break;
				case "5":
					buttons.Add(numbers[4]);
					break;
				case "6":
					buttons.Add(numbers[5]);
					break;
				case "7":
					buttons.Add(numbers[6]);
					break;
				case "8":
					buttons.Add(numbers[7]);
					break;
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
			yield return DoInteractionClick(btntopress);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!_component.GetValue<bool>("moduleSolved"))
		{
			while (_component.GetValue<bool>("checking"))
				yield return true;

			string correctSequence = _component.GetValue<string>("correctSequence").ToCharArray().Join();
			yield return RespondToCommandInternal($"press {correctSequence}");
		}
	}

	private readonly List<KMSelectable> objects = new List<KMSelectable>();
	private readonly List<KMSelectable> numbers = new List<KMSelectable>();
	private readonly static Type ComponentType = ReflectionHelper.FindType("SimonsOnFirstScript");
	private readonly Component _component;
	private readonly List<string> realColourList = new List<string>();
	private readonly List<string> realNumberList = new List<string>();
}
