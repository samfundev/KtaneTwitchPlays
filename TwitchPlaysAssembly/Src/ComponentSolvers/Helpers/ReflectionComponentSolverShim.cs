using System.Collections;

public abstract class ReflectionComponentSolverShim : ReflectionComponentSolver
{
	protected ComponentSolver Unshimmed;

	protected ReflectionComponentSolverShim(TwitchModule module, string componentTypeString) : this(module, componentTypeString, null)
	{
	}

	protected ReflectionComponentSolverShim(TwitchModule module, string componentTypeString, string assemblyName) : base(module, componentTypeString, assemblyName, null)
	{
		// Passing null to the BombCommander argument here because Unshimmed is only used to run RespondInternal(); we don’t want it to award strikes/solves etc. because this object already does that
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
			while (e.MoveNext())
				yield return e.Current;
		}
		else
		{
			yield return null;
		}
	}

	public override IEnumerator Respond(string[] split, string command) => TwitchPlaySettings.data.EnableTwitchPlayShims ? RespondShimmed(split, command) : RespondUnshimmed(command);

	protected virtual IEnumerator RespondShimmed(string[] split, string command) => RespondUnshimmed(command);

	protected IEnumerator RespondUnshimmed(string command) => Unshimmed.RespondToCommandInternal(command);
}
