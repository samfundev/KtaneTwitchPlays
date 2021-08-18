using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

public class GoogleSheet : CustomYieldInstruction
{
	readonly DownloadText download;

	private string[] columnNames;

	public GoogleSheet(string sheetID, string gid = "0", params string[] columns)
	{
		download = new DownloadText($"https://docs.google.com/spreadsheets/d/{sheetID}/gviz/tq?tqx=out:json&gid={gid}");
		columnNames = columns;
	}

	public override bool keepWaiting => download.keepWaiting;

	public bool Success => download.Text != null;

#pragma warning disable CS0649
	private class SheetResponse
	{
		public Table table;

		public class Table
		{
			public Column[] cols;
			public Row[] rows;
			public class Column
			{
				public string label;
			}

			public class Row
			{
				public Value[] c;

				public class Value
				{
					public string v;
				}
			}
		}
	}
#pragma warning restore CS0649

	public IEnumerable<Dictionary<string, string>> GetRows()
	{
		var matches = Regex.Match(download.Text, @"google.visualization.Query.setResponse\((.+)\)");
		if (!matches.Success)
			yield break;

		var json = matches.Groups[1].Value;

		var response = JsonConvert.DeserializeObject<SheetResponse>(json);
		if (response == null)
			yield break;

		var table = response.table;
		var columns = table.cols.Select(column => Regex.Replace(column.label.ToLowerInvariant(), "[^a-z]", "")).ToArray();

		foreach (var row in table.rows)
		{
			var dictionary = new Dictionary<string, string>();
			for (int i = 0; i < columns.Length; i++)
			{
				var columnName = columnNames.ElementAtOrDefault(i) ?? columns[i];
				dictionary[columnName] = row.c[i]?.v ?? "";
			}

			yield return dictionary;
		}
	}
}