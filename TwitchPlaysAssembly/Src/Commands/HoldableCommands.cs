using System.Collections;
using UnityEngine;

/// <summary>Contains commands for holdables (including the freeplay case and the missions binder).</summary>
public static class HoldableCommands
{
	#region Commands
	[Command("help")]
	public static bool Help(TwitchHoldable holdable, string user, bool isWhisper) => holdable.PrintHelp(user, isWhisper);

	[Command("(hold|pick up)")]
	public static IEnumerator Hold(TwitchHoldable holdable) => holdable.Hold();

	[Command("(drop|let go|put down)")]
	public static IEnumerator Drop(TwitchHoldable holdable) => holdable.Drop();

	[Command(@"(turn|turn round|turn around|rotate|flip|spin)")]
	public static IEnumerator Flip(TwitchHoldable holdable) => holdable.Turn();

	[Command(@"throw *(\d+)?", AccessLevel.Mod, AccessLevel.Mod)]
	public static IEnumerator Throw(FloatingHoldable holdable, [Group(1)] int? optionalStrength = 3)
	{
		int strength = optionalStrength ?? 3;

		holdable.Pause();
		Rigidbody rigidbody = holdable.GetComponent<Rigidbody>();
		rigidbody.isKinematic = false;
		rigidbody.useGravity = true;
		rigidbody.velocity = Random.onUnitSphere * rigidbody.mass * strength;
		rigidbody.angularVelocity = Random.onUnitSphere * rigidbody.mass * strength;
		rigidbody.maxAngularVelocity = 100f;
		yield return new WaitForSeconds(2);
		rigidbody.isKinematic = true;
		rigidbody.useGravity = false;
		holdable.Resume();
	}

	[Command(null)]
	public static IEnumerator DefaultCommand(TwitchHoldable holdable, string user, bool isWhisper, string cmd)
	{
		if (holdable.CommandType == typeof(AlarmClockCommands))
			return AlarmClockCommands.Snooze(holdable, user, isWhisper);

		if (holdable.CommandType == typeof(IRCConnectionManagerCommands) ||
		    holdable.CommandType == typeof(MissionBinderCommands) ||
		    holdable.CommandType == typeof(FreeplayCommands))
			return holdable.RespondToCommand(user, isWhisper);

		return holdable.RespondToCommand(user, cmd, isWhisper);
	}
	#endregion
}
