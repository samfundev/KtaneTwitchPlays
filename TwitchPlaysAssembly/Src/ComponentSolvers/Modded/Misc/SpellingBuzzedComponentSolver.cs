using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class SpellingBuzzedComponentSolver : ReflectionComponentSolver
{
	public SpellingBuzzedComponentSolver(TwitchModule module) :
		base(module, "SpellingBuzzedModule", "Use '!{0} submit DRUNK' to submit that word into the module. Use '!{0} reset' to reset the module.")
	{
		_keypadButtons = Selectables.ToList();
		_keypadButtons.RemoveAt(2);
		_keypadButtons.RemoveAt(7);
		for (var buttonIndex = 0; buttonIndex < 7; buttonIndex++)
			_displayedLetters += _keypadButtons[buttonIndex].transform.Find("ButtonText").GetComponent<TextMesh>().text;
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		command = command.ToUpperInvariant();
		if(command.EqualsAny("RESET"))
		{
			yield return null;
			yield return Click(8, 0);
			yield break;
		}
		var m = Regex.Match(command, @"^SUBMIT\s+([" + _displayedLetters + "]+)$"); //Will only allow letters on the module.
		if (m.Success)
		{
			yield return null;
			var display = _component.GetValue<GameObject>("displayText").GetComponent<TextMesh>().text;
			if (display.Length != 0 && !m.Groups[1].Value.StartsWith(display))
				yield return Click(8, 0.15f);
			display = _component.GetValue<GameObject>("displayText").GetComponent<TextMesh>().text;
			foreach (var letter in m.Groups[1].Value.Skip(display.Length))
			{
				_keypadButtons[_displayedLetters.IndexOf(letter)].OnInteract();
				yield return new WaitForSeconds(0.15f);
			}
			yield return Click(2, 0);
		}
	}

	private readonly string _displayedLetters;
	private readonly List<KMSelectable> _keypadButtons;
}