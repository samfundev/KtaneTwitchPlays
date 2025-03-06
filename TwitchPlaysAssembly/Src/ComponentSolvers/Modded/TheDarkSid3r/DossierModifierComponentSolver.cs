using System.Collections;

[ModuleID("TDSDossierModifier")]
public class DossierModifierComponentSolver : ComponentSolver
{
	public DossierModifierComponentSolver(TwitchModule module) :
		base(module)
	{
		SetHelpMessage("!dossier help [View the commands for the dossier menu]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		yield return null;
		yield return "sendtochaterror Please use !dossier commands to interact with this module.";
	}
}