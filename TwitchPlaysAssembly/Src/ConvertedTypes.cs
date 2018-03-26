using System.Reflection;
using Assets.Scripts.Records;

public static class CommonReflectedTypeInfo
{
	static CommonReflectedTypeInfo()
	{
		HandlePassMethod = typeof(BombComponent).GetMethod("HandlePass", BindingFlags.NonPublic | BindingFlags.Instance);
		GameRecordCurrentStrikeIndexField = typeof(GameRecord).GetField("currentStrikeIndex", BindingFlags.NonPublic | BindingFlags.Instance);
		UpdateTimerDisplayMethod = typeof(TimerComponent).GetMethod("UpdateDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	#region Bomb Component
	public static MethodInfo HandlePassMethod
	{
		get;
		private set;
	}
	#endregion

	public static MethodInfo UpdateTimerDisplayMethod
	{
		get;
		private set;
	}
   
	public static FieldInfo GameRecordCurrentStrikeIndexField
	{
		get;
		private set;
	}
}
