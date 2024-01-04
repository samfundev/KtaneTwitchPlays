using System.Collections;
using System.Linq;

public class SimonsSatireShim : ReflectionComponentSolverShim
{
	public SimonsSatireShim(TwitchModule module)
		: base(module, "SimonsSatireModule", "42069")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_buttons = new KMSelectable[] { _component.GetValue<KMSelectable>("RedButton"), _component.GetValue<KMSelectable>("BlueButton"), _component.GetValue<KMSelectable>("YellowButton"), _component.GetValue<KMSelectable>("GreenButton") };
	}

	protected override IEnumerator RespondShimmed(string[] split, string command)
	{
		if (!command.EqualsAny("left", "right") && split.Length < 3)
		{
			if (!split[0].Equals("press"))
				yield break;

			yield return null;

			if (split.Length < 2)
			{
				yield return "sendtochaterror Parameter length invalid. Command ignored.";
				yield break;
			}

			if (!_validButtons.Contains(split[1]))
			{
				yield return "sendtochaterror Command contains an invalid color. Command ignored.";
				yield break;
			}

			switch (split[1])
			{
				case "red":
				case "r":
					yield return DoInteractionClick(_buttons[0]);
					break;
				case "blue":
				case "b":
					yield return DoInteractionClick(_buttons[1]);
					break;
				case "yellow":
				case "y":
					yield return DoInteractionClick(_buttons[2]);
					break;
				default:
					yield return DoInteractionClick(_buttons[3]);
					break;
			}
		}

		yield return RespondUnshimmed(command);
	}

	private readonly string[] _validButtons = { "red", "blue", "yellow", "green", "r", "b", "y", "g" };
	private readonly KMSelectable[] _buttons;
}
