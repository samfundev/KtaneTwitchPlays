using System;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TwitchBombHandle : MonoBehaviour
{
	#region Public Fields
	public TwitchMessage messagePrefab = null;

	public CanvasGroup canvasGroup = null;
	public CanvasGroup highlightGroup = null;
	public Text idText = null;
	public Text nameText = null;
	public ScrollRect messageScroll = null;
	public GameObject messageScrollContents = null;
	public RectTransform mainWindowTransform = null;
	public RectTransform highlightTransform = null;

	public Text edgeworkIDText = null;
	public Text edgeworkText = null;
	public RectTransform edgeworkWindowTransform = null;
	public RectTransform edgeworkHighlightTransform = null;

	[HideInInspector]
	public BombCommander bombCommander = null;

	[HideInInspector]
	public CoroutineQueue coroutineQueue = null;

	[HideInInspector]
	public int bombID = -1;
	#endregion

	#region Private Fields
	private string _code = null;
	private string _edgeworkCode = null;
	#endregion

	#region Unity Lifecycle
	private void Awake()
	{
		_code = "bomb";
		_edgeworkCode = "edgework";
	}

	private void Start()
	{
		if (bombID > -1)
		{
			_code = "bomb" + (bombID + 1);
			_edgeworkCode = "edgework" + (bombID + 1);
		}

		idText.text = string.Format("!{0}", _code);
		edgeworkIDText.text = string.Format("!{0}", _edgeworkCode);
		edgeworkText.text = TwitchPlaySettings.data.BlankBombEdgework;

		canvasGroup.alpha = 1.0f;
		highlightGroup.alpha = 0.0f;
		if (bombID > 0)
		{
			edgeworkWindowTransform.localScale = Vector3.zero;
			edgeworkHighlightTransform.localScale = Vector3.zero;
			mainWindowTransform.localScale = Vector3.zero;
			highlightTransform.localScale = Vector3.zero;
		}
	}

	private void LateUpdate()
	{
		messageScroll.verticalNormalizedPosition = 0.0f;
	}

	private void OnDestroy()
	{
		StopAllCoroutines();
	}
	#endregion

	#region Message Interface
	public IEnumerator OnMessageReceived(string userNickName, string userColor, string text)
	{
		text = text.Trim();
		string internalCommand;
		Match match = Regex.Match(text, string.Format("^{0} (.+)", _code), RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			match = Regex.Match(text, string.Format("^{0}(?> (.+))?", _edgeworkCode), RegexOptions.IgnoreCase);
			if (match.Success)
			{
				internalCommand = match.Groups[1].Value;
				if (!string.IsNullOrEmpty(internalCommand) && (TwitchPlaySettings.data.EnableEdgeworkCommand || UserAccess.HasAccess(userNickName, AccessLevel.Mod, true)))
				{
					if (!IsAuthorizedDefuser(userNickName)) return null;
					edgeworkText.text = internalCommand;
				}
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.BombEdgework, edgeworkText.text);
			}
			return null;
		}

		internalCommand = match.Groups[1].Value;

		TwitchMessage message = Instantiate(messagePrefab, messageScrollContents.transform, false);
		message.SetMessage(string.IsNullOrEmpty(userColor) 
			? string.Format("<b>{0}</b>: {1}", userNickName, internalCommand) 
			: string.Format("<b><color={2}>{0}</color></b>: {1}", userNickName, internalCommand, userColor));

		string internalCommandLower = internalCommand.ToLowerInvariant();
		string[] split = internalCommandLower.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		//Respond instantly to these commands without dropping "The Bomb", should the command be for "The Other Bomb" and vice versa.
		ICommandResponseNotifier notifier = message;
		if (internalCommandLower.EqualsAny("timestamp", "date"))
		{
			//Some modules depend on the date/time the bomb, and therefore that Module instance has spawned, in the bomb defusers timezone.

			notifier.ProcessResponse(CommandResponse.Start);
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.BombTimeStamp, bombCommander.BombTimeStamp);
			notifier.ProcessResponse(CommandResponse.EndNotComplete);
		}
		else if (internalCommandLower.Equals("help"))
		{
			notifier.ProcessResponse(CommandResponse.Start);

			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.BombHelp);

			notifier.ProcessResponse(CommandResponse.EndNotComplete);
		}
		else if (internalCommandLower.EqualsAny("time", "timer", "clock"))
		{
			notifier.ProcessResponse(CommandResponse.Start);
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.BombTimeRemaining, bombCommander.GetFullFormattedTime, bombCommander.GetFullStartingTime);
			notifier.ProcessResponse(CommandResponse.EndNotComplete);
		}
		else if (internalCommandLower.EqualsAny("explode", "detonate", "endzenmode"))
		{
			if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true) || (OtherModes.ZenModeOn && internalCommandLower.Equals("endzenmode")))
			{
				if (internalCommandLower.Equals("endzenmode"))
				{
					Leaderboard.Instance.GetRank(userNickName, out Leaderboard.LeaderboardEntry entry);
					if (entry.SolveScore >= TwitchPlaySettings.data.MinScoreForNewbomb || UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true))
					{
						return DelayBombExplosionCoroutine(notifier);
					}
					else
					{
						IRCConnection.Instance.SendMessage("Sorry, you don't have enough points to use the endzenmode command.");
						return null;
					}
				}
				else
				{
					return DelayBombExplosionCoroutine(notifier);
				}
			}

			return null;
		}
		else if (internalCommandLower.Equals("status") || internalCommandLower.Equals("info"))
		{
			int currentReward = TwitchPlaySettings.GetRewardBonus();
			if (OtherModes.TimeModeOn)
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.BombStatusTimeMode, bombCommander.GetFullFormattedTime, bombCommander.GetFullStartingTime,
					OtherModes.GetAdjustedMultiplier(), bombCommander.bombSolvedModules, bombCommander.bombSolvableModules, currentReward);
			}
			else if (OtherModes.VSModeOn)
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.BombStatusVsMode, bombCommander.GetFullFormattedTime,
					bombCommander.GetFullStartingTime, OtherModes.teamHealth, OtherModes.bossHealth, currentReward);
			}
			else
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.BombStatus, bombCommander.GetFullFormattedTime, bombCommander.GetFullStartingTime,
					bombCommander.StrikeCount, bombCommander.StrikeLimit, bombCommander.bombSolvedModules, bombCommander.bombSolvableModules, currentReward);
			}
		}
		else if (internalCommandLower.Equals("pause") && UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
		{
			if (!bombCommander.timerComponent.IsUpdating)
				return null;

			OtherModes.DisableLeaderboard();
			bombCommander.timerComponent.StopTimer();
		}
		else if (internalCommandLower.Equals("unpause") && UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
		{
			if (!bombCommander.timerComponent.IsUpdating)
				bombCommander.timerComponent.StartTimer();
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
						float originalTime = bombCommander.timerComponent.TimeRemaining;
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
						if (!direct && Math.Abs(time) == 0) break;
						if (negative) time = -time;

						if (direct)
							bombCommander.timerComponent.TimeRemaining = time;
						else
							bombCommander.timerComponent.TimeRemaining = bombCommander.CurrentTimer + time;

						if(originalTime < bombCommander.timerComponent.TimeRemaining)
							OtherModes.DisableLeaderboard(true);

						if (direct)
							IRCConnection.Instance.SendMessage("Set the bomb's timer to {0}.", Math.Abs(time < 0 ? 0 : time).FormatTime());
						else
							IRCConnection.Instance.SendMessage("{0} {1} {2} the timer.", time > 0 ? "Added" : "Subtracted", Math.Abs(time).FormatTime(), time > 0 ? "to" : "from");
						break;
					case "strikes":
					case "strike":
					case "s":
						if (int.TryParse(split[2], out int strikes) && (strikes != 0 || direct))
						{
							int originalStrikes = bombCommander.StrikeCount;
							if (negative) strikes = -strikes;

							if (direct && strikes < 0)
							{
								strikes = 0;
							}
							else if (!direct && (bombCommander.StrikeCount + strikes) < 0)
							{
								strikes = -bombCommander.StrikeCount; //Minimum of zero strikes. (Simon says is unsolvable with negative strikes.)
							}

							if (direct)
								bombCommander.StrikeCount = strikes;
							else
								bombCommander.StrikeCount += strikes;

							if (bombCommander.StrikeCount < originalStrikes)
								OtherModes.DisableLeaderboard(true);

							if (direct)
								IRCConnection.Instance.SendMessage("Set the bomb's strike count to {0} {1}.", Math.Abs(strikes), Math.Abs(strikes) != 1 ? "strikes" : "strike");
							else
								IRCConnection.Instance.SendMessage("{0} {1} {2} {3} the bomb.", strikes > 0 ? "Added" : "Subtracted", Math.Abs(strikes), Math.Abs(strikes) != 1 ? "strikes" : "strike", strikes > 0 ? "to" : "from");
							BombMessageResponder.moduleCameras.UpdateStrikes();
						}
						break;
					case "strikelimit":
					case "sl":
					case "maxstrikes":
					case "ms":
						if (int.TryParse(split[2], out int maxStrikes) && (maxStrikes != 0 || direct))
						{
							int originalStrikeLimit = bombCommander.StrikeLimit;
							if (negative) maxStrikes = -maxStrikes;

							if (direct && maxStrikes < 0)
								maxStrikes = 0;
							else if (!direct && (bombCommander.StrikeLimit + maxStrikes) < 0)
								maxStrikes = -bombCommander.StrikeLimit;

							if (direct)
								bombCommander.StrikeLimit = maxStrikes;
							else
								bombCommander.StrikeLimit += maxStrikes;

							if (originalStrikeLimit < bombCommander.StrikeLimit)
								OtherModes.DisableLeaderboard(true);

							if (direct)
								IRCConnection.Instance.SendMessage("Set the bomb's strike limit to {0} {1}.", Math.Abs(maxStrikes), Math.Abs(maxStrikes) != 1 ? "strikes" : "strike");
							else
								IRCConnection.Instance.SendMessage("{0} {1} {2} {3} the strike limit.", maxStrikes > 0 ? "Added" : "Subtracted", Math.Abs(maxStrikes), Math.Abs(maxStrikes) > 1 ? "strikes" : "strike", maxStrikes > 0 ? "to" : "from");
							BombMessageResponder.moduleCameras.UpdateStrikes();
							BombMessageResponder.moduleCameras.UpdateStrikeLimit();
						}
						break;
				}
			}

			return null;
		}
		else if (!IsAuthorizedDefuser(userNickName))
		{
			return null;
		}
		else
		{
			return RespondToCommandCoroutine(userNickName, internalCommand, message);
		}

		return null;
	}

	public IEnumerator HideMainUIWindow()
	{
		edgeworkWindowTransform.localScale = Vector3.zero;
		edgeworkHighlightTransform.localScale = Vector3.zero;
		mainWindowTransform.localScale = Vector3.zero;
		highlightTransform.localScale = Vector3.zero;
		yield return null;
	}

	public IEnumerator ShowMainUIWindow()
	{
		edgeworkWindowTransform.localScale = Vector3.one;
		edgeworkHighlightTransform.localScale = Vector3.one;
		mainWindowTransform.localScale = Vector3.one;
		highlightTransform.localScale = Vector3.one;
		yield return null;
	}

	public void CauseExplosionByModuleCommand(string message, string reason)
	{
		StartCoroutine(DelayBombExplosionCoroutine(message, reason, 0.1f));
	}
	#endregion

	#region Private Methods
	private bool IsAuthorizedDefuser(string userNickName)
	{
		return MessageResponder.IsAuthorizedDefuser(userNickName);
	}

	private IEnumerator DelayBombExplosionCoroutine(ICommandResponseNotifier notifier)
	{
		notifier.ProcessResponse(CommandResponse.Start);
		yield return DelayBombExplosionCoroutine(TwitchPlaySettings.data.BombDetonateCommand, "Detonate Command", 1.0f);
		notifier.ProcessResponse(CommandResponse.EndNotComplete);
	}

	private IEnumerator DelayBombExplosionCoroutine(string message, string reason, float delay)
	{
		bombCommander.StrikeCount = bombCommander.StrikeLimit - 1;
		if (!string.IsNullOrEmpty(message))
			IRCConnection.Instance.SendMessage(message);
		yield return new WaitForSeconds(delay);
		bombCommander.CauseStrikesToExplosion(reason);
	}

	private IEnumerator RespondToCommandCoroutine(string userNickName, string internalCommand, ICommandResponseNotifier message, float fadeDuration = 0.1f)
	{
		float time = Time.time;
		while (Time.time - time < fadeDuration)
		{
			float lerp = (Time.time - time) / fadeDuration;
			highlightGroup.alpha = Mathf.Lerp(0.0f, 1.0f, lerp);
			yield return null;
		}
		highlightGroup.alpha = 1.0f;

		IEnumerator commandResponseCoroutine = bombCommander.RespondToCommand(userNickName, internalCommand, message);
		while (commandResponseCoroutine.MoveNext())
		{
			if (commandResponseCoroutine.Current is string chatmessage)
			{
				if(chatmessage.StartsWith("sendtochat "))
				{
					IRCConnection.Instance.SendMessage(chatmessage.Substring(11));
				}
			}

			yield return commandResponseCoroutine.Current;
		}

		time = Time.time;
		while (Time.time - time < fadeDuration)
		{
			float lerp = (Time.time - time) / fadeDuration;
			highlightGroup.alpha = Mathf.Lerp(1.0f, 0.0f, lerp);
			yield return null;
		}
		highlightGroup.alpha = 0.0f;
	}
	#endregion    
}
