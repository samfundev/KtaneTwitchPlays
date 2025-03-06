using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ModuleID("CryptModule")]
public class CryptographyComponentSolver : ComponentSolver
{
	public CryptographyComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("Keys");
		SetHelpMessage("Solve the cryptography puzzle with !{0} press N B V T K.");
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

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		string ans = _component.GetValue<string>("mTargetText");
		string curr = _component.GetValue<string>("mEnteredText");
		for (int i = 0; i < curr.Length; i++)
		{
			if (ans[i] != curr[i])
				yield break;
		}
		int start = curr.Length;
		for (int i = start; i < 5; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				if (_buttons[j].GetComponentInChildren<TextMesh>().text == ans[i].ToString())
				{
					yield return DoInteractionClick(_buttons[j]);
					break;
				}
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("CryptMod");

	private readonly KMSelectable[] _buttons;
	private readonly object _component;
}
