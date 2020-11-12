using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class GoogleSheet : CustomYieldInstruction
{
	readonly DownloadText download;
	readonly string[] rows;

	public GoogleSheet(string url, params string[] rows)
	{
		download = new DownloadText(url);
		this.rows = rows;
	}

	public override bool keepWaiting => download.keepWaiting;

	public bool Success => download.Text != null;

	public IEnumerable<Dictionary<string, string>> GetRows()
	{
		foreach (var entry in JObject.Parse(download.Text)["feed"]["entry"])
		{
			var dictionary = new Dictionary<string, string>();
			foreach (var row in rows)
			{
				dictionary[row] = entry[$"gsx${row}"].Value<string>("$t");
			}

			yield return dictionary;
		}
	}
}