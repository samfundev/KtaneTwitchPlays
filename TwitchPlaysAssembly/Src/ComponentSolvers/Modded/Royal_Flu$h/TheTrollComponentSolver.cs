using System.Collections;

public class TheTrollComponentSolver : ComponentSolver
{
	public TheTrollComponentSolver(TwitchModule module) :
		base(module)
	{
		trollButton = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press the button x times: !{0} press x; Press the button when the last digit of the timer is x: !{0} press at x");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string command = inputCommand.ToLowerInvariant();
		if (command.StartsWith("press at"))
		{
			command = command.Replace("press at ", "");
			TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
			int timeRemaining = (int) timerComponent.TimeRemaining;
			if (!int.TryParse(command, out int secstopress))
			{
				yield return "sendtochaterror Number not valid!";
				yield break;
			}

			if (!secstopress.InRange(1, 9))
			{
				yield return "sendtochaterror Number is out of range!";
				yield break;
			}

			yield return null;
			while (timeRemaining % 10 != secstopress)
			{
				yield return null;
				yield return "trycancel Button was't pressed due to request to cancel.";
				timeRemaining = (int) timerComponent.TimeRemaining;
			}
			yield return DoInteractionClick(trollButton[0]);
		}
		else if (command.StartsWith("press"))
		{
			command = command.Replace("press ", "");
			if (!int.TryParse(command, out int numtopress))
			{
				yield return "sendtochaterror Number not valid!";
				yield break;
			}

			yield return null;
			for (int i = 0; i < numtopress; i++)
			{
				yield return DoInteractionClick(trollButton[0]);
			}
		}
	}
	private readonly KMSelectable[] trollButton;
}