using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class StreetFighterComponentSolver : ComponentSolver
{
	public StreetFighterComponentSolver(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(_componentType);
		selectables = (KMSelectable[])fighterButtonsField.GetValue(_component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} select Chun Li, M. Bison [selects Chun Li as player 1, and M. Bison as player 2]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim().ToLowerInvariant().Replace("select ", "").Replace(".", "").Replace(" ", "");
		string[] parts = inputCommand.Split(',');
		if (parts.Length != 2 && parts.Length != 1)
			yield break;

		bool AddToList(ICollection<int> list, int i)
		{
			int index = Array.IndexOf(names, parts[i]);
			if (index == -1) return false;
			list.Add(index);
			return true;
		}

		List<int> indices = new List<int>();
		if (!AddToList(indices, 0)) yield break;
		if (parts.Length == 2 && !AddToList(indices, 1)) yield break;
		foreach (int i in indices)
		{
			yield return null;
			DoInteractionHighlight(selectables[i]);
			yield return new WaitForSeconds(0.1f);
			yield return DoInteractionClick(selectables[i]);
			yield return new WaitForSeconds(0.6f);
		}
	}

	static StreetFighterComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("streetFighterScript");
		fighterButtonsField = _componentType.GetField("fighterButton", BindingFlags.Public | BindingFlags.Instance);
	}

	private static readonly Type _componentType;
	private static readonly FieldInfo fighterButtonsField;

	private readonly object _component;

	private readonly KMSelectable[] selectables;
	private readonly string[] names = { "ryu", "ehonda", "blanka", "guile", "balrog", "vega", "ken", "chunli", "zangief", "dhalsim", "sagat", "mbison" };
}
