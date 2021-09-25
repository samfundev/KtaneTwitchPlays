using System;
using System.Collections;
using System.Linq;

public class PressTheShapeComponentSolver : ReflectionComponentSolver
{
	public PressTheShapeComponentSolver(TwitchModule module) :
		base(module, "PressTheShape", "!{0} press <shape> [Presses the button with the specified shape] | Valid shapes are triangle, square, and circle")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		if (!_shapes.Contains(split[1])) yield break;

		yield return null;
		yield return Click(Array.IndexOf(_shapes, split[1]), 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		int correct = _component.GetValue<int>("CorrectShape");
		if (correct == 1)
			correct = 2;
		else if (correct == 2)
			correct = 1;
		yield return Click(correct, 0);
	}

	private readonly string[] _shapes = new string[] { "triangle", "circle", "square" };
}