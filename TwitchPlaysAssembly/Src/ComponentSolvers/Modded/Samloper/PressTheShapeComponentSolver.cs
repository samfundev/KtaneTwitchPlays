using System;
using System.Collections;
using System.Linq;

public class PressTheShapeComponentSolver : ReflectionComponentSolver
{
	public PressTheShapeComponentSolver(TwitchModule module) :
		base(module, "pressTheShape", "!{0} press <shape> [Presses the button with the specified shape] | Valid shapes are triangle, square, and circle")
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
		_component.GetValue<KMSelectable>("correctButton").OnInteract();
	}

	private readonly string[] _shapes = new string[] { "triangle", "square", "circle" };
}