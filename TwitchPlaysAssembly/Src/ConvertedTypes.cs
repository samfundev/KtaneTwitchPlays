using Assets.Scripts.Records;
using System.Reflection;

public static class CommonReflectedTypeInfo
{
	static CommonReflectedTypeInfo()
	{
		HandlePassMethod = typeof(BombComponent).GetMethod("HandlePass", BindingFlags.NonPublic | BindingFlags.Instance);
		GameRecordCurrentStrikeIndexField = typeof(GameRecord).GetField("currentStrikeIndex", BindingFlags.NonPublic | BindingFlags.Instance);
		UpdateTimerDisplayMethod = typeof(TimerComponent).GetMethod("UpdateDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	public static MethodInfo HandlePassMethod { get; }

	public static MethodInfo UpdateTimerDisplayMethod { get; }

	public static FieldInfo GameRecordCurrentStrikeIndexField { get; }
}
