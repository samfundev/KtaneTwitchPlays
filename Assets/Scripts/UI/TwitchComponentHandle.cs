using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class TwitchComponentHandle : MonoBehaviour
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    #region Public Fields
    public TwitchMessage messagePrefab = null;
    public Image unsupportedPrefab = null;
    public Image idBannerPrefab = null;

    public CanvasGroup canvasGroup = null;
    public CanvasGroup highlightGroup = null;
    public CanvasGroup canvasGroupMultiDecker = null;
    public Text headerText = null;
    public Text idText = null;
    public Text idTextMultiDecker = null;
    public ScrollRect messageScroll = null;
    public GameObject messageScrollContents = null;

    public Image upArrow = null;
    public Image downArrow = null;
    public Image leftArrow = null;
    public Image rightArrow = null;

    public Image upArrowHighlight = null;
    public Image downArrowHighlight = null;
    public Image leftArrowHighlight = null;
    public Image rightArrowHighlight = null;

    public Color claimedBackgroundColour = new Color(255, 0, 0);
    public Color solvedBackgroundColor = new Color(0,128,0);
    public Color markedBackgroundColor = new Color(0, 0, 0);

    public AudioSource takeModuleSound = null;


    [HideInInspector]
    public IRCConnection ircConnection = null;

    [HideInInspector]
    public BombCommander bombCommander = null;

    [HideInInspector]
    public MonoBehaviour bombComponent = null;

    [HideInInspector]
    public ComponentTypeEnum componentType = ComponentTypeEnum.Empty;

    [HideInInspector]
    public Vector3 basePosition = Vector3.zero;

    [HideInInspector]
    public Vector3 idealHandlePositionOffset = Vector3.zero;

    [HideInInspector]
    public Direction direction = Direction.Up;

    [HideInInspector]
    public CoroutineQueue coroutineQueue = null;

    [HideInInspector]
    public CoroutineCanceller coroutineCanceller = null;

    [HideInInspector]
    public Leaderboard leaderboard = null;

    [HideInInspector]
    public bool claimed { get { return (playerName != null); } }

    [HideInInspector]
    public int bombID;

    #endregion

    #region Private Fields
    private string _code = null;
    private ComponentSolver _solver = null;
    private Color unclaimedBackgroundColor = new Color(0, 0, 0);
    private string playerName = null;
    private bool _solved = false;
    #endregion

    #region Private Statics
    private static List<TwitchComponentHandle> _unsupportedComponents = new List<TwitchComponentHandle>();
    private static List<BombCommander> _bombCommanders = new List<BombCommander>();
    private static int _nextID = 0;
    private static int GetNewID()
    {
        return ++_nextID;
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _code = GetNewID().ToString();
    }

    private void Start()
    {
        if (bombComponent != null)
        {
            headerText.text = (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(bombComponent, null);
        }

        idText.text = string.Format("!{0}", _code);
        idTextMultiDecker.text = _code;

        canvasGroup.alpha = 0.0f;
        highlightGroup.alpha = 0.0f;

        canvasGroupMultiDecker.alpha = bombCommander.multiDecker ? 1.0f : 0.0f;

        unclaimedBackgroundColor = idBannerPrefab.GetComponent<Image>().color;

        try
        {
            _solver = ComponentSolverFactory.CreateSolver(bombCommander, bombComponent, componentType, ircConnection, coroutineCanceller);
            if (_solver != null)
            {
                _solver.Code = _code;
                _solver.ComponentHandle = this;
                Vector3 pos = canvasGroupMultiDecker.transform.localPosition;
                canvasGroupMultiDecker.transform.localPosition = new Vector3(_solver.modInfo.statusLightLeft ? -pos.x : pos.x, pos.y, _solver.modInfo.statusLightDown ? -pos.z : pos.z);
                
                /*Vector3 angle = canvasGroupMultiDecker.transform.eulerAngles;
                canvasGroupMultiDecker.transform.localEulerAngles = new Vector3(angle.x, _solver.modInfo.chatRotation, angle.z);
                angle = canvasGroupMultiDecker.transform.localEulerAngles;
                canvasGroup.transform.localEulerAngles = new Vector3(angle.x, _solver.modInfo.chatRotation, angle.z);

                switch ((int) _solver.modInfo.chatRotation)
                {
                    case 90:
                    case -270:
                        switch (direction)
                        {
                            case Direction.Up:
                                direction = Direction.Left;
                                break;
                            case Direction.Left:
                                direction = Direction.Down;
                                break;
                            case Direction.Down:
                                direction = Direction.Right;
                                break;
                            case Direction.Right:
                                direction = Direction.Up;
                                break;
                        }
                        break;

                    case 180:
                    case -180:
                        switch (direction)
                        {
                            case Direction.Up:
                                direction = Direction.Down;
                                break;
                            case Direction.Left:
                                direction = Direction.Right;
                                break;
                            case Direction.Down:
                                direction = Direction.Up;
                                break;
                            case Direction.Right:
                                direction = Direction.Left;
                                break;
                        }
                        break;

                    case 270:
                    case -90:
                        switch (direction)
                        {
                            case Direction.Up:
                                direction = Direction.Right;
                                break;
                            case Direction.Left:
                                direction = Direction.Up;
                                break;
                            case Direction.Down:
                                direction = Direction.Left;
                                break;
                            case Direction.Right:
                                direction = Direction.Down;
                                break;
                        }
                        break;
                }*/
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            unsupportedPrefab.gameObject.SetActive(true);
            idBannerPrefab.gameObject.SetActive(false);
            canvasGroupMultiDecker.alpha = 0.0f;
            _unsupportedComponents.Add(this);

            if (TwitchPlaySettings.data.EnableTwitchPlaysMode && !TwitchPlaySettings.data.EnableInteractiveMode)
            {
                Debug.Log("[TwitchPlays] An unimplemented module was added to a bomb, solving module.");
            }
        }

        if (!_bombCommanders.Contains(bombCommander))
        {
            _bombCommanders.Add(bombCommander);
        }

        Arrow.gameObject.SetActive(true);
        HighlightArrow.gameObject.SetActive(true);
    }

    public static bool SolveUnsupportedModules()
    {
        bool result = _unsupportedComponents.Count > 0;
        foreach (TwitchComponentHandle handle in _unsupportedComponents)
        {
            CommonReflectedTypeInfo.HandlePassMethod.Invoke(handle.bombComponent, null);
        }
        if (result)
        {
            RemoveSolveBasedModules();
        }
        _unsupportedComponents.Clear();
        return result;
    }

    public static void RemoveSolveBasedModules()
    {
        foreach (BombCommander commander in _bombCommanders)
        {
            commander.RemoveSolveBasedModules();
        }
        _bombCommanders.Clear();
    }

    public static void ClearUnsupportedModules()
    {
        _bombCommanders.Clear();
        _unsupportedComponents.Clear();
    }

    private void LateUpdate()
    {
        if (!bombCommander.multiDecker)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 componentForward = transform.up;

            float angle = Vector3.Angle(cameraForward, -componentForward);
            float lerpAmount = Mathf.InverseLerp(60.0f, 20.0f, angle);
            lerpAmount = Mathf.Lerp(canvasGroup.alpha, lerpAmount, Time.deltaTime * 5.0f);
            canvasGroup.alpha = lerpAmount;
            transform.localPosition = basePosition + Vector3.Lerp(Vector3.zero, idealHandlePositionOffset, Mathf.SmoothStep(0.0f, 1.0f, lerpAmount));
            messageScroll.verticalNormalizedPosition = 0.0f;
        }
    }

    public void OnPass()
    {
        canvasGroupMultiDecker.alpha = 0.0f;
        _solved = true;
        if (playerName != null)
        {
            ClaimedList.Remove(playerName);
        }
        if (TakeInProgress != null)
        {
            StopCoroutine(TakeInProgress);
            TakeInProgress = null;
        }
    }

    public static void ResetId()
    {
        _nextID = 0;
    }
    #endregion

    public IEnumerator TakeModule(string userNickName, string targetModule)
    {
        if (takeModuleSound != null)
        {
            takeModuleSound.time = 0.0f;
            takeModuleSound.Play();
        }
        yield return new WaitForSecondsRealtime(60.0f);
        SetBannerColor(unclaimedBackgroundColor);
        if (playerName != null)
        {
            ircConnection.SendMessage(string.Format("/me {1} has released Module {0} ({2}).", targetModule, playerName, headerText.text));
            ClaimedList.Remove(playerName);
            playerName = null;
            TakeInProgress = null;
        }
    }

    public IEnumerator ReleaseModule(string player, string userNickName)
    {
        if (_solved)
        {
            yield break;
        }
        if (!UserAccess.HasAccess(userNickName, AccessLevel.Mod))
        {
            yield return new WaitForSeconds(TwitchPlaySettings.data.ClaimCooldownTime);
        }
        if(!_solved)
        {
            ClaimedList.Remove(player);
        }
    }

    public string ClaimModule(string userNickName, string targetModule)
    {
        if (playerName == null)
        {
            if (ClaimedList.Count(nick => nick.Equals(userNickName)) >= TwitchPlaySettings.data.ModuleClaimLimit && !_solved)
            {
                return string.Format(TwitchPlaySettings.data.TooManyClaimed, userNickName, TwitchPlaySettings.data.ModuleClaimLimit);
            }
            else
            {
                if (!_solved)
                {
                    ClaimedList.Add(userNickName);
                }
                SetBannerColor(claimedBackgroundColour);
                playerName = userNickName;
                return string.Format("/me {1} has claimed Module {0} ({2}).", targetModule, playerName, headerText.text);
            }
        }
        return null;
    }

    public IEnumerator TakeInProgress = null;
    public static List<string> ClaimedList = new List<string>(); 

    #region Message Interface
    public IEnumerator OnMessageReceived(string userNickName, string userColor, string text)
    {
        Match match = Regex.Match(text, string.Format("^!({0}) (.+)", _code), RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        string targetModule = match.Groups[1].Value;
        string internalCommand = match.Groups[2].Value;

    string messageOut = null;
        if ((internalCommand.StartsWith("manual", StringComparison.InvariantCultureIgnoreCase)) || (internalCommand.Equals("help", StringComparison.InvariantCultureIgnoreCase)))
        {
            string manualText = null;
            string manualType = "html";
            if ( (internalCommand.Length > 7) && (internalCommand.Substring(7) == "pdf") )
            {
                manualType = "pdf";
            }
            if (string.IsNullOrEmpty(_solver.modInfo.manualCode)) {
                manualText = headerText.text;
            }
            else {
                manualText = _solver.modInfo.manualCode;
            }
            if (manualText.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                manualText.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                messageOut = string.Format("{0} : {1} : {2}", headerText.text, _solver.modInfo.helpText, manualText);
            }
            else
            {
                //messageOut = string.Format("{0}: {1}", headerText.text, TwitchPlaysService.urlHelper.ManualFor(manualText, manualType));
                messageOut = string.Format("{0} : {1} : {2}", headerText.text, _solver.modInfo.helpText, TwitchPlaysService.urlHelper.ManualFor(manualText, manualType));
            }
        }
        else if (Regex.IsMatch(internalCommand, "^(bomb|queue) (turn( a?round)?|flip|spin)$", RegexOptions.IgnoreCase) && !_solved)
        {
            if (!_solver._turnQueued)
            {
                _solver._turnQueued = true;
                StartCoroutine(_solver.TurnBombOnSolve());
            }
            messageOut = string.Format("/me Turning to the other side when Module {0} ({1}) is solved", targetModule, headerText.text);
        }
        else if (Regex.IsMatch(internalCommand, "^cancel (bomb|queue) (turn( a?round)?|flip|spin)$", RegexOptions.IgnoreCase) && !_solved)
        {
            _solver._turnQueued = false;
            messageOut = string.Format("/me Bomb turn on Module {0} ({1}) solve cancelled", targetModule, headerText.text);
        }
        else if (internalCommand.Equals("claim", StringComparison.InvariantCultureIgnoreCase))
        {
            messageOut = ClaimModule(userNickName, targetModule);
        }
        else if (internalCommand.Equals("unclaim", StringComparison.InvariantCultureIgnoreCase))
        {
            if (((playerName != null) && (playerName == userNickName)) || (UserAccess.HasAccess(userNickName, AccessLevel.Mod)))
            {
                if (TakeInProgress != null)
                {
                    StopCoroutine(TakeInProgress);
                    TakeInProgress = null;
                }
                StartCoroutine(ReleaseModule(playerName, userNickName));
                SetBannerColor(unclaimedBackgroundColor);
                messageOut = string.Format("/me {1} has released Module {0} ({2}).", targetModule, playerName, headerText.text);
                playerName = null;
            }
        }
        else if (internalCommand.Equals("solved", StringComparison.InvariantCultureIgnoreCase))
        {
            if (UserAccess.HasAccess(userNickName, AccessLevel.Mod))
            {
                SetBannerColor(solvedBackgroundColor);
                playerName = null;
                messageOut = string.Format("/me {1} says module {0} ({2}) is ready to be submitted", targetModule, userNickName, headerText.text);
            }
        }
        else if (internalCommand.StartsWith("assign", StringComparison.InvariantCultureIgnoreCase))
        {
            if (UserAccess.HasAccess(userNickName, AccessLevel.Mod))
            {
                if (playerName != null && !_solved)
                {
                    ClaimedList.Remove(playerName);
                }
                string newplayerName = internalCommand.Remove(0, 7).Trim();
                playerName = newplayerName;
                if(!_solved)
                {
                    ClaimedList.Add(playerName);
                }
                SetBannerColor(claimedBackgroundColour);
                messageOut = string.Format("/me Module {0} ({3}) assigned to {1} by {2}", targetModule, playerName, userNickName, headerText.text);
            }
        }
        else if (internalCommand.Equals("take", StringComparison.InvariantCultureIgnoreCase))
        {
            if ((playerName != null) && (userNickName != playerName) && (TakeInProgress == null))
            {
                if (!_solved)
                {
                    messageOut = string.Format("/me {0}, {1} wishes to take Module {2} ({3}). It will be freed up in one minute unless you type !{2} mine.", playerName, userNickName, targetModule, headerText.text);
                    TakeInProgress = TakeModule(userNickName, targetModule);
                    StartCoroutine(TakeInProgress);
                }
            }
            else if ((playerName != null) && (userNickName != playerName))
            {
                messageOut = string.Format("/me Sorry @{0}, There is already a takeover attempt for Module {1} ({2}) in progress.", userNickName, targetModule, headerText.text);
            }
        }
        else if (internalCommand.Equals("mine", StringComparison.InvariantCultureIgnoreCase))
        {
            if (playerName == userNickName)
            {
                messageOut = string.Format("/me {0} confirms he/she is still working on {1} ({2})", playerName, targetModule, headerText.text);
                if (TakeInProgress != null)
                {
                    StopCoroutine(TakeInProgress);
                    TakeInProgress = null;
                }
            }
            else if (playerName == null)
            {
                messageOut = ClaimModule(userNickName, targetModule);
            }
        }
        else if (internalCommand.Equals("mark", StringComparison.InvariantCultureIgnoreCase))
        {
            if (UserAccess.HasAccess(userNickName, AccessLevel.Mod))
            {
                SetBannerColor(markedBackgroundColor);
                return null;
            }

        }
        else if (internalCommand.Equals("player", StringComparison.InvariantCultureIgnoreCase))
        {
            if (playerName != null)
            {
                messageOut = string.Format("/me Module {0} ({2}) was claimed by {1}", targetModule, playerName, headerText.text);
            }
        }

        if (!string.IsNullOrEmpty(messageOut))
        {
            ircConnection.SendMessage(string.Format(messageOut, _code, headerText.text));
            return null;
        }

        TwitchMessage message = (TwitchMessage)Instantiate(messagePrefab, messageScrollContents.transform, false);
        message.leaderboard = leaderboard;
        message.userName = userNickName;
        if (string.IsNullOrEmpty(userColor))
        {
            message.SetMessage(string.Format("<b>{0}</b>: {1}", userNickName, internalCommand));
            message.userColor = new Color(0.31f, 0.31f, 0.31f);
        }
        else
        {
            message.SetMessage(string.Format("<b><color={2}>{0}</color></b>: {1}", userNickName, internalCommand, userColor));
            if (!ColorUtility.TryParseHtmlString(userColor, out message.userColor))
            {
                message.userColor = new Color(0.31f, 0.31f, 0.31f);
            }
        }


        if (_solver != null)
        {
            if ((bombCommander.CurrentTimer > 60.0f) && (playerName != null) && (playerName != userNickName) && (!(internalCommand.Equals("take", StringComparison.InvariantCultureIgnoreCase))))
            {
                ircConnection.SendMessage(string.Format("/me Sorry @{2}, Module {0} ({3}) is currently claimed by {1}.  If you think they have abandoned it, you may type !{0} take to free it up.", targetModule, playerName, userNickName, headerText.text));
                return null;
            }
            else
            {
                return RespondToCommandCoroutine(userNickName, internalCommand, message);
            }
        }
        else
        {
            return null;
        }
    }
    #endregion

    #region Private Methods
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

        if (_solver != null)
        {
            IEnumerator commandResponseCoroutine = _solver.RespondToCommand(userNickName, internalCommand, message, ircConnection);
            while (commandResponseCoroutine.MoveNext())
            {
                yield return commandResponseCoroutine.Current;
            }
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

    private void SetBannerColor(Color color)
    {
        idBannerPrefab.GetComponent<Image>().color = color;
        canvasGroupMultiDecker.GetComponent<Image>().color = color;
    }
    #endregion

    #region Private Properties
    private Image Arrow
    {
        get
        {
            switch (direction)
            {
                case Direction.Up:
                    return upArrow;
                case Direction.Down:
                    return downArrow;
                case Direction.Left:
                    return leftArrow;
                case Direction.Right:
                    return rightArrow;

                default:
                    return null;
            }
        }
    }

    private Image HighlightArrow
    {
        get
        {
            switch (direction)
            {
                case Direction.Up:
                    return upArrowHighlight;
                case Direction.Down:
                    return downArrowHighlight;
                case Direction.Left:
                    return leftArrowHighlight;
                case Direction.Right:
                    return rightArrowHighlight;

                default:
                    return null;
            }
        }
    }
    #endregion
}
