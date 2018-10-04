using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class CryptographyComponentSolver : ComponentSolver
{
	public CryptographyComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_buttons = (KMSelectable[]) _keysField.GetValue(bombComponent.GetComponent(_componentType));
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Solve the cryptography puzzle with !{0} press N B V T K.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (split.Length < 2 || !split[0].EqualsAny("press", "submit"))
			yield break;

		string keytext = _buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToLowerInvariant()).Join(string.Empty);
		List<int> buttons = split.Skip(1).Join(string.Empty).ToCharArray().Select(x => keytext.IndexOf(x)).ToList();
		if (buttons.Any(x => x < 0)) yield break;

		yield return "Cryptography Solve Attempt";
		foreach (int button in buttons)
			yield return DoInteractionClick(_buttons[button]);
	}

	static CryptographyComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("CryptMod");
		_keysField = _componentType.GetField("Keys", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _keysField = null;

	private KMSelectable[] _buttons = null;
}
