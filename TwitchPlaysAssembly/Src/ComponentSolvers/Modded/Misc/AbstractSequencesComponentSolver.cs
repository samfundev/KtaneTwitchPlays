using System;
using System.Collections;
using System.Linq;
using UnityEngine;

[ModuleID("abstractSequences")]
public class AbstractSequencesComponentSolver : ReflectionComponentSolver
{
	public AbstractSequencesComponentSolver(TwitchModule module) :
		base(module, "abstractSequencesScript", "!{0} press <p1> (p2)... [Presses the numbered button(s) in the specified position(s)] | !{0} submit [Presses the submit button] | Valid positions are 1-16 in reading order")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("submit"))
		{
			if (!_component.GetValue<bool>("canClickAgain")) yield break;

			yield return null;
			yield return "solve";
			yield return "strike";
			yield return Click(16, 0);
		}
		else if (command.StartsWith("press ") && split.Length >= 2)
		{
			for (int i = 1; i < split.Length; i++)
			{
				if (!int.TryParse(split[i], out int check)) yield break;
				if (!check.InRange(1, 16)) yield break;
			}
			if (!_component.GetValue<bool>("canClickAgain")) yield break;

			yield return null;
			for (int i = 1; i < split.Length; i++)
				yield return Click(int.Parse(split[i]) - 1);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		// Get module values
		var bombInfo = _component.GetValue<KMBombInfo>("BombInfo");
		ArrayList finalSeq = _component.GetValue<ArrayList>("finalSequence");
		ArrayList input = _component.GetValue<ArrayList>("numbers");
		int[] btnNums = _component.GetValue<int[]>("buttonNumbers");
		// Fill an ArrayList with the displayed numbers that need to be pressed
		ArrayList answer = new ArrayList();
		for (int i = 0; i < 16; i++)
		{
			if (!finalSeq.Contains(btnNums[i]))
				answer.Add(btnNums[i]);
		}
		// Sort the numbers and determine if the sequence should be inputted in reverse or not by current minute count
		bool waiter = false;
		answer.Sort();
		if ((int) bombInfo.GetTime() / 60 % 2 == 1)
		{
			answer.Reverse();
			waiter = true;
		}
		// Convert the answer ArrayList into an ArrayList of button positions for comparison with the input ArrayList
		ArrayList temp = new ArrayList();
		for (int i = 0; i < answer.Count; i++)
			temp.Add(Array.IndexOf(btnNums, answer[i]));
		// If the module is currently submitting and the inputted sequence is wrong, stop everything to prevent a strike
		if (!_component.GetValue<bool>("canClickAgain") && !temp.ToArray().SequenceEqual(input.ToArray()))
		{
			((MonoBehaviour) _component).StopAllCoroutines();
			yield break;
		}
		// Otherwise if the module is not submitting...
		else if (_component.GetValue<bool>("canClickAgain"))
		{
			// Check if the current input does not match the what needs to be inputted
			if (input.Count > answer.Count)
				yield break;
			for (int i = 0; i < input.Count; i++)
			{
				if (Array.IndexOf(btnNums, answer[i]) != (int) input[i])
					yield break;
			}
			// Go through all numbers that need to be inputted and if they are not already pressed, press them
			for (int i = 0; i < answer.Count; i++)
			{
				if (!input.Contains(Array.IndexOf(btnNums, answer[i])))
				{
					for (int j = 0; j < 16; j++)
					{
						if (btnNums[j] == int.Parse(answer[i].ToString()))
						{
							yield return Click(j);
							break;
						}
					}
				}
			}
			// Wait if the current minute parity is not what we calculated for
			while ((int) bombInfo.GetTime() / 60 % 2 != (waiter ? 1 : 0)) yield return true;
			// Press submit
			yield return Click(16, 0);
		}
		// Wait until the module solves so the status light does not turn on early and no other mods solve while submitting that could change the answer
		while (!_component.GetValue<bool>("moduleSolved")) yield return null;
	}
}