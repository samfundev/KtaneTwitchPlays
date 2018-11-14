using System.Collections;
using System.Reflection;
using Assets.Scripts.Props;

/// <summary>Commands for the alarm clock.</summary>
public static class AlarmClockCommands
{
	[Command("snooze")]
	public static IEnumerator Snooze(TwitchHoldable holdable, string user, bool isWhisper) =>
		holdable.RespondToCommand(user, "", isWhisper, Snooze(holdable.Holdable.GetComponent<AlarmClock>(), user, isWhisper));

	public static IEnumerator Snooze(AlarmClock clock, string user, bool isWhisper)
	{
		var onField = typeof(AlarmClock).GetField("isOn", BindingFlags.NonPublic | BindingFlags.Instance);
		if(onField == null) yield break;

		if (TwitchPlaySettings.data.AllowSnoozeOnly && !TwitchPlaySettings.data.AnarchyMode && !(bool) onField.GetValue(clock))
			yield break;

		yield return null;
		clock.SnoozeButton.GetComponent<Selectable>().Trigger();
	}
}
