using System.Collections;
using UnityEngine;

public class TypingTutorComponentSolver : ReflectionComponentSolver
{
	public TypingTutorComponentSolver(TwitchModule module) :
		base(module, "TypingTutor", "!{0} IMPOSTER [Type and submit a word]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Length > 8) yield break;

		if (!_component.GetValue<bool>("isActive"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		if (_component.GetValue<object>("Settings").GetValue<bool>("NoKeyboard"))
		{
			for (int i = 0; i < command.Length; i++)
			{
				if (!command[i].EqualsAny('0', '1'))
				{
					yield return "sendtochaterror Only 0's and 1's are allowed in NoKeyboard mode.";
					yield break;
				}
			}
			yield return null;
			int times = _component.GetValue<TextMesh>("InputText").text.Length;
			for (int i = 0; i < times; i++)
				yield return Click(2);
			for (int i = 0; i < command.Length; i++)
			{
				if (command[i].Equals('0'))
					yield return Click(0);
				else
					yield return Click(1);
			}
			yield return Click(3);
		}
		else
		{
			for (int i = 0; i < command.Length; i++)
			{
				if (!"abcdefghijklmnopqrstuvwxyz".Contains(command[i].ToString()))
					yield break;
			}
			yield return null;
			TextMesh input = _component.GetValue<TextMesh>("InputText");
			int times = input.text.Length;
			for (int i = 0; i < times; i++)
			{
				input.text = input.text.Substring(0, input.text.Length - 1);
				yield return new WaitForSeconds(.1f);
			}
			for (int i = 0; i < command.Length; i++)
			{
				input.text += command[i];
				yield return new WaitForSeconds(.1f);
			}
			_component.CallMethod("Submit");
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		bool noKeyboard = _component.GetValue<object>("Settings").GetValue<bool>("NoKeyboard");
		TextMesh upperTxt = _component.GetValue<TextMesh>("UpperText");
		TextMesh bottomTxt = _component.GetValue<TextMesh>("InputText");
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running)
			{
				yield return true;
				continue;
			}

			while (!upperTxt.text.StartsWith(bottomTxt.text.ToLower()))
			{
				if (!noKeyboard)
				{
					bottomTxt.text = bottomTxt.text.Substring(0, bottomTxt.text.Length - 1);
					yield return new WaitForSeconds(.1f);
				}
				else
					yield return Click(2);
			}
			for (int i = bottomTxt.text.Length; i < 8; i++)
			{
				if (!noKeyboard)
				{
					bottomTxt.text += upperTxt.text[i];
					yield return new WaitForSeconds(.1f);
				}
				else
				{
					if (upperTxt.text[i].Equals('0'))
						yield return Click(0);
					else
						yield return Click(1);
				}
			}
			if (!noKeyboard)
			{
				_component.CallMethod("Submit");
				yield return new WaitForSeconds(.1f);
			}
			else
				yield return Click(3);
		}
	}
}