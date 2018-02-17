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
	#region Public Fields
	public CanvasGroup canvasGroupMultiDecker { get => _data.canvasGroupMultiDecker; set => _data.canvasGroupMultiDecker = value; }
	public CanvasGroup canvasGroupUnsupported { get => _data.canvasGroupUnsupported; set => _data.canvasGroupUnsupported = value; }
	public Text idTextMultiDecker { get => _data.idTextMultiDecker; set => _data.idTextMultiDecker = value; }
	public Text idTextUnsupported { get => _data.idTextUnsupported; set => _data.idTextUnsupported = value; }
	public Image claimedUserMultiDecker { get => _data.claimedUserMultiDecker; set => _data.claimedUserMultiDecker = value; }
	public Color claimedBackgroundColour { get => _data.claimedBackgroundColour; set => _data.claimedBackgroundColour = value; }
	public Color solvedBackgroundColor { get => _data.solvedBackgroundColor; set => _data.solvedBackgroundColor = value; }
	public Color markedBackgroundColor { get => _data.markedBackgroundColor; set => _data.markedBackgroundColor = value; }

	public AudioSource takeModuleSound { get => _data.takeModuleSound; set => _data.takeModuleSound = value; }

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
	public CoroutineQueue coroutineQueue = null;

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

	public ComponentSolver Solver { get; private set; } = null;

	public bool IsMod => bombComponent is ModBombComponent || bombComponent is ModNeedyComponent;

	private string _headerText;
	public string HeaderText
	{
		get => _headerText ?? bombComponent?.GetModuleDisplayName() ?? string.Empty;
		set => _headerText = value;
	}

	#endregion

	#region Private Fields
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
		idTextMultiDecker.text = Code;

		canvasGroupMultiDecker.alpha = 1.0f;

		unclaimedBackgroundColor = TwitchPlaySettings.data.UnclaimedColor;//idBannerPrefab.GetComponent<Image>().color;

		try
		{
			Solver = ComponentSolverFactory.CreateSolver(bombCommander, bombComponent, componentType);
			if (Solver != null)
			{
				if (Solver.modInfo.ShouldSerializeunclaimedColor()) unclaimedBackgroundColor = Solver.modInfo.unclaimedColor;

				Solver.Code = Code;
				Solver.ComponentHandle = this;
				Vector3 pos = canvasGroupMultiDecker.transform.localPosition;
				canvasGroupMultiDecker.transform.localPosition = new Vector3(Solver.modInfo.statusLightLeft ? -pos.x : pos.x, pos.y, Solver.modInfo.statusLightDown ? -pos.z : pos.z);
				RectTransform rectTransform = claimedUserMultiDecker.rectTransform;
				rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(Solver.modInfo.statusLightLeft ? 1 : 0, Solver.modInfo.statusLightDown ? 0 : 1);
				rectTransform.pivot = new Vector2(Solver.modInfo.statusLightLeft ? 0 : 1, Solver.modInfo.statusLightDown ? 0 : 1);

				canvasGroupUnsupported.gameObject.SetActive(Solver.UnsupportedModule);

				idTextUnsupported.text = bombComponent is ModBombComponent 
					? $"To solve this\nmodule, use\n!{Code} solve" 
					: $"To disarm this\nneedy, use\n!{Code} solve";
				if (Solver.UnsupportedModule)
					_unsupportedComponents.Add(this);
			}
		}
		catch (Exception e)
		{
			DebugHelper.LogException(e);
			canvasGroupMultiDecker.alpha = 0.0f;
			_unsupportedComponents.Add(this);
			Solver = null;

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
		                OnPass(null);
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
	}

	public static void DeactivateNeedyModule(TwitchComponentHandle handle)
	{
		IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.UnsupportedNeedyWarning);
		KMNeedyModule needyModule = handle.bombComponent.GetComponent<KMNeedyModule>();
		needyModule.OnNeedyActivation = () => { needyModule.StartCoroutine(KeepUnsupportedNeedySilent(needyModule)); };
		needyModule.OnNeedyDeactivation = () => { needyModule.StopAllCoroutines(); needyModule.HandlePass(); };
		needyModule.OnTimerExpired = () => { needyModule.StopAllCoroutines(); needyModule.HandlePass(); };
		needyModule.WarnAtFiveSeconds = false;
	}

	public static bool SolveUnsupportedModules(bool bombStartup=false)
	{
		List<TwitchComponentHandle> componentsToRemove = bombStartup
			? _unsupportedComponents.Where(x => x.Solver == null).ToList()
			: _unsupportedComponents.Where(x => x.Solver == null || !x.Solved).ToList();

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
		Solver.SolveSilently();
	}

	public static void ClearUnsupportedModules()
	{
		_bombCommanders.Clear();
		_unsupportedComponents.Clear();
	}

	public void OnPass(string userNickname)
	{
		canvasGroupMultiDecker.alpha = 0.0f;
		Solved = true;
		if (playerName != null)
		{
			ClaimedList.Remove(playerName);
		}
		else
		{
			playerName = userNickname;
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
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.ModuleAbandoned, targetModule, playerName, HeaderText);
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
		if (playerName != null) return string.Format(TwitchPlaySettings.data.ModulePlayer, targetModule, playerName, HeaderText);
		if (ClaimedList.Count(nick => nick.Equals(userNickName)) >= TwitchPlaySettings.data.ModuleClaimLimit && !Solved)
		{
			return string.Format(TwitchPlaySettings.data.TooManyClaimed, userNickName, TwitchPlaySettings.data.ModuleClaimLimit);
		}
		else
		{
			ClaimedList.Add(userNickName);
			SetBannerColor(claimedBackgroundColour);
			playerName = userNickName;
			return string.Format(TwitchPlaySettings.data.ModuleClaimed, targetModule, playerName, HeaderText);
		}
	}

	public void CommandError(string userNickName, string message)
	{
		IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.CommandError, userNickName, Code, HeaderText, message);
	}

	public void CommandInvalid(string userNickName)
	{
		IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.InvalidCommand, userNickName, Code, HeaderText);
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

			manualText = string.IsNullOrEmpty(Solver.modInfo.manualCode) ? HeaderText : Solver.modInfo.manualCode;

			if (manualText.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
				manualText.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
			{
				messageOut = string.Format("{0} : {1} : {2}", HeaderText, Solver.modInfo.helpText, manualText);
			}
			else
			{
				//messageOut = string.Format("{0}: {1}", headerText.text, TwitchPlaysService.urlHelper.ManualFor(manualText, manualType));
				messageOut = string.Format("{0} : {1} : {2}", HeaderText, Solver.modInfo.helpText, UrlHelper.Instance.ManualFor(manualText, manualType));
			}
		}
		else if (!Solved)
		{
			if (IsAuthorizedDefuser(userNickName, false))
			{
				if (Regex.IsMatch(internalCommand, "^(bomb|queue) (turn( a?round)?|flip|spin)$", RegexOptions.IgnoreCase))
				{
					if (!Solver._turnQueued)
					{
						Solver._turnQueued = true;
						StartCoroutine(Solver.TurnBombOnSolve());
					}
					messageOut = string.Format(TwitchPlaySettings.data.TurnBombOnSolve, targetModule, HeaderText);
				}
				else if (Regex.IsMatch(internalCommand, "^cancel (bomb|queue) (turn( a?round)?|flip|spin)$", RegexOptions.IgnoreCase))
				{
					Solver._turnQueued = false;
					messageOut = string.Format(TwitchPlaySettings.data.CancelBombTurn, targetModule, HeaderText);
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
						messageOut = string.Format(TwitchPlaySettings.data.ModuleUnclaimed, targetModule, playerName, HeaderText);
						playerName = null;
					}
					else
					{
						messageOut = string.Format(TwitchPlaySettings.data.AlreadyClaimed, targetModule, playerName, userNickName, HeaderText);
					}
				}
				else if (internalCommand.Equals("solved", StringComparison.InvariantCultureIgnoreCase))
				{
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						SetBannerColor(solvedBackgroundColor);
						playerName = null;
						messageOut = string.Format(TwitchPlaySettings.data.ModuleReady, targetModule, userNickName, HeaderText);
					}
					else
					{
						return null;
					}
				}
				else if (internalCommand.StartsWith("assign ", StringComparison.InvariantCultureIgnoreCase))
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
						messageOut = string.Format(TwitchPlaySettings.data.AssignModule, targetModule, playerName, userNickName, HeaderText);
					}
					else
					{
						return null;
					}
				}
				else if (internalCommand.Equals("take", StringComparison.InvariantCultureIgnoreCase))
				{
					if (playerName != null && userNickName != playerName)
					{
						if (TakeInProgress == null)
						{
							messageOut = string.Format(TwitchPlaySettings.data.TakeModule, playerName, userNickName, targetModule, HeaderText);
							TakeInProgress = TakeModule(userNickName, targetModule);
							StartCoroutine(TakeInProgress);
						}
						else
						{
							messageOut = string.Format(TwitchPlaySettings.data.TakeInProgress, userNickName, targetModule, HeaderText);
						}
					}
                    else if (playerName != null)
					{
					    messageOut = string.Format(TwitchPlaySettings.data.ModuleAlreadyOwned, userNickName, targetModule, HeaderText);
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
						messageOut = string.Format(TwitchPlaySettings.data.ModuleIsMine, playerName, targetModule, HeaderText);
						StopCoroutine(TakeInProgress);
						TakeInProgress = null;
					}
					else if (playerName == null)
					{
						messageOut = ClaimModule(userNickName, targetModule);
					}
					else
					{
						messageOut = string.Format(TwitchPlaySettings.data.AlreadyClaimed, targetModule, playerName, userNickName, HeaderText);
					}
				}
				else if (internalCommand.Equals("mark", StringComparison.InvariantCultureIgnoreCase))
				{
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						SetBannerColor(markedBackgroundColor);
					}

					return null;
				}
			}
		}

		if (internalCommand.Equals("player", StringComparison.InvariantCultureIgnoreCase))
		{
			messageOut = playerName != null
				? string.Format(TwitchPlaySettings.data.ModulePlayer, targetModule, playerName, HeaderText)
				: string.Format(TwitchPlaySettings.data.ModuleNotClaimed, userNickName, targetModule, HeaderText);
		}

		if (!string.IsNullOrEmpty(messageOut))
		{
			IRCConnection.Instance.SendMessage(messageOut, Code, HeaderText);
			return null;
		}

		if (Solver != null)
		{ 
			if (!IsAuthorizedDefuser(userNickName, false)) return null;
			bool moduleAlreadyClaimed = bombCommander.CurrentTimer > TwitchPlaySettings.data.MinTimeLeftForClaims;
			moduleAlreadyClaimed &= BombMessageResponder.Instance.ComponentHandles.Count(x => !x.Solved && GameRoom.Instance.IsCurrentBomb(x.bombID)) >= TwitchPlaySettings.data.MinUnsolvedModulesLeftForClaims;
			moduleAlreadyClaimed &= playerName != null;
			moduleAlreadyClaimed &= playerName != userNickName;
			moduleAlreadyClaimed &= !internalCommand.Equals("take", StringComparison.InvariantCultureIgnoreCase);
			moduleAlreadyClaimed &= !(internalCommand.Equals("view pin", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.Mod, true));
			moduleAlreadyClaimed &= !(internalCommand.Equals("solve", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.Admin, true));
			if (moduleAlreadyClaimed)
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.AlreadyClaimed, targetModule, playerName, userNickName, HeaderText);
				return null;
			}
			else
			{
				return RespondToCommandCoroutine(userNickName, internalCommand);
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
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.TwitchPlaysDisabled, userNickName);

        return result;
    }

    private IEnumerator RespondToCommandCoroutine(string userNickName, string internalCommand, float fadeDuration = 0.1f)
    {
	    yield return new WaitForSeconds(0.1f);
		if (Solver != null)
		{
			IEnumerator commandResponseCoroutine = Solver.RespondToCommand(userNickName, internalCommand);
			while (commandResponseCoroutine.MoveNext())
			{
				yield return commandResponseCoroutine.Current;
			}
		}
	    yield return new WaitForSeconds(0.1f);
	}

	private void SetBannerColor(Color color)
	{
		canvasGroupMultiDecker.GetComponent<Image>().color = color;
		claimedUserMultiDecker.color = color;
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

			Image claimedDisplay = claimedUserMultiDecker;
			if (value != null) claimedDisplay.transform.Find("Username").GetComponent<Text>().text = value;
			claimedDisplay.gameObject.SetActive(value != null);
		}
		get => _playerName;
	}
	#endregion
}
