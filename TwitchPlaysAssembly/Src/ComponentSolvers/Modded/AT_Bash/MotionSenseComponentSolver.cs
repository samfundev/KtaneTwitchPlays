using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class MotionSenseComponentSolver : ComponentSolver
{
	public MotionSenseComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_needy = (KMNeedyModule) NeedyField.GetValue(_component);
		SetHelpMessage("I am a passive module that awards strikes for motion while I am active. Use !{0} status to find out if I am active, and for how long.");
		_needy.OnNeedyActivation += () => IRCConnection.SendMessage($"Motion Sense just activated: Active for {(int) _needy.GetNeedyTimeRemaining()} seconds.");

		_needy.OnTimerExpired += () => IRCConnection.SendMessage("Motion Sense now Inactive.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		if (!inputCommand.Equals("status", StringComparison.InvariantCultureIgnoreCase))
			yield break;

		bool active = (bool) ActiveField.GetValue(_component);
		IRCConnection.SendMessage("Motion Sense Status: " + (active ? "Active for " + (int) _needy.GetNeedyTimeRemaining() + " seconds" : "Inactive"));
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("MotionSenseModule");
	private static Component _component;
	private static readonly FieldInfo ActiveField = ComponentType.GetField("_active", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo NeedyField = ComponentType.GetField("NeedyModule", BindingFlags.Public | BindingFlags.Instance);

	private readonly KMNeedyModule _needy;
}
