using System;
using System.Collections;
using UnityEngine;

public class CatchphraseShim : ComponentSolverShim
{
	public CatchphraseShim(TwitchModule module) :
		base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		component = module.BombComponent.GetComponent(ComponentType);
		panels = component.GetValue<KMSelectable[]>("panels");
		keypad = component.GetValue<KMSelectable[]>("keypads");
		clear = component.GetValue<KMSelectable>("clearButton");
		submit = component.GetValue<KMSelectable>("submitButton");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().SplitFull(' ');
		if (commands.Length == 4 && commands[0] == "panel" && int.TryParse(commands[1], out int panelPosition) && panelPosition.InRange(1, 4) && commands[2] == "at" && int.TryParse(commands[3], out int timerDigit) && timerDigit.InRange(0, 9) && panels[panelPosition - 1].GetComponentInParent<Animator>().GetBool("shrink"))
		{
			yield return $"sendtochaterror Panel {panelPosition} has already been pressed.";
			yield break;
		}

		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		string curr = component.GetValue<TextMesh>("answerBox").text;
		string ans = component.GetValue<int>("correctAnswer").ToString();
		bool clrPress = false;
		if (curr.Length > ans.Length)
		{
			yield return DoInteractionClick(clear);
			clrPress = true;
		}
		else
		{
			for (int i = 0; i < curr.Length; i++)
			{
				if (i == ans.Length)
					break;
				if (curr[i] != ans[i])
				{
					yield return DoInteractionClick(clear);
					clrPress = true;
					break;
				}
			}
		}
		int start = 0;
		if (!clrPress)
			start = curr.Length;
		for (int j = start; j < ans.Length; j++)
		{
			if (ans[j] == '0')
				yield return DoInteractionClick(keypad[9]);
			else
				yield return DoInteractionClick(keypad[int.Parse(ans[j].ToString()) - 1]);
		}
		yield return DoInteractionClick(submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("catchphraseScript", "catchphrase");

	private readonly object component;
	private readonly KMSelectable[] panels;
	private readonly KMSelectable[] keypad;
	private readonly KMSelectable clear;
	private readonly KMSelectable submit;
}
