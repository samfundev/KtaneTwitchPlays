using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
	{
		return source.OrderBy(x => UnityEngine.Random.value);
	}

    //String wrapping code from http://www.java2s.com/Code/CSharp/Data-Types/ForcesthestringtowordwrapsothateachlinedoesntexceedthemaxLineLength.htm
    public static string Wrap(this string str, int maxLength)
    {
        return Wrap(str, maxLength, "");
    }

    public static string Wrap(this string str, int maxLength, string prefix)
    {
        if (string.IsNullOrEmpty(str)) return "";
        if (maxLength <= 0) return prefix + str;

        var lines = new List<string>();

        // breaking the string into lines makes it easier to process.
        foreach (string line in str.Split("\n".ToCharArray()))
        {
            var remainingLine = line.Trim();
            do
            {
                var newLine = GetLine(remainingLine, maxLength - prefix.Length);
                lines.Add(newLine);
                remainingLine = remainingLine.Substring(newLine.Length).Trim();
                // Keep iterating as int as we've got words remaining 
                // in the line.
            } while (remainingLine.Length > 0);
        }

        return string.Join("\n" + prefix, lines.ToArray());
    }

    private static string GetLine(string str, int maxLength)
    {
        // The string is less than the max length so just return it.
        if (str.Length <= maxLength) return str;

        // Search backwords in the string for a whitespace char
        // starting with the char one after the maximum length
        // (if the next char is a whitespace, the last word fits).
        for (int i = maxLength; i >= 0; i--)
        {
            if (char.IsWhiteSpace(str[i]))
                return str.Substring(0, i).TrimEnd();
        }

        // No whitespace chars, just break the word at the maxlength.
        return str.Substring(0, maxLength);
    }
    
    public static int? TryParseInt(this string number)
    {
        return int.TryParse(number, out int i) ? (int?)i : null;
    }

	public static bool ContainsIgnoreCase(this string str, string value)
	{
		return str.ToLowerInvariant().Contains(value.ToLowerInvariant());
	}

	public static bool EqualsIgnoreCase(this string str, string value)
	{
		return str.Equals(value, StringComparison.InvariantCultureIgnoreCase);
	}

	public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
	{
		return source.Skip(Math.Max(0, source.Count() - N));
	}

	public static bool RegexMatch(this string str, params string[]patterns)
	{
		return str.RegexMatch(out _, patterns);
	}

	public static bool RegexMatch(this string str, out Match match, params string []patterns)
	{
		if (patterns == null) throw new ArgumentNullException(nameof(patterns));
		match = null;
		foreach (string pattern in patterns)
		{
			try
			{
				Regex r = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
				match = r.Match(str);
				if (match.Success)
					return true;
			}
			catch (Exception ex)
			{
				DebugHelper.LogException(ex);
			}
		}
		return false;
	}

	public static double TotalSeconds(this DateTime datetime)
	{
		return TimeSpan.FromTicks(datetime.Ticks).TotalSeconds;
	}

	public static bool TryEquals(this string str, string value)
	{
		if (!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(value)) return str.Equals(value);
		if (str == null && value == null) return true;
		if (str == string.Empty && value == string.Empty) return true;
		return false;
	}

	public static bool TryEquals(this string str, string value, StringComparison comparisonType)
	{
		if (!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(value)) return str.Equals(value, comparisonType);
		if (str == null && value == null) return true;
		if (str == string.Empty && value == string.Empty) return true;
		return false;
	}
}