using System.Collections;
using System;
using System.Linq;

public class OverKiloComponentSolver : ReflectionComponentSolver
{
	public OverKiloComponentSolver(TwitchModule module) :
		base(module, "OverKiloModule", "!{0} press <left/right/ok> [Presses the left button, the right button, or the \"Over Kilo\" button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		string[] btnTypes = { "left", "ok", "right" };
		if (!btnTypes.Contains(split[1])) yield break;

		yield return null;
		yield return Click(Array.IndexOf(btnTypes, split[1]), 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		while (_component.GetValue<int>("curPage") == 0) yield return true;
		while (!Module.BombComponent.IsSolved)
		{
			if (_component.GetValue<float>("sum") == 0f)
				yield return Click((UnityEngine.Random.Range(0, 2) == 1) ? 2 : 0);
			else if ((_component.GetValue<float>("sum") + _component.GetValue<int>("currentNumber") + _component.GetValue<int>("tempAdder")) * _component.GetValue<float>("multiplier") >= 1000f)
				yield return Click(1, 0);
			else if (_component.GetValue<int>("currentNumber") > _component.GetValue<int>("previousNumber"))
			{
				if (_component.GetValue<int>("curPage") == 8)
					yield return Click(2, 0);
				else
					yield return Click(2);
			}
			else
			{
				if (_component.GetValue<int>("curPage") == 8)
					yield return Click(0, 0);
				else
					yield return Click(0);
			}
		}
	}
}