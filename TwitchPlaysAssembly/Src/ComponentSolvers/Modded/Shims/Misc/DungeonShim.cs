using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

[ModuleID("dungeon")]
internal class DungeonShim : ComponentSolverShim
{
	private enum ActionType
	{ Sword = 1, Shield = 2, Left = 3, Right = 4, Forward = 5 }
	private readonly object _component;
	private static readonly Type ComponentType = ReflectionHelper.FindType("DungeonScript");
	private readonly KMSelectable[] buttons;
	private readonly KMSelectable leftButton, rightButton, forwardButton;

	public DungeonShim(TwitchModule module) : base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		leftButton = GetButtonObj("buttonL");
		rightButton = GetButtonObj("buttonR");
		forwardButton = GetButtonObj("buttonFwd");

		buttons = new[]
		{
			GetButtonObj("buttonSw"),
			GetButtonObj("buttonSh"),
			leftButton,
			rightButton,
			forwardButton
		};
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;
		int currentStage = -1;
		//wait until module initalizes
		yield return WaitForStageChange(currentStage);
		currentStage = GetStage();

		while (!IsModuleSolved())
		{
			if (currentStage == 16)
			{
				yield return new WaitUntil(IsModuleSolved);
				break;
			}

			//input direction
			int roll = Math.Abs(_component.GetValue<int>("currentState")) % 10;
			if (roll < 3)
			{
				yield return DoInteractionClick(leftButton);
			}

			else if (roll >= 3 && roll <= 6)
			{
				yield return DoInteractionClick(forwardButton);
			}

			else
			{
				yield return DoInteractionClick(rightButton);
			}

			//check to see if a monster spawns
			yield return true;

			//check if moster is there
			if (_component.GetValue<bool>("inCombat"))
			{
				//see what monster it is
				int[] actionInts = _component.CallMethod<int[]>("Codex", _component.GetValue<int>("currentFight"));

				foreach (int action in actionInts)
				{
					if (Enum.IsDefined(typeof(ActionType), action))
					{
						yield return DoInteractionClick(buttons[action - 1]);
					}
					else
					{
						Debug.LogWarning($"DungeonShim: Unknown action int: {action}");
						yield return "sendtochaterror There was an issue with the autosolver. Contact the developer";
					}
				}

			}

			//wait until new stage is different
			yield return WaitForStageChange(currentStage);
			currentStage = GetStage();
		}

	}

	private KMSelectable GetButtonObj(string variableName)
		=> (KMSelectable) ComponentType.GetField(variableName, BindingFlags.Public | BindingFlags.Instance).GetValue(_component);

	private IEnumerator WaitForStageChange(int previousStage)
		=> new WaitUntil(() => GetStage() != previousStage);

	private int GetStage() => _component.GetValue<int>("stage");

	private bool IsModuleSolved() => _component.GetValue<bool>("moduleSolved");
}
