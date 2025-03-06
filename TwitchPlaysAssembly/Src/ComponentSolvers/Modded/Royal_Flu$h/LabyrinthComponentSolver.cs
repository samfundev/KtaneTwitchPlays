using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

[ModuleID("labyrinth")]
public class LabyrinthComponentSolver : ComponentSolver
{
	public LabyrinthComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		SetHelpMessage("Move around the labyrinth using !{0} move <directions>. Directions must be abbreviated.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push|move) ", "");

		var buttonIndexes = inputCommand.Replace(" ", "").ToCharArray().Select(character => "dulr".IndexOf(character)).ToArray();

		if (buttonIndexes.All(index => index.InRange(0, 3)))
		{
			yield return null;
			foreach (int index in buttonIndexes)
			{
				yield return DoInteractionClick(selectables[index]);
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type labyrinthScriptType = ReflectionHelper.FindType("labyrinthScript");
		if (labyrinthScriptType == null) yield break;

		object component = Module.BombComponent.GetComponent(labyrinthScriptType);

		Type PositionRulesType = ReflectionHelper.FindType("PositionRules");

		Stack<object> exploredPath = new Stack<object>();
		Stack<string> solutionPath = new Stack<string>();

		string[] directions = new[] { "Up", "Left", "Right", "Down" };
		bool explorePosition(object position)
		{
			if (exploredPath.Contains(position))
				return false;

			if (position.GetValue<bool>("isTarget"))
				return true;

			exploredPath.Push(position);
			foreach (string direction in directions)
			{
				if (position.GetValue<bool>($"canMove{direction}"))
				{
					bool foundTarget = explorePosition(position.GetValue<Renderer>($"to{direction}").GetComponent(PositionRulesType));
					if (foundTarget)
					{
						solutionPath.Push(direction);
						return true;
					}
				}
			}

			exploredPath.Pop();
			return false;
		}

		object currentLevelInfo = component.GetValue<object>("currentLevelInfo");
		object getActive() => currentLevelInfo.GetValue<Renderer[]>("level1Indicators").Select(renderer => renderer.GetComponent(PositionRulesType)).FirstOrDefault(position => position.GetValue<bool>("isActive"));
		yield return null;
		while (!component.GetValue<bool>("moduleSolved"))
		{
			object activePosition = getActive();
			if (!explorePosition(activePosition))
				yield break;

			if (solutionPath.Count == 0) // There have been cases where we were standing on top of a portal, not sure why but this handles that.
			{
				foreach (string direction in directions)
				{
					if (activePosition.GetValue<bool>($"canMove{direction}"))
					{
						yield return RespondToCommandInternal(direction[0].ToString());
						break;
					}
				}

				continue;
			}

			yield return RespondToCommandInternal(solutionPath.Select(direction => direction[0]).Join(""));

			solutionPath.Clear();
			exploredPath.Clear();
			currentLevelInfo = component.GetValue<object>("currentLevelInfo");
		}
	}

	private readonly KMSelectable[] selectables;
}
