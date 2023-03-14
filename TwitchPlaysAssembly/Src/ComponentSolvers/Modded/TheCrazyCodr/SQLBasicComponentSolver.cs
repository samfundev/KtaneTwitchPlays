using System;
using System.Collections;
using System.Threading;
using UnityEngine;

public class SQLBasicComponentSolver : ReflectionComponentSolver
{
	public SQLBasicComponentSolver(TwitchModule module) :
		base(module, "SQLModule", "!{0} toggle [Toggles between showing the goal table and the editor] | !{0} SELECT X, Y, Z [Sets the SELECT clause] | !{0} WHERE W OPER X (AND/OR Y OPER Z) [Sets the WHERE clause, where the section in parentheses is optional] | !{0} LIMIT X, Y [Sets the LIMIT clause] | !{0} check/submit [Presses the check button]")
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
			for (int i = 1; i < 4; i++)
			{
				if (!split[i].EqualsAny("a", "b", "c", "d", "e", "f", "g", "-"))
					yield break;
			}
			if (!_component.GetValue<bool>("isEditorMode"))
			{
				yield return "sendtochaterror You must be in the editor to do this!";
				yield break;
			}

			yield return null;
			KMSelectable[] changers = { _component.GetValue<KMSelectable>("selection1Button"), _component.GetValue<KMSelectable>("selection2Button"), _component.GetValue<KMSelectable>("selection3Button") };
			for (int i = 0; i < 3; i++)
			{
				while (changers[i].GetComponentInChildren<TextMesh>().text.ToLower() != split[i + 1])
					yield return DoInteractionClick(changers[i]);
			}
		}
		else if (command.StartsWith("where "))
		{
			if (split.Length == 4)
			{
				if (!split[1].EqualsAny("a", "b", "c", "d", "e", "f", "g"))
					yield break;
				if (!split[2].EqualsAny("=", "<>", "<", "<=", ">", ">="))
					yield break;
				if (!split[3].EqualsAny("1", "2", "3", "4", "5", "6", "7", "8", "9", "0"))
					yield break;
				if (!_component.GetValue<bool>("isEditorMode"))
				{
					yield return "sendtochaterror You must be in the editor to do this!";
					yield break;
				}

				yield return null;
				KMSelectable[] changers = { _component.GetValue<KMSelectable>("where1LeftOperandButton"), _component.GetValue<KMSelectable>("where1OperatorButton"), _component.GetValue<KMSelectable>("where1RightOperandButton"), _component.GetValue<KMSelectable>("whereCombinationOperatorButton") };
				for (int i = 0; i < 4; i++)
				{
					while (changers[i].GetComponentInChildren<TextMesh>().text.ToLower() != (i != 3 ? split[i + 1] : "-"))
						yield return DoInteractionClick(changers[i]);
				}
			}
			else if (split.Length == 8)
			{
				if (!split[1].EqualsAny("a", "b", "c", "d", "e", "f", "g") || !split[5].EqualsAny("a", "b", "c", "d", "e", "f", "g"))
					yield break;
				if (!split[2].EqualsAny("=", "<>", "<", "<=", ">", ">=") || !split[6].EqualsAny("=", "<>", "<", "<=", ">", ">="))
					yield break;
				if (!split[3].EqualsAny("1", "2", "3", "4", "5", "6", "7", "8", "9", "0") || !split[7].EqualsAny("1", "2", "3", "4", "5", "6", "7", "8", "9", "0"))
					yield break;
				if (!split[4].EqualsAny("and", "or"))
					yield break;
				if (!_component.GetValue<bool>("isEditorMode"))
				{
					yield return "sendtochaterror You must be in the editor to do this!";
					yield break;
				}

				yield return null;
				KMSelectable[] changers = { _component.GetValue<KMSelectable>("where1LeftOperandButton"), _component.GetValue<KMSelectable>("where1OperatorButton"), _component.GetValue<KMSelectable>("where1RightOperandButton"), _component.GetValue<KMSelectable>("whereCombinationOperatorButton"), _component.GetValue<KMSelectable>("where2LeftOperandButton"), _component.GetValue<KMSelectable>("where2OperatorButton"), _component.GetValue<KMSelectable>("where2RightOperandButton") };
				for (int i = 0; i < 7; i++)
				{
					while (changers[i].GetComponentInChildren<TextMesh>().text.ToLower() != (i != 3 ? split[i + 1] : split[4]))
						yield return DoInteractionClick(changers[i]);
				}
			}
		}
		else if (command.StartsWith("limit "))
		{
			if (split.Length != 3) yield break;
			if (!split[1].EqualsAny("1", "2", "3", "4", "5", "6", "7", "8", "9", "999"))
				yield break;
			if (!split[2].EqualsAny("1", "2", "3", "4", "5", "6", "7", "8", "9", "0"))
				yield break;
			if (!_component.GetValue<bool>("isEditorMode"))
			{
				yield return "sendtochaterror You must be in the editor to do this!";
				yield break;
			}

			yield return null;
			KMSelectable[] changers = { _component.GetValue<KMSelectable>("limitTakeButton"), _component.GetValue<KMSelectable>("limitSkipButton") };
			for (int i = 0; i < 2; i++)
			{
				while (changers[i].GetComponentInChildren<TextMesh>().text.ToLower() != split[i + 1].Replace("999", "all").Replace("0", "none"))
					yield return DoInteractionClick(changers[i]);
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		if (!_component.GetValue<bool>("isEditorMode"))
			yield return Click(0);
		while (!receivedAnswer) yield return true;
		string targetQuery = targetQueryObj.ToString();
		string selectCmd = targetQuery.Substring(0, targetQuery.IndexOf("WHERE")).Trim().ToLower();
		string limitCmd = targetQuery.Substring(targetQuery.IndexOf("LIMIT")).Trim().ToLower();
		string whereCmd = targetQuery.Substring(targetQuery.IndexOf("WHERE"), targetQuery.Length - selectCmd.Length - limitCmd.Length - 1).Trim().ToLower().Replace("(", "").Replace(")", "");
		if (selectCmd.Length == 11)
			selectCmd += ", -";
		if (limitCmd.Length == 7)
			limitCmd += ", 0";
		yield return Respond(selectCmd.SplitFull(" ,"), selectCmd);
		yield return Respond(whereCmd.SplitFull(" ,"), whereCmd);
		yield return Respond(limitCmd.SplitFull(" ,"), limitCmd);
		yield return Click(1, 0);
	}

	private static readonly Type DataComponentType = ReflectionHelper.FindType("DataSetFactory", "simple-sql");
	private static readonly Type Data2ComponentType = ReflectionHelper.FindType("DataQueryGenerator", "simple-sql");

	private object targetQueryObj;
	private bool receivedAnswer;
}