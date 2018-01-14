using System;
using System.Reflection;
using Assets.Scripts.Records;

public static class CommonReflectedTypeInfo
{
    static CommonReflectedTypeInfo()
    {
		HandlePassMethod = typeof(BombComponent).GetMethod("HandlePass", BindingFlags.NonPublic | BindingFlags.Instance);
        GameRecordCurrentStrikeIndexField = typeof(GameRecord).GetField("currentStrikeIndex", BindingFlags.NonPublic | BindingFlags.Instance);
    }

	#region Bomb Component
	public static MethodInfo HandlePassMethod
	{
		get;
		private set;
	}
    #endregion
   
    public static FieldInfo GameRecordCurrentStrikeIndexField
    {
        get;
        private set;
    }
}
