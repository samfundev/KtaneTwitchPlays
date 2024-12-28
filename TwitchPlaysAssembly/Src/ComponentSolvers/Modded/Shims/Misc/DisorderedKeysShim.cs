using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

internal class DisorderedKeysShim : ComponentSolverShim
{
	private static readonly Type ComponentType = ReflectionHelper.FindType("DisorderedKeysScript");
	private static readonly FieldInfo ButtonsField = ComponentType.GetField("keys", BindingFlags.Public | BindingFlags.Instance);
	private readonly object _component;
	private readonly List<KMSelectable> keys;
	private readonly TwitchBomb bomb;

	public DisorderedKeysShim(TwitchModule module) : base(module)
	{
		bomb = module.Bomb;
		_component = module.BombComponent.GetComponent(ComponentType);
		keys = (List<KMSelectable>) ButtonsField.GetValue(_component);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		int[] quriks = _component.GetValue<int[]>("quirk");
		bool[] revealedKeys = _component.GetValue<bool[]>("revealed");
		List<int> values = _component.GetValue<List<int>>("valueList");

		//check is the button that needs be pressed next requires the timer
		List<bool> timerRules = new List<bool>();
		//separates the keys based on their quirks
		List<int>[] keyQuirkList = Enumerable.Range(0, 5).Select(_ => new List<int>()).ToArray();
		List<int> keyIndicesToPress = new List<int>();

		//press all the unrevealed keys that are not false 
		//add all the keys to a specific list based on their quirk
		for (int i = 0; i < quriks.Length; i++)
		{
			if (quriks[i] != 4 && !revealedKeys[i])
			{
				keys[i].OnInteract();
			}

			switch (quriks[i])
			{

				case 2:
					keyQuirkList[0].Add(i); //first
					break;

				case 0:
				case 5:
					keyQuirkList[1].Add(i); //none / sequence
					break;

				case 1:
					keyQuirkList[2].Add(i); //time
					break;

				case 3:
					keyQuirkList[3].Add(i); //last
					break;

				case 4:
					keyQuirkList[4].Add(i); //false
					break;
			}
		}

		//wait for all of the animations to finish
		yield return new WaitForSeconds(0.1f);

		for (int i = 0; i < keyQuirkList.Length; i++)
		{
			List<int> list = keyQuirkList[i];
			if (list.Count > 0)
			{
				keyIndicesToPress.AddRange(list.OrderBy(keyIndex => values[keyIndex]));
				//only make a timer rule if the quirk is time
				timerRules.AddRange(Enumerable.Repeat(i == 2, list.Count));
			}
		}

		for (int i = 0; i < keyIndicesToPress.Count; i++)
		{
			int buttonIndex = keyIndicesToPress[i];
			if (timerRules[i])
			{
				int value = values[buttonIndex];
				if (value == 6)
				{
					while (!DigitsMatch())
					{
						yield return true;
					}
				}
				else
				{
					while (!DigitsMatchValue(value))
					{
						yield return true;
					}
				}
			}

			keys[buttonIndex].OnInteract();
		}

		while (!_component.GetValue<bool>("moduleSolved"))
		{
			yield return true;
		}
	}

	private bool DigitsMatchValue(int value)
	{
		string seconds = bomb.GetFullFormattedTime.Split(':').Last();
		return Math.Abs(seconds[0] - seconds[1]) == value;
	}

	private bool DigitsMatch()
	{
		string seconds = bomb.GetFullFormattedTime.Split(':').Last();
		return seconds[0] == seconds[1];
	}
}
