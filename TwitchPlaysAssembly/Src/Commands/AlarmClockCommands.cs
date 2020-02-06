using System.Collections;
using System.Reflection;
using Assets.Scripts.Props;

/// <summary>Commands for the alarm clock.</summary>
public static class AlarmClockCommands
{
	/// <name>Snooze</name>
	/// <syntax>snooze</syntax>
	/// <summary>Hits the snooze button on the alarm clock.</summary>
	[Command("snooze")]
	public static IEnumerator Snooze(TwitchHoldable holdable, string user, bool isWhisper) =>
		holdable.RespondToCommand(user, "", isWhisper, Snooze(holdable.Holdable.GetComponent<AlarmClock>()));

	public static IEnumerator Snooze(AlarmClock clock)
	{
		var onField = typeof(AlarmClock).GetField("isOn", BindingFlags.NonPublic | BindingFlags.Instance);
		if(onField == null) yield break;

		if (TwitchPlaySettings.data.AllowSnoozeOnly && !TwitchPlaySettings.data.AnarchyMode && !(bool) onField.GetValue(clock))
			yield break;

		yield return null;
		clock.SnoozeButton.GetComponent<Selectable>().Trigger();
	}
}
