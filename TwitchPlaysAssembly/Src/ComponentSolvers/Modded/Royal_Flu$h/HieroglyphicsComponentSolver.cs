using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class HieroglyphicsComponentSolver : ComponentSolver
{
	public HieroglyphicsComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} <anubis> <horus> <digit> [set the anubis and horus lock positions then submit when timer matches the digit]");
	}

	readonly string[] positions = new[] { "left", "center", "right" };
	bool GetPosition(string target, out int index)
	{
		target = target.Replace("middle", "center").Replace("centre", "center");
		index = positions.IndexOf(position => target.FirstOrWhole(position));
		return index != -1;
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (split.Length == 3 && GetPosition(split[0], out int anubis) && GetPosition(split[1], out int horus) && int.TryParse(split[2], out int digit))
		{
			yield return null;

			int[] lockPositions = new[] { anubis, horus };
			int[] pointerMoves = _component.GetValue<int[]>("pointerMoves");
			for (int i = 0; i < 2; i++)
			{
				if (lockPositions[i] == 1 && pointerMoves[i].EqualsAny(1, 3)) continue;

				int presses = (lockPositions[i] - pointerMoves[i]).Mod(4);
				for (int j = 0; j < presses; j++)
				{
					selectables[i].OnInteract();
					while (_component.GetValue<bool>("moving"))
						yield return true;
				}
			}

			while (Mathf.FloorToInt(Module.Bomb.CurrentTimer) % 10 != digit)
				yield return true;
			selectables[2].OnInteract();
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return RespondToCommandInternal($"{_component.GetValue<int[]>("correctLockPosition").Select(index => positions[index]).Join()} {(int) _component.GetValue<float>("correctPressTime")}");
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("hieroglyphicsScript");
	private readonly object _component;

	private readonly KMSelectable[] selectables;
}
