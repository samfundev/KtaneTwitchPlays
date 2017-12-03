using System.Linq;

public static class GeneralExtensions
{
	public static bool EqualsAny(this object obj, params object[] targets)
	{
		return targets.Contains(obj);	
	}

	public static bool InRange(this int num, int min, int max)
	{
		return min <= num && num <= max;
	}
}