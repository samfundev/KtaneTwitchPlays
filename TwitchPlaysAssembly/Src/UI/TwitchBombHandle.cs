using System;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Assets.Scripts.Records;

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
    public IRCConnection ircConnection = null;

    [HideInInspector]
    public BombCommander bombCommander = null;

    [HideInInspector]
    public CoroutineQueue coroutineQueue = null;

    [HideInInspector]
    public CoroutineCanceller coroutineCanceller = null;

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
        string internalCommand;
        Match match = Regex.Match(text, string.Format("^!{0} (.+)", _code), RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            match = Regex.Match(text, string.Format("^!{0}(?> (.+))?", _edgeworkCode), RegexOptions.IgnoreCase);
            if (match.Success)
            {
                internalCommand = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(internalCommand))
                {
                    if (!IsAuthorizedDefuser(userNickName)) return null;
                    edgeworkText.text = internalCommand;
                }
                ircConnection.SendMessage(TwitchPlaySettings.data.BombEdgework,edgeworkText.text);
            }
            return null;
        }

        internalCommand = match.Groups[1].Value;

        TwitchMessage message = (TwitchMessage)Instantiate(messagePrefab, messageScrollContents.transform, false);
        if (string.IsNullOrEmpty(userColor))
        {
            message.SetMessage(string.Format("<b>{0}</b>: {1}", userNickName, internalCommand));
        }
        else
        {
            message.SetMessage(string.Format("<b><color={2}>{0}</color></b>: {1}", userNickName, internalCommand, userColor));
        }

        string internalCommandLower = internalCommand.ToLowerInvariant();
		string[] split = internalCommandLower.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		//Respond instantly to these commands without dropping "The Bomb", should the command be for "The Other Bomb" and vice versa.
		ICommandResponseNotifier notifier = message;
		if (internalCommandLower.EqualsAny("timestamp", "date"))
		{
			//Some modules depend on the date/time the bomb, and therefore that Module instance has spawned, in the bomb defusers timezone.

			notifier.ProcessResponse(CommandResponse.Start);
			ircConnection.SendMessage(TwitchPlaySettings.data.BombTimeStamp, bombCommander.BombTimeStamp);
			notifier.ProcessResponse(CommandResponse.EndNotComplete);
		}
		else if (internalCommandLower.Equals("help"))
		{
			notifier.ProcessResponse(CommandResponse.Start);

			ircConnection.SendMessage(TwitchPlaySettings.data.BombHelp);

			notifier.ProcessResponse(CommandResponse.EndNotComplete);
		}
		else if (internalCommandLower.EqualsAny("time", "timer", "clock"))
		{
			notifier.ProcessResponse(CommandResponse.Start);
			ircConnection.SendMessage(TwitchPlaySettings.data.BombTimeRemaining, bombCommander.GetFullFormattedTime, bombCommander.GetFullStartingTime);
			notifier.ProcessResponse(CommandResponse.EndNotComplete);
		}
		else if (internalCommandLower.EqualsAny("explode", "detonate"))
		{
			if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
			{
                return DelayBombExplosionCoroutine(notifier);
			}
		}
		else if (split[0].EqualsAny("add", "increase", "change", "subtract", "decrease", "remove"))
		{
			if (UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
			{
				bool negitive = split[0].EqualsAny("subtract", "decrease", "remove");
				switch (split[1])
				{
					case "time":
					case "t":
						float time = 0;
						Dictionary<string, float> timeLengths = new Dictionary<string, float>()
						{
							{ "ms", 0.001f },
							{ "s", 1 },
							{ "m", 60 },
							{ "h", 3600 },
							{ "d", 86400 },
						};

						foreach (string part in split.Skip(2))
						{
							bool valid = false;
							foreach (string name in timeLengths.Keys)
							{
								if (part.EndsWith(name))
								{
									float length;
									if (float.TryParse(part.Substring(0, part.Length - name.Length), out length))
									{
										time += length * timeLengths[name];
										valid = true;
										break;
									}
								}
							}

							if (!valid) return null;
						}

						if (time == 0) break;
						if (negitive) time = -time;

					    bombCommander.timerComponent.TimeRemaining = bombCommander.CurrentTimer + time;
						ircConnection.SendMessage("{0} {1} {2} the timer.", time > 0 ? "Added" : "Subtracted", Math.Abs(time).FormatTime(), time > 0 ? "to" : "from");
						break;
					case "strikes":
					case "strike":
					case "s":
						int strikes;
						if (int.TryParse(split[2], out strikes) && strikes != 0)
						{
							if (negitive) strikes = -strikes;

						    if ((bombCommander.StrikeCount + strikes) < 0)
						    {
						        strikes = -bombCommander.StrikeCount;   //Minimum of zero strikes. (Simon says is unsolvable with negative strikes.)
						    }
						    bombCommander.Bomb.NumStrikes = bombCommander.StrikeCount + strikes;
						    ircConnection.SendMessage("{0} {1} {2} {3} the bomb.", strikes > 0 ? "Added" : "Subtracted", Math.Abs(strikes), strikes > 1 ? "strikes" : "strike", strikes > 0 ? "to" : "from");
                            BombMessageResponder.moduleCameras.UpdateStrikes();
							HandleStrikeChanges();
						}
						break;
					case "strikelimit":
					case "sl":
					case "maxstrikes":
					case "ms":
						int maxStrikes;
						if (int.TryParse(split[2], out maxStrikes) && maxStrikes != 0)
						{
							if (negitive) maxStrikes = -maxStrikes;

						    bombCommander.Bomb.NumStrikesToLose = bombCommander.StrikeLimit + maxStrikes;
							ircConnection.SendMessage("{0} {1} {2} {3} the strike limit.", maxStrikes > 0 ? "Added" : "Subtracted", Math.Abs(maxStrikes), maxStrikes > 1 ? "strikes" : "strike", maxStrikes > 0 ? "to" : "from");
						    BombMessageResponder.moduleCameras.UpdateStrikes();
                            BombMessageResponder.moduleCameras.UpdateStrikeLimit();
							HandleStrikeChanges();
						}
						break;
				}

				return null;
			}
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
        StartCoroutine(DelayBombExplosionCoroutine(message, reason,0.1f));
    }
    #endregion

    #region Private Methods
    private void HandleStrikeChanges()
    {
        int strikeLimit = bombCommander.StrikeLimit;
        int strikeCount = Math.Min(bombCommander.StrikeCount, strikeLimit);

        RecordManager RecordManager = RecordManager.Instance;
        GameRecord GameRecord = RecordManager.GetCurrentRecord();
        StrikeSource[] Strikes = GameRecord.Strikes;
        if (Strikes.Length != strikeLimit)
        {
            StrikeSource[] newStrikes = new StrikeSource[Math.Max(strikeLimit, 1)];
            Array.Copy(Strikes, newStrikes, Math.Min(Strikes.Length, newStrikes.Length));
            GameRecord.Strikes = newStrikes;
        }

        if (strikeCount == strikeLimit)
        {
            if (strikeLimit < 1)
            {
                bombCommander.Bomb.NumStrikesToLose = 1;
                strikeLimit = 1;
            }
            bombCommander.Bomb.NumStrikes = strikeLimit - 1;
            CommonReflectedTypeInfo.GameRecordCurrentStrikeIndexField.SetValue(GameRecord, strikeLimit - 1);
            bombCommander.CauseStrike("Strike count / limit changed.");
        }
        else
        {
            Debug.Log(string.Format("[Bomb] Strike from TwitchPlays! {0} / {1} strikes", bombCommander.StrikeCount, bombCommander.StrikeLimit));
            CommonReflectedTypeInfo.GameRecordCurrentStrikeIndexField.SetValue(GameRecord, strikeCount);
            //MasterAudio.PlaySound3DAtTransformAndForget("strike", base.transform, 1f, null, 0f, null);
            float[] rates = {1, 1.25f, 1.5f, 1.75f, 2};
            bombCommander.timerComponent.SetRateModifier(rates[Math.Min(strikeCount, 4)]);
            bombCommander.Bomb.StrikeIndicator.StrikeCount = strikeCount;
        }
    }

    private bool IsAuthorizedDefuser(string userNickName)
    {
        if (userNickName.EqualsAny(nameText.text,"Bomb Factory"))
            return true;
        bool result = (TwitchPlaySettings.data.EnableTwitchPlaysMode || UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true));
        if (!result)
            ircConnection.SendMessage(TwitchPlaySettings.data.TwitchPlaysDisabled, userNickName);

        return result;
    }

    private IEnumerator DelayBombExplosionCoroutine(ICommandResponseNotifier notifier)
    {
        notifier.ProcessResponse(CommandResponse.Start);
        yield return DelayBombExplosionCoroutine(TwitchPlaySettings.data.BombDetonateCommand, "Detonate Command", 1.0f);
        notifier.ProcessResponse(CommandResponse.EndNotComplete);
    }

    private IEnumerator DelayBombExplosionCoroutine(string message, string reason, float delay)
    {
        bombCommander.Bomb.NumStrikes = bombCommander.StrikeLimit - 1;
        HandleStrikeChanges();
        if (!string.IsNullOrEmpty(message))
            ircConnection.SendMessage(message);
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

        IEnumerator commandResponseCoroutine = bombCommander.RespondToCommand(userNickName, internalCommand, message, ircConnection);
        while (commandResponseCoroutine.MoveNext())
        {
            string chatmessage = commandResponseCoroutine.Current as string;
            if (chatmessage != null)
            {
                if(chatmessage.StartsWith("sendtochat "))
                {
                    ircConnection.SendMessage(chatmessage.Substring(11));
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
