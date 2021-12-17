using System;
using System.Collections;
using System.Linq;

public class TechSupportComponentSolver : ReflectionComponentSolver
{
	public TechSupportComponentSolver(TwitchModule module) :
		base(module, "TechSupport", "!{0} <0/1/2> [Presses the button with the specified label] | Presses can be chained with or without spaces")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		char[] btns = { '0', '1', '2' };
		string inputs = command.Replace(" ", "");
		foreach (char c in inputs)
			if (!btns.Contains(c)) yield break;
		if (Module.BombComponent.GetComponent<NeedyComponent>().State != NeedyComponent.NeedyStateEnum.Running || _component.GetValue<bool>("moduleResolved"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		foreach (char c in inputs)
			yield return Click(Array.IndexOf(btns, c));
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running || _component.GetValue<bool>("moduleResolved"))
			{
				yield return true;
				continue;
			}

			yield return null;
			if (_component.GetValue<IList>("options").Count == 2)
				yield return Click(0);
			int curIndex = _component.GetValue<int>("selectedOption");
			int answer = -1;
			while (!_component.GetValue<bool>("moduleResolved"))
			{
				var temp = _component.GetValue<IList>("options")[0];
				if (temp.GetValue<string>("A") == "Version 1")
					answer = _component.CallMethod<int>("CorrectVersion", _component.GetValue<object>("errorData"));
				else if (temp.GetValue<string>("A") == "prle.cba")
					answer = _component.CallMethod<int>("CorrectPatchFile", _component.GetValue<object>("errorData"));
				else
					answer = _component.CallMethod<int>("CorrectParameter", _component.GetValue<object>("errorData"));
				while (curIndex < answer)
				{
					curIndex++;
					yield return Click(2);
				}
				while (curIndex > answer)
				{
					curIndex--;
					yield return Click(1);
				}
				yield return Click(0);
				curIndex = 0;
			}
		}
	}
}