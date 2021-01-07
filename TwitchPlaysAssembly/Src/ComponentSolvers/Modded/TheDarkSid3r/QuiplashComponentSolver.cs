using System;
using System.Collections;
using System.Linq;

public class QuiplashComponentSolver : ReflectionComponentSolver
{
	public QuiplashComponentSolver(TwitchModule module) :
		base(module, "QLModule", "!{0} answer <left/right> [Chooses the left or right answer]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("answer ")) yield break;
		if (!_answers.Contains(split[1])) yield break;

		yield return null;
		yield return Click(Array.IndexOf(_answers, split[1]), 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		int correct = _component.GetValue<object>("ChosenConfig").GetValue<int>("Correct");
		yield return Click(correct == 2 ? UnityEngine.Random.Range(0, 2) : correct, 0);
	}

	private readonly string[] _answers = new string[] { "left", "right" };
}