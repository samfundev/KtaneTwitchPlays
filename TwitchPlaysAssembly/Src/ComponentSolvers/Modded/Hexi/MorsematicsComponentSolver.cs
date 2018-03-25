using System.Collections;
using System.Text.RegularExpressions;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class MorsematicsComponentSolver : ComponentSolverShim
{
	public MorsematicsComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant();
		if (inputCommand.RegexMatch(out Match match, "^(transmit|xmit|trans|tx|submit).*$"))
		{
			inputCommand = inputCommand.Replace(match.Groups[1].Value, "transmit");
		}
		else if (inputCommand.RegexMatch(out match, "^lights (on|off)$"))
		{
			bool lightsOn = match.Groups[1].Value.Equals("on");
			if (_lightsOn == lightsOn)
			{
				yield return $"sendtochaterror The lights are already {(lightsOn ? "on" : "off")}.";
				yield break;
			}
			_lightsOn |= match.Groups[1].Value.Equals("on");
			_lightsOn &= !match.Groups[1].Value.Equals("off");
			inputCommand = "toggle";
			yield return null;
		}
		else if (inputCommand.Equals("toggle"))
		{
			_lightsOn = !_lightsOn;
			yield return null;
		}
		IEnumerator processTwitchCommand = base.RespondToCommandInternal(inputCommand);
		while (processTwitchCommand.MoveNext())
		{
			yield return processTwitchCommand.Current;
		}
	}

	private bool _lightsOn = true;
}
