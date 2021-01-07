using System.Collections;
using UnityEngine;

public class NeedyMrsBobComponentSolver : ReflectionComponentSolver
{
	public NeedyMrsBobComponentSolver(TwitchModule module) :
		base(module, "needyMrsBobScript", "!{0} send <pos> [Sends the emoji in the specified position] | Valid positions are 1-24 in reading order")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("send ")) yield break;
		if (!int.TryParse(split[1], out _)) yield break;
		if (int.Parse(split[1]) < 1 || int.Parse(split[1]) > 24) yield break;
		if (!_component.GetValue<GameObject>("responses").activeSelf)
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		yield return Click(int.Parse(split[1]) - 1, 0);
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

			while (!_component.GetValue<GameObject>("responses").activeSelf) { yield return null; }
			int index = -1;
			object[] emojis = _component.GetValue<object[]>("emojiNames");
			string correctEmoji = _component.GetValue<string>("correctAnswer");
			for (int i = 0; i < 24; i++)
			{
				if (emojis[i].GetValue<string>("emojiName") == correctEmoji)
				{
					index = i;
					break;
				}
			}
			yield return RespondToCommandInternal("send " + (index + 1));
		}
	}
}