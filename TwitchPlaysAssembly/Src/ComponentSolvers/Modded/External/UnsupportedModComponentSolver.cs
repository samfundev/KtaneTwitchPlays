using System;
using System.Collections;
using System.Collections.Generic;

public class UnsupportedModComponentSolver : ComponentSolver
{
	public UnsupportedModComponentSolver(BombCommander bombCommander, BombComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) 
		: base(bombCommander, bombComponent, ircConnection, canceller)
	{
		bombModule = bombComponent.GetComponent<KMBombModule>();
		modInfo = new ModuleInformation { moduleScore = 0, builtIntoTwitchPlays = true, DoesTheRightThing = true, helpText = "Solve this module with !{0} solve", moduleDisplayName = $"Unsupported Twitchplays Module  ({bombComponent.GetModuleDisplayName()})", moduleID = "UnsupportedTwitchPlaysModule" };

		UnsupportedModule = true;

		Selectable selectable = bombModule.GetComponent<Selectable>();
		Selectable[] selectables = bombComponent.GetComponentsInChildren<Selectable>();
		HashSet<Selectable> selectableHashSet = new HashSet<Selectable>(selectables) {selectable};


		selectable.OnInteract += () => { ComponentHandle?.canvasGroupUnsupported?.gameObject.SetActive(false); return true; };
		selectable.OnDeselect += (x) => { ComponentHandle?.canvasGroupUnsupported?.gameObject.SetActive(x == null || !selectableHashSet.Contains(x)); };
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (!inputCommand.Equals("solve", StringComparison.InvariantCultureIgnoreCase)) yield break;
		yield return null;
		yield return null;
		
		bombModule.HandlePass();
	}

	private KMBombModule bombModule = null;
}
