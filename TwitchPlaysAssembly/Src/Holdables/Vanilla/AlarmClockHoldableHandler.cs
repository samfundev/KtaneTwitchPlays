using System.Collections;
using System.Reflection;
using Assets.Scripts.Props;

public class AlarmClockHoldableHandler : HoldableHandler
{
	public AlarmClockHoldableHandler(KMHoldableCommander commander, FloatingHoldable holdable) : base(commander, holdable)
	{
		clock = Holdable.GetComponentInChildren<AlarmClock>();
		HelpMessage = "Snooze the alarm clock with !{0} snooze.";
		HelpMessage += TwitchPlaySettings.data.AllowSnoozeOnly 
			? " (Current Twitch play settings forbids turning the Alarm clock back on.)" 
			: " Alarm clock may also be turned back on with !{0} snooze.";
		Instance = this;
	}

	protected override IEnumerator RespondToCommandInternal(string command)
	{
		if ((TwitchPlaySettings.data.AllowSnoozeOnly && (!(bool) _alarmClockOnField.GetValue(clock)))) yield break;

		yield return null;
		yield return DoInteractionClick(clock.SnoozeButton);
	}

	static AlarmClockHoldableHandler()
	{
		_alarmClockOnField = typeof(AlarmClock).GetField("isOn", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static FieldInfo _alarmClockOnField = null;
	private AlarmClock clock;
	public static AlarmClockHoldableHandler Instance = null;
}
