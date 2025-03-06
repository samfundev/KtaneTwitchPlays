using System.Collections;

[ModuleID("needycrafting")]
public class CraftingTableComponentSolver : ReflectionComponentSolver
{
	public CraftingTableComponentSolver(TwitchModule module) :
		base(module, "CraftingTableScript", "!{0} <button> [Presses the specified button (can chain with spaces)] | Valid buttons are stick, wood, cobble, iron, gold, diamond, reset and 1-9 for the cells of the crafting grid in reading order")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		for (int i = 0; i < split.Length; i++)
		{
			if (!split[i].EqualsAny("stick", "wood", "cobble", "cobblestone", "iron", "gold", "diamond", "reset", "1", "2", "3", "4", "5", "6", "7", "8", "9"))
				yield break;
		}
		if (Module.BombComponent.GetComponent<NeedyComponent>().State != NeedyComponent.NeedyStateEnum.Running)
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		for (int i = 0; i < split.Length; i++)
		{
			switch (split[i])
			{
				case "stick":
					yield return Click(0);
					break;
				case "wood":
					yield return Click(1);
					break;
				case "cobble":
				case "cobblestone":
					yield return Click(2);
					break;
				case "diamond":
					yield return Click(3);
					break;
				case "gold":
					yield return Click(4);
					break;
				case "iron":
					yield return Click(5);
					break;
				case "reset":
					yield return Click(15);
					break;
				default:
					yield return Click(int.Parse(split[i]) + 5);
					break;
			}
		}
	}
}