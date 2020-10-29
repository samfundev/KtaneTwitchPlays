using System.Collections;
using System;

public class MorseIdentificationComponentSolver : ReflectionComponentSolver
{
	public MorseIdentificationComponentSolver(TwitchModule module) :
		base(module, "MorseIdentificationScript", "!{0} submit <char> [Submits the specified number or letter]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("submit")) yield break;
		if (!split[1].RegexMatch("^[a-z0-9]$")) yield break;
		if (!_component.GetValue<bool>("needyactive"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;

		int current = _component.GetValue<int>("CharacterDisplayNumber");
		int target = Array.IndexOf(_component.GetValue<string[]>("characterdisplaylist"), split[1].ToUpper());
		yield return SelectIndex(current, target, 36, selectables[2], selectables[0]);

		yield return Click(1, 0);
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

			if (_component.GetValue<bool>("needyactive"))
				yield return RespondToCommandInternal("submit " + _component.GetValue<string[]>("characterdisplaylist")[_component.GetValue<int>("MorseDisplayNumber")]);
		}
	}
}