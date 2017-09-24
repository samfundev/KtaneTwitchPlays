using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class SwitchesComponentSolver : ComponentSolver
{
	public SwitchesComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		_component = bombComponent.GetComponent(_componentType);
		helpMessage = "Flip switches using !{0} flip 1 5 3 2.";
	}

	private int? TryParse(string input)
	{
		int i;
		return int.TryParse(input, out i) ? (int?) i : null;
	}
	
	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length > 1 && validCommands.Contains(commands[0]))
		{
			var switches = commands.Where((_, i) => i > 0).Select(n => TryParse(n));
			if (switches.All(n => n != null && n > 0 && n < 6))
			{
				yield return null;

				foreach (int? switchIndex in switches)
				{
					_OnToggleMethod.Invoke(_component, new object[] { switchIndex - 1 });
					yield return new WaitForSeconds(0.1f);
				}
			}
		}
	}

	static SwitchesComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("SwitchModule");
		_OnToggleMethod = _componentType.GetMethod("OnToggle", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static MethodInfo _OnToggleMethod = null;

	private static string[] validCommands = { "flip", "switch", "press", "toggle" };

	private object _component = null;
}
