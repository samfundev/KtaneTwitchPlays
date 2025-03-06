using System;
using System.Collections;
using System.Collections.Generic;

[ModuleID("stainedGlass")]
public class StainedGlassComponentSolver : ComponentSolver
{
	public StainedGlassComponentSolver(TwitchModule module) :
		base(module)
	{
		component = module.BombComponent.GetComponent(componentType);
		buttons = Module.BombComponent.GetComponent<KMSelectable>().Children;
		order = new Dictionary<int, KMSelectable>()
		{
			{11, buttons[0]},
			{21, buttons[1]},
			{22, buttons[2]},
			{31, buttons[3]},
			{32, buttons[4]},
			{33, buttons[5]},
			{41, buttons[6]},
			{42, buttons[7]},
			{43, buttons[8]},
			{44, buttons[9]},
			{51, buttons[10]},
			{52, buttons[11]},
			{53, buttons[12]},
			{54, buttons[13]},
			{55, buttons[14]},
			{61, buttons[15]},
			{62, buttons[16]},
			{63, buttons[17]},
			{64, buttons[18]},
			{71, buttons[19]},
			{72, buttons[20]},
			{73, buttons[21]},
			{81, buttons[22]},
			{82, buttons[23]},
			{91, buttons[24]},
		};
		SetHelpMessage("Press the x button: !{0} press x; Buttons are two digit numbers which refers to row and column in that order. For ex. 32 is row 3 column 2. Buttons can be chained using spaces as separators.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] chars = inputCommand.ToUpper().Replace("PRESS ", "").Split(' ');
		List<KMSelectable> btns = new List<KMSelectable>();
		foreach (string input in chars)
		{
			if (!int.TryParse(input, out int ind))
			{
				yield return "sendtochaterror Number not valid!";
				yield break;
			}

			if (!order.ContainsKey(ind))
			{
				yield return "sendtochaterror Position not valid!";
				yield break;
			}

			btns.Add(order[ind]);
		}

		yield return null;
		foreach (KMSelectable btntopress in btns)
		{
			yield return DoInteractionClick(btntopress);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		object[] panes = component.GetValue<object[]>("pane");

		for (int i = 0; i < 25; i++)
		{
			if (!panes[i].GetValue<bool>("broken") && panes[i].GetValue<bool>("toBreak"))
				yield return DoInteractionClick(buttons[i]);
		}
	}

	private static readonly Type componentType = ReflectionHelper.FindType("StainedGlassScript");

	private readonly object component;

	private readonly KMSelectable[] buttons;
	private readonly Dictionary<int, KMSelectable> order;
}