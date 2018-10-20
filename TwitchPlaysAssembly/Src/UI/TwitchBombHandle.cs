using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class TwitchBombHandle : MonoBehaviour
{
	#region Public Fields
	public CanvasGroup CanvasGroup;

	public Text EdgeworkIDText;
	public Text EdgeworkText;
	public RectTransform EdgeworkWindowTransform;
	public RectTransform EdgeworkHighlightTransform;

	[HideInInspector]
	public BombCommander BombCommander;

	[HideInInspector]
	public CoroutineQueue CoroutineQueue;

	[HideInInspector]
	public int BombID = -1;
	#endregion

	#region Private Fields
	private string _code;
	private string _edgeworkCode;

	private string _bombName;
	public string BombName
	{
		get => _bombName;
		set
		{
			_bombName = value;
			if (BombMessageResponder.ModuleCameras != null) BombMessageResponder.ModuleCameras.UpdateHeader();
		}
	}
	#endregion

	#region Unity Lifecycle
	private void Awake()
	{
		_code = "bomb";
		_edgeworkCode = "edgework";
	}

	private void Start()
	{
		if (BombID > -1)
		{
			_code = "bomb" + (BombID + 1);
			_edgeworkCode = "edgework" + (BombID + 1);
		}

		EdgeworkIDText.text = $"!{_edgeworkCode}";
		EdgeworkText.text = TwitchPlaySettings.data.BlankBombEdgework;

		CanvasGroup.alpha = 1.0f;
		if (BombID > 0)
		{
			EdgeworkWindowTransform.localScale = Vector3.zero;
			EdgeworkHighlightTransform.localScale = Vector3.zero;
		}
	}

	private void OnDestroy() => StopAllCoroutines();
	#endregion

	#region Message Interface
	public IEnumerator OnMessageReceived(Message message)
	{
		string text = message.Text.Trim();
		string userNickName = message.UserNickName;
		bool isWhisper = message.IsWhisper;

		string internalCommand;
		Match match = Regex.Match(text, $"^{_code} (.+)", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			match = Regex.Match(text, $"^{_edgeworkCode}(?> (.+))?", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				internalCommand = match.Groups[1].Value;
				if (!string.IsNullOrEmpty(internalCommand) && (TwitchPlaySettings.data.EnableEdgeworkCommand || UserAccess.HasAccess(userNickName, AccessLevel.Mod, true)))
				{
					if (!IsAuthorizedDefuser(userNickName, isWhisper)) return null;
					EdgeworkText.text = internalCommand;
				}
				IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombEdgework, EdgeworkText.text), userNickName, !isWhisper);
			}
			return null;
		}

		internalCommand = match.Groups[1].Value;

		string internalCommandLower = internalCommand.ToLowerInvariant();
		string[] split = internalCommandLower.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		//Respond instantly to these commands without dropping "The Bomb", should the command be for "The Other Bomb" and vice versa.
		if (internalCommandLower.EqualsAny("timestamp", "date"))
		{
			//Some modules depend on the date/time the bomb, and therefore that Module instance has spawned, in the bomb defusers timezone.

			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombTimeStamp, BombCommander.BombTimeStamp), userNickName, !isWhisper);
		}
		else if (internalCommandLower.Equals("help"))
		{
			IRCConnection.SendMessage(TwitchPlaySettings.data.BombHelp, userNickName, !isWhisper);
		}
		else if (internalCommandLower.EqualsAny("time", "timer", "clock"))
		{
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombTimeRemaining, BombCommander.GetFullFormattedTime, BombCommander.GetFullStartingTime), userNickName, !isWhisper);
		}
		else if (internalCommandLower.EqualsAny("explode", "detonate", "endzenmode"))
		{
			if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true) || (OtherModes.ZenModeOn && internalCommandLower.Equals("endzenmode")))
			{
				if (internalCommandLower.Equals("endzenmode"))
				{
					if (isWhisper)
					{
						IRCConnection.SendMessage($"Sorry {userNickName}, you can't use the endzenmode command in a whisper.", userNickName, false);
						return null;
					}
					Leaderboard.Instance.GetRank(userNickName, out Leaderboard.LeaderboardEntry entry);
					if (entry.SolveScore >= TwitchPlaySettings.data.MinScoreForNewbomb || UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true))
					{
						return DelayBombExplosionCoroutine();
					}
					else
					{
						IRCConnection.SendMessage("Sorry, you don't have enough points to use the endzenmode command.");
						return null;
					}
				}
				else
				{
					return DelayBombExplosionCoroutine();
				}
			}

			return null;
		}
		else if (internalCommandLower.Equals("status") || internalCommandLower.Equals("info"))
		{
			int currentReward = TwitchPlaySettings.GetRewardBonus();
			if (OtherModes.TimeModeOn)
			{
				IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombStatusTimeMode, BombCommander.GetFullFormattedTime, BombCommander.GetFullStartingTime,
					OtherModes.GetAdjustedMultiplier(), BombCommander.BombSolvedModules, BombCommander.BombSolvableModules, currentReward), userNickName, !isWhisper);
			}
			else if (OtherModes.VSModeOn)
			{
				IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombStatusVsMode, BombCommander.GetFullFormattedTime,
					BombCommander.GetFullStartingTime, OtherModes.teamHealth, OtherModes.bossHealth, currentReward), userNickName, !isWhisper);
			}
			else
			{
				IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombStatus, BombCommander.GetFullFormattedTime, BombCommander.GetFullStartingTime,
					BombCommander.StrikeCount, BombCommander.StrikeLimit, BombCommander.BombSolvedModules, BombCommander.BombSolvableModules, currentReward), userNickName, !isWhisper);
			}
		}
		else if (internalCommandLower.Equals("pause") && UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
		{
			if (!BombCommander.TimerComponent.IsUpdating)
				return null;

			OtherModes.DisableLeaderboard();
			BombCommander.TimerComponent.StopTimer();
		}
		else if (internalCommandLower.Equals("unpause") && UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
		{
			if (!BombCommander.TimerComponent.IsUpdating)
				BombCommander.TimerComponent.StartTimer();
		}
		else if (split[0].EqualsAny("add", "increase", "change", "subtract", "decrease", "remove", "set"))
		{
			if (UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
			{
				bool negative = split[0].EqualsAny("subtract", "decrease", "remove");
				bool direct = split[0].EqualsAny("set");
				switch (split[1])
				{
					case "time":
					case "t":
						float time = 0;
						float originalTime = BombCommander.TimerComponent.TimeRemaining;
						Dictionary<string, float> timeLengths = new Dictionary<string, float>()
						{
							{ "ms", 0.001f },
							{ "s", 1 },
							{ "m", 60 },
							{ "h", 3600 },
							{ "d", 86400 },
							{ "w", 604800 },
							{ "y", 31536000 },
						};

						foreach (string part in split.Skip(2))
						{
							bool valid = false;
							foreach (string unit in timeLengths.Keys)
							{
								if (!part.EndsWith(unit) || !float.TryParse(part.Substring(0, part.Length - unit.Length), out float length)) continue;
								time += length * timeLengths[unit];
								valid = true;
								break;
							}

							if (!valid) return null;
						}

						time = (float) Math.Round((decimal) time, 2, MidpointRounding.AwayFromZero);
						if (!direct && Math.Abs(time) < 0.01f) break;
						if (negative) time = -time;

						if (direct)
							BombCommander.TimerComponent.TimeRemaining = time;
						else
							BombCommander.TimerComponent.TimeRemaining = BombCommander.CurrentTimer + time;

						if (originalTime < BombCommander.TimerComponent.TimeRemaining)
							OtherModes.DisableLeaderboard(true);

						IRCConnection.SendMessage(direct 
							? $"Set the bomb's timer to {Math.Abs(time < 0 ? 0 : time).FormatTime()}." 
							: $"{(time > 0 ? "Added" : "Subtracted")} {Math.Abs(time).FormatTime()} {(time > 0 ? "to" : "from")} the timer.", userNickName, !isWhisper);
						break;
					case "strikes":
					case "strike":
					case "s":
						if (int.TryParse(split[2], out int strikes) && (strikes != 0 || direct))
						{
							int originalStrikes = BombCommander.StrikeCount;
							if (negative) strikes = -strikes;

							if (direct && strikes < 0)
							{
								strikes = 0;
							}
							else if (!direct && (BombCommander.StrikeCount + strikes) < 0)
							{
								strikes = -BombCommander.StrikeCount; //Minimum of zero strikes. (Simon says is unsolvable with negative strikes.)
							}

							if (direct)
								BombCommander.StrikeCount = strikes;
							else
								BombCommander.StrikeCount += strikes;

							if (BombCommander.StrikeCount < originalStrikes)
								OtherModes.DisableLeaderboard(true);

							IRCConnection.SendMessage(direct 
								? $"Set the bomb's strike count to {Math.Abs(strikes)} {(Math.Abs(strikes) != 1 ? "strikes" : "strike")}." 
								: $"{(strikes > 0 ? "Added" : "Subtracted")} {Math.Abs(strikes)} {(Math.Abs(strikes) != 1 ? "strikes" : "strike")} {(strikes > 0 ? "to" : "from")} the bomb.", userNickName, !isWhisper);
							BombMessageResponder.ModuleCameras.UpdateStrikes();
						}
						break;
					case "strikelimit":
					case "sl":
					case "maxstrikes":
					case "ms":
						if (int.TryParse(split[2], out int maxStrikes) && (maxStrikes != 0 || direct))
						{
							int originalStrikeLimit = BombCommander.StrikeLimit;
							if (negative) maxStrikes = -maxStrikes;

							if (direct && maxStrikes < 0)
								maxStrikes = 0;
							else if (!direct && (BombCommander.StrikeLimit + maxStrikes) < 0)
								maxStrikes = -BombCommander.StrikeLimit;

							if (direct)
								BombCommander.StrikeLimit = maxStrikes;
							else
								BombCommander.StrikeLimit += maxStrikes;

							if (originalStrikeLimit < BombCommander.StrikeLimit)
								OtherModes.DisableLeaderboard(true);

							IRCConnection.SendMessage(direct 
								? $"Set the bomb's strike limit to {Math.Abs(maxStrikes)} {(Math.Abs(maxStrikes) != 1 ? "strikes" : "strike")}." 
								: $"{(maxStrikes > 0 ? "Added" : "Subtracted")} {Math.Abs(maxStrikes)} {(Math.Abs(maxStrikes) > 1 ? "strikes" : "strike")} {(maxStrikes > 0 ? "to" : "from")} the strike limit.", userNickName, !isWhisper);
							BombMessageResponder.ModuleCameras.UpdateStrikes();
						}
						break;
				}
			}

			return null;
		}
		else if (!IsAuthorizedDefuser(userNickName, isWhisper))
		{
			return null;
		}
		else
		{
			return RespondToCommandCoroutine(userNickName, internalCommand, isWhisper);
		}

		return null;
	}

	public IEnumerator HideMainUIWindow()
	{
		EdgeworkWindowTransform.localScale = Vector3.zero;
		EdgeworkHighlightTransform.localScale = Vector3.zero;
		IRCConnection.Instance.MainWindowTransform.localScale = Vector3.zero;
		IRCConnection.Instance.HighlightTransform.localScale = Vector3.zero;
		yield return null;
	}

	public IEnumerator ShowMainUIWindow()
	{
		EdgeworkWindowTransform.localScale = Vector3.one;
		EdgeworkHighlightTransform.localScale = Vector3.one;
		IRCConnection.Instance.MainWindowTransform.localScale = Vector3.one;
		IRCConnection.Instance.HighlightTransform.localScale = Vector3.one;
		yield return null;
	}

	public void CauseExplosionByModuleCommand(string message, string reason) => StartCoroutine(DelayBombExplosionCoroutine(message, reason, 0.1f));
	#endregion

	#region Private Methods
	private bool IsAuthorizedDefuser(string userNickName, bool isWhisper) => MessageResponder.IsAuthorizedDefuser(userNickName, isWhisper);

	private IEnumerator DelayBombExplosionCoroutine()
	{
		yield return DelayBombExplosionCoroutine(TwitchPlaySettings.data.BombDetonateCommand, "Detonate Command", 1.0f);
	}

	private IEnumerator DelayBombExplosionCoroutine(string message, string reason, float delay)
	{
		BombCommander.StrikeCount = BombCommander.StrikeLimit - 1;
		if (!string.IsNullOrEmpty(message))
			IRCConnection.SendMessage(message);
		yield return new WaitForSeconds(delay);
		BombCommander.CauseStrikesToExplosion(reason);
	}

	// ReSharper disable once UnusedParameter.Local
	private IEnumerator RespondToCommandCoroutine(string userNickName, string internalCommand, bool isWhisper, float fadeDuration = 0.1f)
	{
		IEnumerator commandResponseCoroutine = BombCommander.RespondToCommand(new Message(userNickName, null, internalCommand, isWhisper));
		while (commandResponseCoroutine.MoveNext())
		{
			if (commandResponseCoroutine.Current is string chatMessage)
			{
				if (chatMessage.StartsWith("sendtochat "))
				{
					IRCConnection.SendMessage(chatMessage.Substring(11));
				}
			}

			yield return commandResponseCoroutine.Current;
		}
	}
	#endregion
}
