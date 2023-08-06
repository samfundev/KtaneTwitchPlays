using System;
using System.Collections;
using System.Collections.Generic;

public class UnsupportedModComponentSolver : ComponentSolver
{
	public UnsupportedModComponentSolver(TwitchModule module, ComponentSolverFields componentSolverFields = null)
		: base(module, componentSolverFields == null || componentSolverFields.HookUpEvents)
	{
		_bombModule = module.BombComponent.GetComponent<KMBombModule>();
		_needyModule = module.BombComponent.GetComponent<KMNeedyModule>();

		ModInfo = new ModuleInformation { scoreString = "0", builtIntoTwitchPlays = true, helpText = $"Solve this {(_bombModule != null ? "module" : "needy")} with !{{0}} solve", moduleDisplayName = $"Unsupported Twitchplays Module  ({module.BombComponent.GetModuleDisplayName()})", moduleID = "UnsupportedTwitchPlaysModule" };

		UnsupportedModule = true;

		Selectable selectable = module.BombComponent.GetComponent<Selectable>();
		Selectable[] selectables = module.BombComponent.GetComponentsInChildren<Selectable>();
		HashSet<Selectable> selectableHashSet = new HashSet<Selectable>(selectables) { selectable };

		selectable.OnInteract += () => { if (Module != null && Module.CanvasGroupUnsupported != null) Module.CanvasGroupUnsupported.gameObject.SetActive(false); return true; };
		selectable.OnDeselect += (x) => { if (Module != null && Module.CanvasGroupUnsupported != null) Module.CanvasGroupUnsupported.gameObject.SetActive(x == null || !selectableHashSet.Contains(x)); };

		if (componentSolverFields == null) return;
		CommandComponent = componentSolverFields.CommandComponent;
		ForcedSolveMethod = componentSolverFields.ForcedSolveMethod;
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (!inputCommand.Trim().Equals("solve", StringComparison.InvariantCultureIgnoreCase)) yield break;
		yield return null;
		yield return null;
		if (_bombModule != null)
		{
			if (ForcedSolveMethod == null)
				_bombModule.HandlePass();
			SolveSilently();
		}
		else if (_needyModule != null)
		{
			_needyModule.HandlePass();
		}
	}

	private readonly KMBombModule _bombModule;
	private readonly KMNeedyModule _needyModule;
}
