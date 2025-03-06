using System.Collections;
using System.Linq;
using UnityEngine;

[ModuleID("rapidButtons")]
public class RapidButtonsComponentSolver : ReflectionComponentSolver
{
	public RapidButtonsComponentSolver(TwitchModule module) :
		base(module, "rapidButtonsScript", "!{0} press <pos> [Presses the button in the specified position] | Valid positions are 1-3 in reading order")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		if (!int.TryParse(split[1], out int check)) yield break;
		if (check < 1 || check > 3) yield break;
		int[] indexes = new int[3];
		int ct = 0;
		for (int i = 0; i < Selectables.Length; i++)
		{
			if (Selectables[i].gameObject.activeSelf)
			{
				indexes[ct] = i;
				ct++;
			}
		}
		if (ct != 3)
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}
		yield return null;
		int[] realPositions = new int[] { 5, 4, 0, 3, 1, 2, 9, 6, 7, 11, 8, 10, 13, 14, 12, 16, 15, 17 };
		ct = 0;
		for (int j = 0; j < realPositions.Length; j++)
		{
			if (indexes.Contains(realPositions[j]))
			{
				ct++;
				if (check == ct)
				{
					yield return Click(realPositions[j], 0);
					break;
				}
			}
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

			object[] corrButtons = _component.GetValue<object[]>("correctButtons");
			if (Selectables.Any(x => x.gameObject.activeSelf))
			{
				yield return null;
				corrButtons[Random.Range(0, corrButtons.Length)].GetValue<KMSelectable>("selectable").OnInteract();
			}
		}
	}
}