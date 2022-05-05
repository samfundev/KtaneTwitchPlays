using System.Collections;

public class FreePasswordComponentSolver : ComponentSolver
{
	public FreePasswordComponentSolver(TwitchModule module) :
		base(module)
	{
		string modType = GetModuleType();
		ModInfo = ComponentSolverFactory.GetModuleInfo(modType, "!{0} submit [Presses the submit button]");
		_submit = module.BombComponent.GetComponent<KMSelectable>().Children[modType == "FreePassword" ? 10 : 40];
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (!inputCommand.Equals("submit")) yield break;

		yield return null;
		yield return DoInteractionClick(_submit, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		yield return DoInteractionClick(_submit);
	}

	private readonly KMSelectable _submit;
}