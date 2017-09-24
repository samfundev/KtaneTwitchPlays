using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

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
        edgeworkText.text = string.Format("Not set, use !edgework <edgework> to set!{0}Use !bomb edgework or !bomb edgework 45 to view the bomb edges.", Environment.NewLine);

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
                    edgeworkText.text = internalCommand;
                }
                ircConnection.SendMessage("Edgework: " + edgeworkText.text);
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

        //Respond instantly to these commands without dropping "The Bomb", should the command be for "The Other Bomb" and vice versa.
        ICommandResponseNotifier notifier = message;
        if (internalCommand.Equals("timestamp", StringComparison.InvariantCultureIgnoreCase) || 
            internalCommand.Equals("date", StringComparison.InvariantCultureIgnoreCase))
        {
            //Some modules depend on the date/time the bomb, and therefore that Module instance has spawned, in the bomb defusers timezone.

            notifier.ProcessResponse(CommandResponse.Start);

            StringBuilder sb = new StringBuilder();
            sb.Append("The Date/Time this bomb started is ");
            sb.Append(string.Format("{0:F}", bombCommander.BombTimeStamp));
            ircConnection.SendMessage(sb.ToString());

            notifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (internalCommand.Equals("help", StringComparison.InvariantCultureIgnoreCase))
        {
            notifier.ProcessResponse(CommandResponse.Start);

            ircConnection.SendMessage("The Bomb: !bomb hold [pick up] | !bomb drop | !bomb turn [turn to the other side] | !bomb edgework [show the widgets on the sides] | !bomb top [show one side; sides are Top/Bottom/Left/Right | !bomb time [time remaining] | !bomb timestamp [bomb start time]");

            notifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (internalCommand.Equals("time", StringComparison.InvariantCultureIgnoreCase) ||
                 internalCommand.Equals("timer", StringComparison.InvariantCultureIgnoreCase) ||
                 internalCommand.Equals("clock", StringComparison.InvariantCultureIgnoreCase))
        {
            notifier.ProcessResponse(CommandResponse.Start);

            StringBuilder sb = new StringBuilder();
            sb.Append("/me panicBasket [");
            sb.Append(string.Format(bombCommander.GetFullFormattedTime));
            sb.Append("] out of [");
            sb.Append(string.Format(bombCommander.GetFullStartingTime));
            sb.Append("].");
            ircConnection.SendMessage(sb.ToString());

            notifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (internalCommand.Equals("explode", StringComparison.InvariantCultureIgnoreCase) ||
                internalCommand.Equals("detonate", StringComparison.InvariantCultureIgnoreCase))
            {
                if (UserAccess.HasAccess(userNickName, AccessLevel.Mod))
                {
                   return DelayBombExplosionCoroutine(notifier);

            }
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

    #endregion

    #region Private Methods
    private IEnumerator DelayBombExplosionCoroutine(ICommandResponseNotifier notifier)
    {
        notifier.ProcessResponse(CommandResponse.Start);

        ircConnection.SendMessage("panicBasket This bomb's gonna blow!");
        yield return new WaitForSeconds(1.0f);

        bombCommander.CauseStrikesToExplosion("Detonate Command");

        notifier.ProcessResponse(CommandResponse.EndNotComplete);
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
