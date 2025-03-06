using System.Collections;

[ModuleID("jackboxServerModule")]
public class JackboxTVComponentSolver : ReflectionComponentSolver
{
	public JackboxTVComponentSolver(TwitchModule module) :
		base(module, "jackboxServerModule", "!{0} s [Presses the solve button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (!command.Equals("s")) yield break;
		if (!_component.GetValue<bool>("isSolved"))
		{
			yield return "sendtochaterror The solve button is not currently present!";
			yield break;
		}

		yield return null;
		yield return Click(0, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		if (_component.GetValue<bool>("isSolved"))
			yield return Click(0);
		else
			yield return _component.CallMethod<IEnumerator>("WSSolve", "TP Autosolver");
	}
}