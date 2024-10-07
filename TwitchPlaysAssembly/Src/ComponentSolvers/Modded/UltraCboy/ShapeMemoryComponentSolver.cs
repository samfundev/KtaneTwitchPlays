using System.Collections;

public class ShapeMemoryComponentSolver : ReflectionComponentSolver
{
	public ShapeMemoryComponentSolver(TwitchModule module) :
		base(module, "NeedyShapeMemoryScript", "!{0} yes [press the green yes button] | !{0} no [press the red no button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if ("yes".StartsWith(command))
		{
			yield return null;
			yield return Click(0);
		}
		else if ("no".StartsWith(command))
		{
			yield return null;
			yield return Click(1);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running)
			{
				yield return true;
				continue;
			}

			var sameAsPrevious = _component.GetValue<int>("currentShape") == _component.GetValue<int>("previousShape");
			yield return RespondToCommandInternal(sameAsPrevious ? "yes" : "no");
			yield return true;
		}
	}
}
