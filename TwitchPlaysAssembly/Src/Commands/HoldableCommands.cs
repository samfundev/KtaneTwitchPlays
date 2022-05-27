using System.Collections;
using UnityEngine;

/// <summary>Contains commands for all holdables (including the freeplay case and the missions binder).</summary>
public static class HoldableCommands
{
	#region Commands
	/// <name>Help</name>
	/// <syntax>help</syntax>
	/// <summary>Sends a message to chat with information on what commands you can use to intreact with the holdable.</summary>
	[Command("help")]
	public static bool Help(TwitchHoldable holdable, string user, bool isWhisper) => holdable.PrintHelp(user, isWhisper);

	/// <name>Hold</name>
	/// <syntax>hold</syntax>
	/// <summary>Holds the holdable.</summary>
	[Command("(hold|pick up)")]
	public static IEnumerator Hold(TwitchHoldable holdable) => holdable.Hold();

	/// <name>Drop</name>
	/// <syntax>drop</syntax>
	/// <summary>Drops the holdable.</summary>
	[Command("(drop|let go|put down)")]
	public static IEnumerator Drop(TwitchHoldable holdable) => holdable.Drop();

	/// <name>Turn</name>
	/// <syntax>turn</syntax>
	/// <summary>Turns the holdable around.</summary>
	[Command(@"(turn|turn round|turn around|rotate|flip|spin)")]
	public static IEnumerator Flip(TwitchHoldable holdable) => holdable.Turn();

	/// <name>Throw</name>
	/// <syntax>throw\nthrow 10</syntax>
	/// <summary>Throws the holdable with an optional strength.</summary>
	[Command(@"(?:throw|yeet) *(\d+)?", AccessLevel.Admin, AccessLevel.Admin)]
	public static IEnumerator Throw(FloatingHoldable holdable, [Group(1)] int? optionalStrength = 5)
	{
		int strength = optionalStrength ?? 5;

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
		if (holdable.CommandType != null)
			return holdable.RespondToCommand(user, isWhisper);

		return holdable.RespondToCommand(user, cmd, isWhisper);
	}
	#endregion
}
