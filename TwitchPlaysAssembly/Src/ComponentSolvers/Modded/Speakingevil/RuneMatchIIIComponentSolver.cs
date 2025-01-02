using System;
using System.Collections;
using System.Linq;
using System.Reflection;

public class RuneMatchIIIComponentSolver : ComponentSolver
{
	public RuneMatchIIIComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_orbs = (KMSelectable[]) OrbsField.GetValue(_component);
		SetHelpMessage("!{0} <a-c><1-3> [Selects the orb at the specified coordinate] | Command is chainable with spaces");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		string[] coords = { "a1", "b1", "c1", "a2", "b2", "c2", "a3", "b3", "c3" };
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
		int[] disporbs = _component.GetValue<int[]>("disporbs");
		for (int i = 0; i < commands.Length; i++)
		{
			yield return DoInteractionClick(_orbs[Array.IndexOf(coords, commands[i])]);
			if (disporbs[0] == Array.IndexOf(coords, commands[i]) || disporbs[1] == Array.IndexOf(coords, commands[i]))
			{
				yield return "strike";
				break;
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
			int[] disporbs = _component.GetValue<int[]>("disporbs");
			bool[] active = _component.GetValue<bool[]>("activeorbs");
			for (int i = 0; i < 9; i++)
			{
				if (!disporbs.Contains(i) && active[i])
					yield return DoInteractionClick(_orbs[i]);
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("RuneMatch3Script");
	private static readonly FieldInfo OrbsField = ComponentType.GetField("orbs", BindingFlags.Public | BindingFlags.Instance);

	private readonly object _component;
	private readonly KMSelectable[] _orbs;
}