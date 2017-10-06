using System.Linq;

public static class GeneralExtensions
{
	public static bool EqualsAny(this object obj, params object[] targets)
	{
		return targets.Contains(obj);	
	}
}