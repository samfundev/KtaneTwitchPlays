using System;
using System.Collections;
using UnityEngine;

public class NecronomiconComponentSolver : ComponentSolver
{
	public NecronomiconComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Cycle all the pages using !{0} cycle. Submit a specific page using !{0} page 3.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim();
		string[] split = inputCommand.Split(new[] { ' ', ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);

		if (split.Length == 1 && split[0].EqualsAny("cycle", "c", "pages"))
		{
			yield return null;

			yield return DoInteractionClick(selectables[0]);

			for (int i = 0; i < 8; i++)
			{
				yield return new WaitForSeconds(2.25f);
				yield return DoInteractionClick(selectables[1]);
			}
		}
		else if (split.Length == 2 && split[0].EqualsAny("page", "p") && int.TryParse(split[1], out int pageNumber) && pageNumber.InRange(1, 8))
		{
			yield return null;

			yield return DoInteractionClick(selectables[0]);

			for (int i = 1; i < pageNumber; i++)
			{
				yield return new WaitForSeconds(0.3f);
				yield return DoInteractionClick(selectables[1]);
			}

			yield return "solve";
			yield return "strike";
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type necronomiconScriptType = ReflectionHelper.FindType("necronomiconScript");
		if (necronomiconScriptType == null) yield break;

		object component = Module.BombComponent.GetComponent(necronomiconScriptType);

		yield return null;
		yield return RespondToCommandInternal("page " + component.GetValue<int>("correctPage"));
	}

	private readonly KMSelectable[] selectables;
}
