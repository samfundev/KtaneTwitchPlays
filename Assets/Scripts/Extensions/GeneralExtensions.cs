using System.Linq;
using System.Collections.Generic;
using System.Text;

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

	public static string FormatTime(this float seconds)
	{
		bool addMilliseconds = seconds < 60;
		int[] timeLengths = { 86400, 3600, 60, 1 };
		List<int> timeParts = new List<int>();

		if (seconds < 1)
		{
			timeParts.Add(0);
		}
		else
		{
			foreach (int timeLength in timeLengths)
			{
				int time = (int) (seconds / timeLength);
				if (time > 0 || timeParts.Count > 0)
				{
					timeParts.Add(time);
					seconds -= time * timeLength;
				}
			}
		}

		string formatedTime = string.Join(":", timeParts.Select((time, i) => timeParts.Count > 2 && i == 0 ? time.ToString() : time.ToString("00")).ToArray());
		if (addMilliseconds) formatedTime += ((int) (seconds * 100)).ToString(@"\.00");

		return formatedTime;
	}

	public static string Join(this IEnumerable<string> strings, string separator = " ")
	{
		StringBuilder stringBuilder = new StringBuilder();
		IEnumerator<string> enumerator = strings.GetEnumerator();
		if (enumerator.MoveNext()) stringBuilder.Append(enumerator.Current); else return "";

		while (enumerator.MoveNext()) stringBuilder.Append(separator).Append(enumerator.Current);

		return stringBuilder.ToString();
	}
}