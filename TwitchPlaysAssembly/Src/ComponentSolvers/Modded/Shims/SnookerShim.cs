using System;
using System.Collections;
using System.Collections.Generic;

public class SnookerShim : ComponentSolverShim
{
	public SnookerShim(TwitchModule module)
		: base(module, "snooker")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_cue = _component.GetValue<KMSelectable>("cueBall");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		if (_component.GetValue<int>("currentBreak") != 0 || _component.GetValue<int[]>("breaksPlayed")[0] != 0)
			yield break;
		object[] balls = _component.GetValue<object[]>("balls");
		bool[] usedRedBalls = new bool[balls.Length];
		List<string> breaks = new List<string>();
		for (int i = 0; i < balls.Length; i++)
		{
			if (!balls[i].GetValue<KMSelectable>("selectable").gameObject.activeSelf)
				usedRedBalls[i] = true;
		}
		for (int i = 1; i < 5; i++)
		{
			breaks = _component.GetValue<List<string>>("break"+i+"String");
			for (int j = 0; j < breaks.Count; j++)
			{
				List<object> choices = new List<object>();
				for (int k = 0; k < balls.Length; k++)
				{
					if (balls[k].GetValue<string>("colour") == breaks[j] && !usedRedBalls[k])
						choices.Add(balls[k]);
				}
				int pick = UnityEngine.Random.Range(0, choices.Count);
				if (breaks[j] == "red")
					usedRedBalls[Array.IndexOf(balls, choices[pick])] = true;
				yield return DoInteractionClick(choices[pick].GetValue<KMSelectable>("selectable"), 1);
			}
			if (i != 4)
				yield return DoInteractionClick(_cue, 2);
			else
				yield return DoInteractionClick(_cue, 0);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("snookerScript", "snooker");

	private readonly object _component;
	private readonly KMSelectable _cue;
}
