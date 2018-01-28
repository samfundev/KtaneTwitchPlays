using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Scripts.Missions;
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
	public TwitchMessage messagePrefab {get => _data.messagePrefab; set => _data.messagePrefab = value;}
	public Image unsupportedPrefab { get => _data.unsupportedPrefab; set => _data.unsupportedPrefab = value; }
	public Image idBannerPrefab { get => _data.idBannerPrefab; set => _data.idBannerPrefab = value; }

	public CanvasGroup canvasGroup { get => _data.canvasGroup; set => _data.canvasGroup = value; }
	public CanvasGroup highlightGroup { get => _data.highlightGroup; set => _data.highlightGroup = value; }
	public CanvasGroup canvasGroupMultiDecker { get => _data.canvasGroupMultiDecker; set => _data.canvasGroupMultiDecker = value; }
	public CanvasGroup canvasGroupUnsupported { get => _data.canvasGroupUnsupported; set => _data.canvasGroupUnsupported = value; }
	public Text headerText { get => _data.headerText; set => _data.headerText = value; }
	public Text idText { get => _data.idText; set => _data.idText = value; }
	public Text idTextMultiDecker { get => _data.idTextMultiDecker; set => _data.idTextMultiDecker = value; }
	public Text idTextUnsupported { get => _data.idTextUnsupported; set => _data.idTextUnsupported = value; }
	public Image claimedUser { get => _data.claimedUser; set => _data.claimedUser = value; }
	public Image claimedUserMultiDecker { get => _data.claimedUserMultiDecker; set => _data.claimedUserMultiDecker = value; }
	public ScrollRect messageScroll { get => _data.messageScroll; set => _data.messageScroll = value; }
	public GameObject messageScrollContents { get => _data.messageScrollContents; set => _data.messageScrollContents = value; }

	public Image upArrow { get => _data.upArrow; set => _data.upArrow = value; }
	public Image downArrow { get => _data.downArrow; set => _data.downArrow = value; }
	public Image leftArrow { get => _data.leftArrow; set => _data.leftArrow = value; }
	public Image rightArrow { get => _data.rightArrow; set => _data.rightArrow = value; }

	public Image upArrowHighlight { get => _data.upArrowHighlight; set => _data.upArrowHighlight = value; }
	public Image downArrowHighlight { get => _data.downArrowHighlight; set => _data.downArrowHighlight = value; }
	public Image leftArrowHighlight { get => _data.leftArrowHighlight; set => _data.leftArrowHighlight = value; }
	public Image rightArrowHighlight { get => _data.rightArrowHighlight; set => _data.rightArrowHighlight = value; }

	public Color claimedBackgroundColour { get => _data.claimedBackgroundColour; set => _data.claimedBackgroundColour = value; }
	public Color solvedBackgroundColor { get => _data.solvedBackgroundColor; set => _data.solvedBackgroundColor = value; }
	public Color markedBackgroundColor { get => _data.markedBackgroundColor; set => _data.markedBackgroundColor = value; }

	public AudioSource takeModuleSound { get => _data.takeModuleSound; set => _data.takeModuleSound = value; }


	[HideInInspector]
	public IRCConnection ircConnection = null;

	[HideInInspector]
	public BombCommander bombCommander = null;

	[HideInInspector]
	public BombComponent bombComponent = null;

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
	public bool claimed => (playerName != null);

	[HideInInspector]
	public int bombID;

	[HideInInspector]
	public string PlayerName => playerName;

	[HideInInspector]
	public bool Solved { get; private set; } = false;

	[HideInInspector]
	public bool Unsupported = false;

	public string Code { get; private set; } = null;

	#endregion

	#region Private Fields

	private ComponentSolver _solver = null;
	private Color unclaimedBackgroundColor = new Color(0, 0, 0);
	private TwitchComponentHandleData _data;

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
		_data = GetComponent<TwitchComponentHandleData>();
		Code = GetNewID().ToString();
	}

	private void Start()
	{
		if (bombComponent != null)
		{
		    headerText.text = bombComponent.GetModuleDisplayName();
		}

		idText.text = string.Format("!{0}", Code);
		idTextMultiDecker.text = Code;

		canvasGroup.alpha = 0.0f;
		highlightGroup.alpha = 0.0f;

		canvasGroupMultiDecker.alpha = bombCommander.multiDecker ? 1.0f : 0.0f;

		unclaimedBackgroundColor = TwitchPlaySettings.data.UnclaimedColor;//idBannerPrefab.GetComponent<Image>().color;

		try
		{
			_solver = ComponentSolverFactory.CreateSolver(bombCommander, bombComponent, componentType, ircConnection, coroutineCanceller);
			if (_solver != null)
			{
				if (_solver.modInfo.ShouldSerializeunclaimedColor()) unclaimedBackgroundColor = _solver.modInfo.unclaimedColor;

				_solver.Code = Code;
				_solver.ComponentHandle = this;
				Vector3 pos = canvasGroupMultiDecker.transform.localPosition;
				canvasGroupMultiDecker.transform.localPosition = new Vector3(_solver.modInfo.statusLightLeft ? -pos.x : pos.x, pos.y, _solver.modInfo.statusLightDown ? -pos.z : pos.z);
				RectTransform rectTransform = claimedUserMultiDecker.rectTransform;
				rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(_solver.modInfo.statusLightLeft ? 1 : 0, _solver.modInfo.statusLightDown ? 0 : 1);
				rectTransform.pivot = new Vector2(_solver.modInfo.statusLightLeft ? 0 : 1, _solver.modInfo.statusLightDown ? 0 : 1);

				canvasGroupUnsupported.gameObject.SetActive(_solver.UnsupportedModule);

				idTextUnsupported.text = bombComponent is ModBombComponent 
					? $"To solve this\nmodule, use\n!{Code} solve" 
					: $"To disarm this\nneedy, use\n!{Code} solve";
				if (_solver.UnsupportedModule)
					_unsupportedComponents.Add(this);

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
			DebugHelper.LogException(e);
			unsupportedPrefab.gameObject.SetActive(true);
			idBannerPrefab.gameObject.SetActive(false);
			canvasGroupMultiDecker.alpha = 0.0f;
			_unsupportedComponents.Add(this);
			_solver = null;

			canvasGroupUnsupported.gameObject.SetActive(true);
			idTextUnsupported.gameObject.SetActive(false);

			if (TwitchPlaySettings.data.EnableTwitchPlaysMode && !TwitchPlaySettings.data.EnableInteractiveMode)
			{
				DebugHelper.Log("An unimplemented module was added to a bomb, solving module.");
			}

		    if (bombComponent != null)
		    {
		        if (bombComponent.GetComponent<KMBombModule>() != null)
		        {
		            KMBombModule module = bombComponent.GetComponent<KMBombModule>();
		            module.OnPass += delegate
		            {
		                bombCommander.bombSolvedModules++;
						BombMessageResponder.moduleCameras?.UpdateSolves();
		                OnPass();
						BombMessageResponder.moduleCameras?.DetachFromModule(bombComponent);
		                return false;
		            };

		            module.OnStrike += delegate
		            {
						BombMessageResponder.moduleCameras?.UpdateStrikes();
		                return false;
		            };
		        }
		        else if (bombComponent.GetComponent<KMNeedyModule>() != null)
		        {
		            bombComponent.GetComponent<KMNeedyModule>().OnStrike += delegate
		            {
						BombMessageResponder.moduleCameras?.UpdateStrikes();
		                return false;
		            };
		        }
		    }

		}
		
		SetBannerColor(unclaimedBackgroundColor);

		if (!_bombCommanders.Contains(bombCommander))
		{
			_bombCommanders.Add(bombCommander);
		}

		Arrow.gameObject.SetActive(true);
		HighlightArrow.gameObject.SetActive(true);
	}

	public static void DeactivateNeedyModule(TwitchComponentHandle handle)
	{
		handle.ircConnection.SendMessage(TwitchPlaySettings.data.UnsupportedNeedyWarning);
		KMNeedyModule needyModule = handle.bombComponent.GetComponent<KMNeedyModule>();
		needyModule.OnNeedyActivation = () => { needyModule.StartCoroutine(KeepUnsupportedNeedySilent(needyModule)); };
		needyModule.OnNeedyDeactivation = () => { needyModule.StopAllCoroutines(); needyModule.HandlePass(); };
		needyModule.OnTimerExpired = () => { needyModule.StopAllCoroutines(); needyModule.HandlePass(); };
		needyModule.WarnAtFiveSeconds = false;
	}

	public static bool SolveUnsupportedModules(bool bombStartup=false)
	{
		List<TwitchComponentHandle> componentsToRemove = bombStartup
			? _unsupportedComponents.Where(x => x._solver == null).ToList()
			: _unsupportedComponents.Where(x => x._solver == null || !x.Solved).ToList();

		if (componentsToRemove.Count == 0) return false;

		foreach (TwitchComponentHandle handle in componentsToRemove)
		{
			if (handle.bombComponent.GetComponent<KMNeedyModule>() != null)
			{
				DeactivateNeedyModule(handle);
			}
			else if (handle.bombComponent.GetComponent<KMBombModule>() != null)
			{
				handle.bombComponent.GetComponent<KMBombModule>().HandlePass();
			}
		}
		RemoveSolveBasedModules();

		_unsupportedComponents.Clear();
		return true;
	}

	public static void RemoveSolveBasedModules()
	{
		foreach (BombCommander commander in _bombCommanders)
		{
			commander.RemoveSolveBasedModules();
		}
	}

	public void SolveSilently()
	{
		_solver.SolveSilently();
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
		Solved = true;
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

    private void OnDestroy()
    {
        StopAllCoroutines();
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
			ircConnection.SendMessage(TwitchPlaySettings.data.ModuleAbandoned, targetModule, playerName, headerText.text);
			ClaimedList.Remove(playerName);
			playerName = null;
			TakeInProgress = null;
		}
	}

	public IEnumerator ReleaseModule(string player, string userNickName)
	{
		if (!UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
		{
			yield return new WaitForSeconds(TwitchPlaySettings.data.ClaimCooldownTime);
		}
		ClaimedList.Remove(player);
	}

	public string ClaimModule(string userNickName, string targetModule)
	{
		if (playerName != null) return string.Format(TwitchPlaySettings.data.ModulePlayer, targetModule, playerName, headerText.text);
		if (ClaimedList.Count(nick => nick.Equals(userNickName)) >= TwitchPlaySettings.data.ModuleClaimLimit && !Solved)
		{
			return string.Format(TwitchPlaySettings.data.TooManyClaimed, userNickName, TwitchPlaySettings.data.ModuleClaimLimit);
		}
		else
		{
			ClaimedList.Add(userNickName);
			SetBannerColor(claimedBackgroundColour);
			playerName = userNickName;
			return string.Format(TwitchPlaySettings.data.ModuleClaimed, targetModule, playerName, headerText.text);
		}
	}

	public void CommandError(string userNickName, string message)
	{
		ircConnection.SendMessage(TwitchPlaySettings.data.CommandError, userNickName, Code, headerText.text, message);
	}

	public void CommandInvalid(string userNickName)
	{
		ircConnection.SendMessage(TwitchPlaySettings.data.InvalidCommand, userNickName, Code, headerText.text);
	}

	public IEnumerator TakeInProgress = null;
	public static List<string> ClaimedList = new List<string>();

	#region Message Interface
	public IEnumerator OnMessageReceived(string userNickName, string userColor, string text)
	{
		Match match = Regex.Match(text, string.Format("^({0}) (.+)", Code), RegexOptions.IgnoreCase);
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
			if ((internalCommand.Length > 7) && (internalCommand.Substring(7) == "pdf"))
			{
				manualType = "pdf";
			}

			manualText = string.IsNullOrEmpty(_solver.modInfo.manualCode) ? headerText.text : _solver.modInfo.manualCode;

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
		else if (!Solved)
		{
			if (IsAuthorizedDefuser(userNickName, false))
			{
				if (Regex.IsMatch(internalCommand, "^(bomb|queue) (turn( a?round)?|flip|spin)$", RegexOptions.IgnoreCase))
				{
					if (!_solver._turnQueued)
					{
						_solver._turnQueued = true;
						StartCoroutine(_solver.TurnBombOnSolve());
					}
					messageOut = string.Format(TwitchPlaySettings.data.TurnBombOnSolve, targetModule, headerText.text);
				}
				else if (Regex.IsMatch(internalCommand, "^cancel (bomb|queue) (turn( a?round)?|flip|spin)$", RegexOptions.IgnoreCase))
				{
					_solver._turnQueued = false;
					messageOut = string.Format(TwitchPlaySettings.data.CancelBombTurn, targetModule, headerText.text);
				}
				else if (internalCommand.Equals("claim", StringComparison.InvariantCultureIgnoreCase))
				{
					messageOut = ClaimModule(userNickName, targetModule);
				}
				else if (internalCommand.ToLowerInvariant().EqualsAny("release", "unclaim"))
				{
					if (playerName == userNickName || UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						if (TakeInProgress != null)
						{
							StopCoroutine(TakeInProgress);
							TakeInProgress = null;
						}
						StartCoroutine(ReleaseModule(playerName, userNickName));
						SetBannerColor(unclaimedBackgroundColor);
						messageOut = string.Format(TwitchPlaySettings.data.ModuleUnclaimed, targetModule, playerName, headerText.text);
						playerName = null;
					}
				}
				else if (internalCommand.Equals("solved", StringComparison.InvariantCultureIgnoreCase))
				{
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						SetBannerColor(solvedBackgroundColor);
						playerName = null;
						messageOut = string.Format(TwitchPlaySettings.data.ModuleReady, targetModule, userNickName, headerText.text);
					}
				}
				else if (internalCommand.StartsWith("assign", StringComparison.InvariantCultureIgnoreCase))
				{
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						if (playerName != null)
						{
							ClaimedList.Remove(playerName);
						}
						string newplayerName = internalCommand.Remove(0, 7).Trim();
						playerName = newplayerName;
						ClaimedList.Add(playerName);
						SetBannerColor(claimedBackgroundColour);
						messageOut = string.Format(TwitchPlaySettings.data.AssignModule, targetModule, playerName, userNickName, headerText.text);
					}
				}
				else if (internalCommand.Equals("take", StringComparison.InvariantCultureIgnoreCase))
				{
					if (playerName != null && userNickName != playerName)
					{
						if (TakeInProgress == null)
						{
							messageOut = string.Format(TwitchPlaySettings.data.TakeModule, playerName, userNickName, targetModule, headerText.text);
							TakeInProgress = TakeModule(userNickName, targetModule);
							StartCoroutine(TakeInProgress);
						}
						else
						{
							messageOut = string.Format(TwitchPlaySettings.data.TakeInProgress, userNickName, targetModule, headerText.text);
						}
					}
                    else if (playerName != null)
					{
					    messageOut = string.Format(TwitchPlaySettings.data.ModuleAlreadyOwned, userNickName, targetModule, headerText.text);
					}
					else
					{
					    messageOut = ClaimModule(userNickName, targetModule);
					}
				}
				else if (internalCommand.Equals("mine", StringComparison.InvariantCultureIgnoreCase))
				{
					if (playerName == userNickName && TakeInProgress != null)
					{
						messageOut = string.Format(TwitchPlaySettings.data.ModuleIsMine, playerName, targetModule, headerText.text);
						StopCoroutine(TakeInProgress);
						TakeInProgress = null;
					}
					else if (playerName == null)
					{
						messageOut = ClaimModule(userNickName, targetModule);
					}
				}
				else if (internalCommand.Equals("mark", StringComparison.InvariantCultureIgnoreCase))
				{
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						SetBannerColor(markedBackgroundColor);
						return null;
					}
				}
			}

			if (internalCommand.Equals("player", StringComparison.InvariantCultureIgnoreCase))
			{
				messageOut = playerName != null 
					? string.Format(TwitchPlaySettings.data.ModulePlayer, targetModule, playerName, headerText.text) 
					: string.Format(TwitchPlaySettings.data.ModuleNotClaimed, userNickName, targetModule, headerText.text);
			}
		}

		if (!string.IsNullOrEmpty(messageOut))
		{
			ircConnection.SendMessage(messageOut, Code, headerText.text);
			return null;
		}

		TwitchMessage message = Instantiate<TwitchMessage>(messagePrefab, messageScrollContents.transform, false);
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
		    if (!IsAuthorizedDefuser(userNickName)) return null;
            if ((bombCommander.CurrentTimer > 60.0f) && (playerName != null) && (playerName != userNickName) && (!(internalCommand.Equals("take", StringComparison.InvariantCultureIgnoreCase) || (internalCommand.Equals("view pin", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName,AccessLevel.Mod,true)) || (internalCommand.Equals("solve", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.Admin, true)))))
			{
				ircConnection.SendMessage(TwitchPlaySettings.data.AlreadyClaimed, targetModule, playerName, userNickName, headerText.text);
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
    private bool IsAuthorizedDefuser(string userNickName, bool sendMessage = true)
    {
        bool result = (TwitchPlaySettings.data.EnableTwitchPlaysMode || UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true));
        if (!result && sendMessage)
            ircConnection.SendMessage(TwitchPlaySettings.data.TwitchPlaysDisabled, userNickName);

        return result;
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
		if (bombCommander.multiDecker)
		{
			canvasGroupMultiDecker.GetComponent<Image>().color = color;
			claimedUserMultiDecker.color = color;
		}
		else
		{

			idBannerPrefab.GetComponent<Image>().color = color;
			claimedUser.color = color;
		}
	}

	private static IEnumerator KeepUnsupportedNeedySilent(KMNeedyModule needyModule)
	{
		while (true)
		{
			yield return null;
			needyModule.SetNeedyTimeRemaining(99f);
		}
	}
	#endregion

	#region Private Properties
	private string _playerName = null;
	private string playerName
	{
		set
		{
			_playerName = value;

			Image claimedDisplay = bombCommander.multiDecker ? claimedUserMultiDecker : claimedUser;
			if (value != null) claimedDisplay.transform.Find("Username").GetComponent<Text>().text = value;
			claimedDisplay.gameObject.SetActive(value != null);
		}
		get => _playerName;
	}

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
