using System;
using System.Collections;
using System.Text.RegularExpressions;

[ModuleID("primeEncryption")]
public class PrimeEncryptionComponentSolver : ComponentSolver
{
	public PrimeEncryptionComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = Module.BombComponent.GetComponent(ComponentType);
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		SetHelpMessage("!{0} submit 123 456 [submit 123 and 456 as the bases]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push|submit) ", "");

		string[] split = inputCommand.SplitFull(' ');

		if (split.Length == 2 && int.TryParse(split[0], out int num1) && num1 > 0 && int.TryParse(split[1], out int num2) && num2 > 0)
		{
			yield return null;
			yield return DoInteractionClick(selectables[0]);

			foreach (char character in split[0])
				yield return DoInteractionClick(selectables[character.ToIndex() + 3]);

			yield return DoInteractionClick(selectables[1]);

			foreach (char character in split[1])
				yield return DoInteractionClick(selectables[character.ToIndex() + 3]);

			yield return DoInteractionClick(selectables[1]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		int[] selectedBases = _component.GetValue<int[]>("selectedBases");
		yield return RespondToCommandInternal($"{selectedBases[0]} {selectedBases[1]}");
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("PrimeEncryptionScript");
	private readonly object _component;

	private readonly KMSelectable[] selectables;
}
