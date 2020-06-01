using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class PlungerButtonShim : ComponentSolverShim
{
	public PlungerButtonShim(TwitchModule module) :
		base(module, "plungerButton")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		if (Unshimmed.ForcedSolveMethod == null) yield break;
		var coroutine = (IEnumerator)Unshimmed.ForcedSolveMethod.Invoke(Unshimmed.CommandComponent, null);
		coroutine.MoveNext();
		yield return coroutine.Current;
		var idColor = Module.ClaimedUserMultiDecker.color;
		var discoRoutine = Module.StartCoroutine(Disco());
		coroutine.MoveNext();
		yield return coroutine.Current;
		Module.StopCoroutine(discoRoutine);
		Module.SetBannerColor(idColor);
		coroutine.MoveNext();
		yield return coroutine.Current;
	}

	IEnumerator Disco()
	{
		while (true)
		{
			int index = UnityEngine.Random.Range(0, colors.Length);
			Module.SetBannerColor(colors[index]);
			yield return new WaitForSeconds(0.125f);
		}
	}

	private readonly Color[] colors = new[] { Color.blue, brown, Color.green, Color.grey, lime, orange, Color.magenta, Color.red, Color.white, Color.yellow  };
	static Color brown = new Color(165 / 255f, 42 / 255f, 42 / 255f), lime = new Color(50/255f, 205/255f, 50/255f), orange = new Color(1, 0.5f, 0);
}
