using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ModuleID("orderedKeys")]
internal class OrderedKeysShim : ReflectionComponentSolverShim
{
	private readonly List<KMSelectable> keys;
	private readonly TwitchBomb bomb;
	private int NewStage => _component.GetValue<int>("stage");
	private bool[] NewAlreadyPressed => _component.GetValue<bool[]>("alreadypressed");
	private bool NewPressable => _component.GetValue<bool>("pressable");
	private bool solveAnimationDone = false;

	public OrderedKeysShim(TwitchModule module) : base(module, "OrderedKeysScript")
	{
		keys = _component.GetValue<List<KMSelectable>>("keys");
		bomb = module.Bomb;

		Module.BombComponent.GetComponent<KMBombModule>().OnPass += () =>
		{
			solveAnimationDone = true;
			return false;
		};
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;
		int currentStrikes = bomb.StrikeCount;
		int stage = NewStage;

		while (true)
		{
			//Wait for the mod to be pressable
			while (!NewPressable)
			{
				yield return true;
			}

			//Check if the module is solvable based on current presses
			List<string> answers = _component.GetValue<List<string>>("answer");
			List<string> presses = _component.GetValue<List<string>>("presses");

			for (int i = 0; i < presses.Count; i++)
			{
				//If not solvable, force solve the module 
				if (presses[i] != answers[i])
				{
					Debug.Log("Module is unsolvable, forcing solve");
					Module.BombComponent.GetComponent<KMBombModule>().HandlePass();
					yield break;
				}
			}

			//If sovlvable, press the remaining buttons in the correct order
			List<int> answerIndex = answers.Select(i => int.Parse(i) - 1).ToList();
			bool[] alreadypressed = NewAlreadyPressed;
			List<int> unpressedIndex = answerIndex.Where(ix => !alreadypressed[ix]).ToList();
			foreach (int ix in unpressedIndex)
			{
				keys[ix].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}

			//If at the last stage, solve
			if (stage == 3)
				break;

			//Increment stage
			while (stage == NewStage)
			{
				yield return true;
			}

			stage = NewStage;
		}

		while (!solveAnimationDone)
		{
			yield return true;
		}
	}
}

