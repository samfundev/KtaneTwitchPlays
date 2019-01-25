using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class AlchemyComponentSolver : ComponentSolver
{
	public AlchemyComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press buttons using !{0} press <buttons>. Runes are specified directionally. Frequencies are specified by full name. Other buttons: redraw, submit and clear.");
	}

	readonly Dictionary<string, int> buttonMap = new Dictionary<string, int>()
	{
		{ "s", 0 },
		{ "redraw", 1 },
		{ "re-draw", 1 },
		{ "draw", 1 },
		{ "rd", 1 },
		{ "d", 1 },
		{ "br", 2 },
		{ "r", 3 },
		{ "tr", 4 },
		{ "tl", 5 },
		{ "l", 6 },
		{ "bl", 7 },
		{ "mind", 8 },
		{ "flames", 9 },
		{ "matter", 10 },
		{ "energy", 11 },
		{ "life", 12 },
		{ "clear", 13 },
		{ "cl", 13 },
	};

	string SimplifyButtonName(string buttonName)
	{
		buttonName = buttonName.Replace("middle", "center");
		foreach (string direction in new[] { "left", "right", "top", "bottom", "center", "centre", "submit" })
			buttonName = buttonName.Replace(direction, direction[0].ToString());

		return Regex.Replace(buttonName, "([lr])([tb])", "$2$1");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push)", "");
		string[] split = inputCommand.Split(new[] { ' ', ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);

		if (split.Length >= 1)
		{
			List<int> buttonIndexes = new List<int>();
			foreach (string name in split)
			{
				if (!buttonMap.TryGetValue(SimplifyButtonName(name), out int index))
					yield break;

				buttonIndexes.Add(index);
			}

			yield return null;
			foreach (int index in buttonIndexes)
				yield return DoInteractionClick(selectables[index]);
		}
	}

	private readonly KMSelectable[] selectables;
}
