using System;
using System.Collections;
using System.Collections.Generic;

public class UnsupportedModComponentSolver : ComponentSolver
{
	public UnsupportedModComponentSolver(BombCommander bombCommander, BombComponent bombComponent, ComponentSolverFields componentSolverFields = null) 
		: base(bombCommander, bombComponent)
	{
		_bombModule = bombComponent.GetComponent<KMBombModule>();
		_needyModule = bombComponent.GetComponent<KMNeedyModule>();
		
		ModInfo = new ModuleInformation { moduleScore = 0, builtIntoTwitchPlays = true, DoesTheRightThing = true, helpText = $"Solve this {(_bombModule != null ? "module" : "needy")} with !{{0}} solve", moduleDisplayName = $"Unsupported Twitchplays Module  ({bombComponent.GetModuleDisplayName()})", moduleID = "UnsupportedTwitchPlaysModule" };

		UnsupportedModule = true;

		Selectable selectable = bombComponent.GetComponent<Selectable>();
		Selectable[] selectables = bombComponent.GetComponentsInChildren<Selectable>();
		HashSet<Selectable> selectableHashSet = new HashSet<Selectable>(selectables) { selectable };

		selectable.OnInteract += () => { if(ComponentHandle != null && ComponentHandle.CanvasGroupUnsupported != null) ComponentHandle.CanvasGroupUnsupported.gameObject.SetActive(false); return true; };
		selectable.OnDeselect += (x) => { if (ComponentHandle != null && ComponentHandle.CanvasGroupUnsupported != null) ComponentHandle.CanvasGroupUnsupported.gameObject.SetActive(x == null || !selectableHashSet.Contains(x)); };

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
			if(ForcedSolveMethod == null)
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
