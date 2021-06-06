using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class NeedyHotateComponentSolver : ReflectionComponentSolver
{
	public NeedyHotateComponentSolver(TwitchModule module) :
		base(module, "NeedyHotate", "!{0} press <p1> (p2)... [Presses the button(s) in the specified position(s)] | Valid positions are tl, tm, tr, ml, mm, mr, bl, bm, and br")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (!command.StartsWith("press ")) yield break;
		for (int i = 1; i < split.Length; i++)
		{
			if (!buttons.Contains(split[i]))
				yield break;
		}
		if (!_component.GetValue<bool>("active"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		for (int i = 1; i < split.Length; i++)
		{
			yield return Click(Array.IndexOf(buttons, split[i]), .25f);
			if (_component.GetValue<int>("Hotate") == 0)
				yield break;
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running)
			{
				yield return true;
				continue;
			}

			TextMesh[] texts = _component.GetValue<TextMesh[]>("Text");
			int pos = _component.GetValue<int>("Hotate");
			int actCt = _component.GetValue<int>("activationCount");
			for (int i = pos; i < 3; i++)
			{
				for (int j = 0; j < texts.Length; j++)
				{
					if (actCt % 2 == 1)
					{
						if (texts[j].text == hotate[i])
						{
							yield return RespondToCommandInternal("press " + buttons[j]);
							break;
						}
					}
					else
					{
						if (texts[j].text == hotate[2 - i])
						{
							yield return RespondToCommandInternal("press " + buttons[j]);
							break;
						}
					}
				}
			}
		}
	}

	private readonly string[] buttons = { "tl", "tm", "tr", "ml", "mm", "mr", "bl", "bm", "br" };
	private readonly string[] hotate = { "HO", "TA", "TE" };
}