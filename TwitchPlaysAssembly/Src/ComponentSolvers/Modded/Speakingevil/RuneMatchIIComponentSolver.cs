using System;
using System.Collections;
using System.Linq;
using System.Reflection;

public class RuneMatchIIComponentSolver : ComponentSolver
{
	public RuneMatchIIComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_orbs = (KMSelectable[]) OrbsField.GetValue(_component);
		SetHelpMessage("!{0} <a-d><1-3> [Selects the orb at the specified coordinate] | Command is chainable with spaces");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		string[] coords = { "a1", "b1", "c1", "d1", "a2", "b2", "c2", "d2", "a3", "b3", "c3", "d3" };
		for (int i = 0; i < commands.Length; i++)
			if (!coords.Contains(commands[i])) yield break;
		bool[] active = _component.GetValue<bool[]>("activeorbs");
		if (!active.Contains(true))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}
		for (int i = 0; i < commands.Length; i++)
		{
			if (!active[Array.IndexOf(coords, commands[i])])
			{
				yield return "sendtochaterror You can't select a deactivated orb.";
				yield break;
			}
		}

		yield return null;
		int[] orbshuff = _component.GetValue<int[]>("orbshuff");
		int breakCt = _component.GetValue<int>("breakcount");
		for (int i = 0; i < commands.Length; i++)
		{
			int check = _component.GetValue<int>("check");
			yield return DoInteractionClick(_orbs[Array.IndexOf(coords, commands[i])]);
			breakCt++;
			if (breakCt % 2 == 0)
			{
				if (orbshuff[Array.IndexOf(coords, commands[i])] != check)
				{
					yield return "strike";
					break;
				}
			}
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

			yield return null;
			bool[] active = _component.GetValue<bool[]>("activeorbs");
			int[] orbshuff = _component.GetValue<int[]>("orbshuff");
			int breakCt = _component.GetValue<int>("breakcount");
			if (breakCt % 2 != 0)
			{
				int check = _component.GetValue<int>("check");
				for (int i = 0; i < 12; i++)
				{
					if (check == orbshuff[i] && active[i])
					{
						yield return DoInteractionClick(_orbs[i]);
						active[i] = false;
						break;
					}
				}
			}
			for (int i = 0; i < 12; i++)
			{
				if (active[i])
				{
					yield return DoInteractionClick(_orbs[i]);
					active[i] = false;
					for (int j = 0; j < 12; j++)
					{
						if (orbshuff[j] == orbshuff[i] && active[j])
						{
							yield return DoInteractionClick(_orbs[j]);
							active[i] = false;
							i = 0;
							break;
						}
					}
				}
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("RuneMatch2Script");
	private static readonly FieldInfo OrbsField = ComponentType.GetField("orbs", BindingFlags.Public | BindingFlags.Instance);

	private readonly object _component;
	private readonly KMSelectable[] _orbs;
}