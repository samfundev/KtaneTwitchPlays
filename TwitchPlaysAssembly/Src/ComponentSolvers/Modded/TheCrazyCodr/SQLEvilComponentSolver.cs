using System;
using System.Collections;
using System.Linq;
using System.Threading;
using UnityEngine;

public class SQLEvilComponentSolver : ReflectionComponentSolver
{
	public SQLEvilComponentSolver(TwitchModule module) :
		base(module, "SQLModule", "!{0} toggle [Toggles between showing the goal table and the editor] | !{0} SELECT AGG(X), Y, Z [Sets the SELECT clause, where X has an optional aggregator] | !{0} GROUP BY X [Sets the GROUP BY clause] | !{0} check/submit [Presses the check button]")
	{
		// Generated answer is not stored anywhere in the mod, best way I can think of to get it for autosolver
		var source = DataComponentType.CallMethod<object>("FromDifficulty", null, _component.GetValue<object>("difficulty"));
		var getAnswerThread = new Thread(() =>
		{
			targetQueryObj = Data2ComponentType.CallMethod<object>("GenerateFromDifficulty", null, _component.GetValue<object>("difficulty"), source);
			while (_component.GetValue<object>("goal").ToString() != targetQueryObj.CallMethod<object>("Apply", source).ToString())
				targetQueryObj = Data2ComponentType.CallMethod<object>("GenerateFromDifficulty", null, _component.GetValue<object>("difficulty"), source);
			receivedAnswer = true;
			return;
		});
		getAnswerThread.Start();
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("toggle"))
		{
			yield return null;
			yield return Click(0, 0);
		}
		else if (command.EqualsAny("check", "submit"))
		{
			yield return null;
			yield return Click(1, 0);
		}
		else if (command.StartsWith("select "))
		{
			if (split.Length != 4) yield break;
			bool[] passed = { false, false, false };
			string[] vars = { "a", "b", "c", "d", "e", "f", "g", "-" };
			for (int i = 1; i < 4; i++)
			{
				for (int j = 0; j < vars.Length; j++)
				{
					if (split[i].EqualsAny("min("+vars[j]+")", "max(" + vars[j] + ")", "avg(" + vars[j] + ")", "sum(" + vars[j] + ")", "count(" + vars[j] + ")", vars[j]))
					{
						passed[i - 1] = true;
						break;
					}
				}
			}
			if (passed.Contains(false)) yield break;

			yield return null;
			KMSelectable[] changers = { _component.GetValue<KMSelectable>("selection1GroupButton"), _component.GetValue<KMSelectable>("selection2GroupButton"), _component.GetValue<KMSelectable>("selection3GroupButton") };
			KMSelectable[] changers2 = { _component.GetValue<KMSelectable>("selection1Button"), _component.GetValue<KMSelectable>("selection2Button"), _component.GetValue<KMSelectable>("selection3Button") };
			for (int i = 0; i < 3; i++)
			{
				string cur = split[i + 1];
				if (cur.Length != 1)
				{
					int offset = cur.StartsWith("count") ? 5 : 3;
					while (changers[i].GetComponentInChildren<TextMesh>().text.ToLower() != cur.Substring(0, offset))
						yield return DoInteractionClick(changers[i]);
					cur = cur[cur.IndexOf('(') + 1].ToString();
				}
				else
				{
					while (changers[i].GetComponentInChildren<TextMesh>().text.ToLower() != "none")
						yield return DoInteractionClick(changers[i]);
				}
				while (changers2[i].GetComponentInChildren<TextMesh>().text.ToLower() != cur)
					yield return DoInteractionClick(changers2[i]);
			}
		}
		else if (command.StartsWith("group by "))
		{
			if (split.Length != 3) yield break;
			if (!split[2].EqualsAny("a", "b", "c", "d", "e", "f", "g", "-"))
				yield break;

			yield return null;
			KMSelectable changer = _component.GetValue<KMSelectable>("groupBy1Button");
			while (changer.GetComponentInChildren<TextMesh>().text.ToLower() != split[2])
				yield return DoInteractionClick(changer);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		if (!_component.GetValue<bool>("isEditorMode"))
			yield return Click(0);
		while (!receivedAnswer) yield return true;
		string targetQuery = targetQueryObj.ToString();
		string selectCmd = targetQuery.Substring(0, targetQuery.IndexOf("GROUP")).Trim().ToLower();
		string groupCmd = targetQuery.Substring(targetQuery.IndexOf("GROUP"), 10).Trim().ToLower();
		yield return Respond(selectCmd.SplitFull(" ,"), selectCmd);
		yield return Respond(groupCmd.SplitFull(" ,"), groupCmd);
		yield return Click(1, 0);
	}

	private static readonly Type DataComponentType = ReflectionHelper.FindType("DataSetFactory", "simple-sql");
	private static readonly Type Data2ComponentType = ReflectionHelper.FindType("DataQueryGenerator", "simple-sql");

	private object targetQueryObj;
	private bool receivedAnswer;
}