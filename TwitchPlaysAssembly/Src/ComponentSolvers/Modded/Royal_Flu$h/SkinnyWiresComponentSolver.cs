using System;
using System.Collections;
using System.Linq;

public class SkinnyWiresComponentSolver : ComponentSolver
{
	public SkinnyWiresComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} cut <letter><number> [cut the wire going from <letter> to <number>]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim();

		string[] split = inputCommand.SplitFull(' ');

		if (split.Length.EqualsAny(2, 3) && split[0].FirstOrWhole("cut"))
		{
			if (split.Length == 3) split[1] = split.Skip(1).Join("");

			if (split[1].Length != 2) yield break;

			int letter = split[1][0].ToIndex();
			int number = split[1][1].ToIndex();

			var wire = selectables[letter * 3 + number];
			if (!wire.gameObject.activeSelf)
			{
				yield return $"sendtochaterror There is no wire that goes between {split[1][0]} and {split[1][0]}.";
				yield break;
			}

			yield return null;
			yield return DoInteractionClick(selectables[letter * 3 + number]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		foreach (KMSelectable wire in selectables)
		{
			if (!wire.gameObject.activeSelf) continue;

			if (!wire.GetComponent(WireDetailsType).GetValue<bool>("correctWire")) continue;

			yield return RespondToCommandInternal($"cut {wire.gameObject.name.Substring(4)}");
			break;
		}
	}

	static SkinnyWiresComponentSolver()
	{
		WireDetailsType = ReflectionHelper.FindType("WireDetails");
	}

	private static readonly Type WireDetailsType;

	private readonly KMSelectable[] selectables;
}
