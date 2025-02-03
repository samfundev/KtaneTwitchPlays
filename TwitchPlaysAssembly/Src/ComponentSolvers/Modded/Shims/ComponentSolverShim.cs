using System.Collections;

public abstract class ComponentSolverShim : ComponentSolver
{
	protected ComponentSolver Unshimmed;

	protected ComponentSolverShim(TwitchModule module) : base(module)
	{
		// Passing null to the BombCommander argument here because Unshimmed is only used to run RespondToCommandInternal(); we don’t want it to award strikes/solves etc. because this object already does that
		Unshimmed = ComponentSolverFactory.CreateDefaultModComponentSolver(module, module.BombComponent.GetModuleID(), module.BombComponent.GetModuleDisplayName(), false);
	}

	protected sealed override IEnumerator ForcedSolveIEnumerator() => TwitchPlaySettings.data.EnableTwitchPlayShims ? ForcedSolveIEnumeratorShimmed() : ForcedSolveIEnumeratorUnshimmed();

	protected virtual IEnumerator ForcedSolveIEnumeratorShimmed() => ForcedSolveIEnumeratorUnshimmed();

	protected IEnumerator ForcedSolveIEnumeratorUnshimmed()
	{
		if (Unshimmed.ForcedSolveMethod == null) yield break;

		object result = Unshimmed.ForcedSolveMethod.Invoke(Unshimmed.CommandComponent, null);
		if (result is IEnumerator e)
		{
			yield return e;
		}
		else
		{
			yield return null;
		}
	}

	protected internal sealed override IEnumerator RespondToCommandInternal(string inputCommand) => TwitchPlaySettings.data.EnableTwitchPlayShims ? RespondToCommandShimmed(inputCommand) : RespondToCommandUnshimmed(inputCommand);

	protected virtual IEnumerator RespondToCommandShimmed(string inputCommand) => RespondToCommandUnshimmed(inputCommand);

	protected IEnumerator RespondToCommandUnshimmed(string inputCommand) => Unshimmed.RespondToCommandInternal(inputCommand);
}
