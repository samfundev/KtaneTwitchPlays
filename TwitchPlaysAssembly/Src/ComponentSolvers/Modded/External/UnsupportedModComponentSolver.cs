using System;
using System.Collections;

public class UnsupportedModComponentSolver : ComponentSolver
{
	public UnsupportedModComponentSolver(BombCommander bombCommander, BombComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) 
		: base(bombCommander, bombComponent, ircConnection, canceller)
	{
		bombModule = bombComponent.GetComponent<KMBombModule>();
		modInfo = ComponentSolverFactory.GetModuleInfo("UnsupportedTwitchPlaysModule");
		ComponentHandle.canvasGroupUnsupported.gameObject.SetActive(true);
		ComponentHandle.idTextUnsupported.text = ComponentHandle.idText.text.Replace("!<id>", Code);
		ComponentHandle.Unsupported = true;
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (!inputCommand.Equals("solve", StringComparison.InvariantCultureIgnoreCase)) yield break;
		yield return null;
		yield return null;
		ComponentHandle.idTextUnsupported.gameObject.SetActive(false);
		bombModule.HandlePass();
	}

	private KMBombModule bombModule = null;
}
