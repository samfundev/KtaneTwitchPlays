using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class GeneralExtensions
{
	public static bool EqualsAny(this object obj, params object[] targets) => targets.Contains(obj);

	public static bool InRange(this int num, int min, int max) => min <= num && num <= max;

	public static int Mod(this int n, int m) => (n % m + m) % m;

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
				if (time <= 0 && timeParts.Count <= 0) continue;
				timeParts.Add(time);
				seconds -= time * timeLength;
			}
		}

		string formattedTime = string.Join(":", timeParts.Select((time, i) => timeParts.Count > 2 && i == 0 ? time.ToString() : time.ToString("00")).ToArray());
		if (addMilliseconds) formattedTime += ((int) (seconds * 100)).ToString(@"\.00");

		return formattedTime;
	}

	public static string Join<T>(this IEnumerable<T> values, string separator = " ")
	{
		StringBuilder stringBuilder = new StringBuilder();
		IEnumerator<T> enumerator = values.GetEnumerator();
		if (enumerator.MoveNext()) stringBuilder.Append(enumerator.Current); else return "";

		while (enumerator.MoveNext()) stringBuilder.Append(separator).Append(enumerator.Current);

		enumerator.Dispose();

		return stringBuilder.ToString();
	}

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => source.OrderBy(_ => UnityEngine.Random.value);

	//String wrapping code from http://www.java2s.com/Code/CSharp/Data-Types/ForcesthestringtowordwrapsothateachlinedoesntexceedthemaxLineLength.htm

	public static string Wrap(this string str, int maxLength, string prefix = "")
	{
		if (string.IsNullOrEmpty(str)) return "";
		if (maxLength <= 0) return prefix + str;

		List<string> lines = new List<string>();

		// breaking the string into lines makes it easier to process.
		foreach (string line in str.Split("\n".ToCharArray()))
		{
			string remainingLine = line.Trim();
			do
			{
				string newLine = GetLine(remainingLine, maxLength - prefix.Length);
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

		// Search backwards in the string for a whitespace char
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

	public static int? TryParseInt(this string number) => int.TryParse(number, out int i) ? (int?) i : null;

	public static bool ContainsIgnoreCase(this string str, string value) => str.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) != -1;

	public static bool EqualsIgnoreCase(this string str, string value) => str.Equals(value, StringComparison.InvariantCultureIgnoreCase);

	public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
	{
		IEnumerable<T> enumerable = source.ToList();
		return enumerable.Skip(Math.Max(0, enumerable.Count() - N));
	}

	public static void AddAny<T>(this List<T> source, params T[] items) => source.AddRange(items);

	public static bool RegexMatch(this string str, params string[] patterns) => str.RegexMatch(out _, patterns);

	public static bool RegexMatch(this string str, out Match match, params string[] patterns)
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

	public static double TotalSeconds(this DateTime datetime) => TimeSpan.FromTicks(datetime.Ticks).TotalSeconds;

	public static bool TryEquals(this string str, string value)
	{
		if (!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(value)) return str.Equals(value);
		if (str == null && value == null) return true;
		return str?.Length == 0 && value?.Length == 0;
	}

	public static bool TryEquals(this string str, string value, StringComparison comparisonType)
	{
		if (!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(value)) return str.Equals(value, comparisonType);
		if (str == null && value == null) return true;
		return str?.Length == 0 && value?.Length == 0;
	}

	/// <summary>
	///     Adds an element to a List&lt;V&gt; stored in the current IDictionary&lt;K, List&lt;V&gt;&gt;. If the specified key
	///     does not exist in the current IDictionary, a new List is created.</summary>
	/// <typeparam name="K">
	///     Type of the key of the IDictionary.</typeparam>
	/// <typeparam name="V">
	///     Type of the values in the Lists.</typeparam>
	/// <param name="dic">
	///     IDictionary to operate on.</param>
	/// <param name="key">
	///     Key at which the list is located in the IDictionary.</param>
	/// <param name="value">
	///     Value to add to the List located at the specified Key.</param>
	public static void AddSafe<K, V>(this IDictionary<K, List<V>> dic, K key, V value)
	{
		if (dic == null)
			throw new ArgumentNullException(nameof(dic));
		if (key == null)
			throw new ArgumentNullException(nameof(key), "Null values cannot be used for keys in dictionaries.");
		if (!dic.ContainsKey(key))
			dic[key] = new List<V>();
		dic[key].Add(value);
	}

	public static bool TryParseTime(this string timeString, out float time)
	{
		int[] multiplier = { 0, 1, 60, 3600, 86400 };

		string[] split = timeString.Split(new[] { ':' }, StringSplitOptions.None);
		float[] splitFloat = split.Where(x => float.TryParse(x, out _)).Select(float.Parse).ToArray();

		if (split.Length != splitFloat.Length)
		{
			time = 0;
			return false;
		}

		time = splitFloat.Select((t, i) => t * multiplier[split.Length - i]).Sum();
		return true;
	}

	/// <summary>
	///     Returns the index of the first element in this <paramref name="source"/> satisfying the specified <paramref
	///     name="predicate"/>. If no such elements are found, returns <c>-1</c>.</summary>
	public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source == null)
			throw new ArgumentNullException(nameof(source));
		if (predicate == null)
			throw new ArgumentNullException(nameof(predicate));
		int index = 0;
		foreach (var v in source)
		{
			if (predicate(v))
				return index;
			index++;
		}
		return -1;
	}

	public static void Trigger(this Selectable selectable)
	{
		selectable.HandleSelect(true);
		KTInputManager.Instance.SelectableManager.Select(selectable, true);
		KTInputManager.Instance.SelectableManager.HandleInteract();
		selectable.OnInteractEnded();
	}

	public static void CopyTo(this DirectoryInfo source, DirectoryInfo target)
	{
		Directory.CreateDirectory(target.FullName);

		foreach (FileInfo file in source.GetFiles())
		{
			file.CopyTo(Path.Combine(target.FullName, file.Name));
		}

		foreach (DirectoryInfo directory in source.GetDirectories())
		{
			directory.CopyTo(new DirectoryInfo(Path.Combine(target.FullName, directory.Name)));
		}
	}

	/// <summary>Splits a string using the <paramref name="seperator"/> provided and removing empty entries.</summary>
	/// <param name="value">The string to split.</param>
	/// <param name="seperator">The seperators to split by.</param>
	public static string[] SplitFull(this string value, params char[] seperator) => value.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

	/// <summary>Converts a lowercase <paramref name="character"/> into a zero-based index. Supports a-z and 1-9.</summary>
	/// <param name="character">The character to convert into a index.</param>
	public static int ToIndex(this char character) => character >= 'a' ? character - 'a' : character - '1';

	/// <summary>Checks if <paramref name="value"/> has the same first letter or is equal to <paramref name="match"/>.</summary>
	/// <param name="value">The string to check against the match.</param>
	/// <param name="match">The string to match.</param>
	public static bool FirstOrWhole(this string value, string match) => value[0] == match[0] || value == match;
}
