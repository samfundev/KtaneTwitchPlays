using System.Collections;
using System.Linq;
using KModkit;
using UnityEngine;

public class KeepClickingComponentSolver : ReflectionComponentSolver
{
	public KeepClickingComponentSolver(TwitchModule module) :
		base(module, "keepClicking", "!{0} click <1/2/3> [Clicks the specified button from left to right] | !{0} submit [Clicks the submit button]")
	{
		_buttons = _component.GetValue<KMSelectable[]>("buttons");
		_submitButton = _component.GetValue<KMSelectable>("submitButton");
		_textMeshes = _component.GetValue<TextMesh[]>("buttonTextMeshes");
	}

	public override IEnumerator Respond(string[] split, string inputCommand)
	{
		if (inputCommand.Equals("submit"))
		{
			yield return null;
			yield return DoInteractionClick(_submitButton, 0);
		}
		else if (inputCommand.StartsWith("click ") && split.Length == 2)
		{
			if (!int.TryParse(split[1], out int check)) yield break;
			if (check < 1 || check > 3) yield break;

			yield return null;
			yield return DoInteractionClick(_buttons[check - 1], 0);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var edgework = Module.BombComponent.GetComponent<KMBombInfo>();
		yield return null;

		for (int i = 0; i < 3; i++)
		{
			int type = -1;
			if (i == 1 && edgework.GetBatteryCount() > 2)
				type = 0;
			else if (i == 0 && edgework.IsIndicatorOn("SIG"))
				type = 1;
			else
			{
				foreach (string r in edgework.GetOnIndicators())
				{
					if (r.EndsWith("R"))
					{
						type = 2;
						break;
					}
				}
				const string vowels = "AEIOU";
				if (type == -1)
				{
					foreach (char c in edgework.GetSerialNumber())
					{
						if (vowels.Contains(c))
						{
							type = 1;
							break;
						}
					}
				}
				if (type == -1)
				{
					type = 2;
				}
			}

			string[] type0symbols = new string[] { "ಗ", "♌", "୩", "ة", "♋", "♏", "♎" };
			string[] type1symbols = new string[] { "♓", "♈", "Ϡ", "ﾔ", "σ", "Ѡ", "♊" };
			string[] type2symbols = new string[] { "♍", "♑", "ױ", "♉", "♒", "৬", "♐" };
			if (type == 0)
			{
				while (!type0symbols.Contains(_textMeshes[i].text))
					yield return DoInteractionClick(_buttons[i]);
			}
			else if (type == 1)
			{
				while (!type1symbols.Contains(_textMeshes[i].text))
					yield return DoInteractionClick(_buttons[i]);
			}
			else
			{
				while (!type2symbols.Contains(_textMeshes[i].text))
					yield return DoInteractionClick(_buttons[i]);
			}
		}
		yield return DoInteractionClick(_submitButton, 0);
	}

	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _submitButton;
	private readonly TextMesh[] _textMeshes;
}