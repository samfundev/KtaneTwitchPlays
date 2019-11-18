using System.Collections;

public class MemorableButtonsComponentSolver : ComponentSolver
{
	public MemorableButtonsComponentSolver(TwitchModule module) :
		base(module)
	{
		buttons = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press the x button !{0} press x; Buttons can be: 1; 2; 3; 4; TL; TR; BL; BR (The buttons are numbered in reading order.)");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string command = inputCommand.ToLowerInvariant().Replace("press ", "");
		KMSelectable btn;
		switch (command)
		{
			case "1":
			case "TL":
				btn = buttons[2];
				break;
			case "2":
			case "TR":
				btn = buttons[3];
				break;
			case "3":
			case "BL":
				btn = buttons[0];
				break;
			case "4":
			case "BR":
				btn = buttons[1];
				break;
			default:
				yield return $"sendtochaterror {command} is not a valid button.";
				yield break;
		}
		yield return null;
		yield return DoInteractionClick(btn);
	}

	private KMSelectable[] buttons;
}