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
	public CanvasGroup CanvasGroupMultiDecker { get => _data.canvasGroupMultiDecker; set => _data.canvasGroupMultiDecker = value; }
	public CanvasGroup CanvasGroupUnsupported { get => _data.canvasGroupUnsupported; set => _data.canvasGroupUnsupported = value; }
	public Text IDTextMultiDecker { get => _data.idTextMultiDecker; set => _data.idTextMultiDecker = value; }
	public Text IDTextUnsupported { get => _data.idTextUnsupported; set => _data.idTextUnsupported = value; }
	public Image ClaimedUserMultiDecker { get => _data.claimedUserMultiDecker; set => _data.claimedUserMultiDecker = value; }
	public Color ClaimedBackgroundColour { get => _data.claimedBackgroundColour; set => _data.claimedBackgroundColour = value; }
	public Color SolvedBackgroundColor { get => _data.solvedBackgroundColor; set => _data.solvedBackgroundColor = value; }
	public Color MarkedBackgroundColor { get => _data.markedBackgroundColor; set => _data.markedBackgroundColor = value; }

	public AudioSource TakeModuleSound { get => _data.takeModuleSound; set => _data.takeModuleSound = value; }

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
	public bool Claimed => (PlayerName != null);

	[HideInInspector]
	public int bombID;

	[HideInInspector]
	public bool Solved { get; private set; } = false;

	[HideInInspector]
	public bool Unsupported = false;

	[HideInInspector]
	public List<Tuple<string, double>> ClaimQueue = new List<Tuple<string, double>>();

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
	private bool claimCooldown = true;
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
		IDTextMultiDecker.text = Code;

		CanvasGroupMultiDecker.alpha = 1.0f;

		unclaimedBackgroundColor = TwitchPlaySettings.data.UnclaimedColor;//idBannerPrefab.GetComponent<Image>().color;

		try
		{
			Solver = ComponentSolverFactory.CreateSolver(bombCommander, bombComponent, componentType);
			if (Solver != null)
			{
				if (Solver.modInfo.ShouldSerializeunclaimedColor()) unclaimedBackgroundColor = Solver.modInfo.unclaimedColor;

				Solver.Code = Code;
				Solver.ComponentHandle = this;
				Vector3 pos = CanvasGroupMultiDecker.transform.localPosition;
				CanvasGroupMultiDecker.transform.localPosition = new Vector3(Solver.modInfo.statusLightLeft ? -pos.x : pos.x, pos.y, Solver.modInfo.statusLightDown ? -pos.z : pos.z);
				RectTransform rectTransform = ClaimedUserMultiDecker.rectTransform;
				rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(Solver.modInfo.statusLightLeft ? 1 : 0, Solver.modInfo.statusLightDown ? 0 : 1);
				rectTransform.pivot = new Vector2(Solver.modInfo.statusLightLeft ? 0 : 1, Solver.modInfo.statusLightDown ? 0 : 1);

				CanvasGroupUnsupported.gameObject.SetActive(Solver.UnsupportedModule);

				IDTextUnsupported.text = bombComponent is ModBombComponent 
					? $"To solve this\nmodule, use\n!{Code} solve" 
					: $"To disarm this\nneedy, use\n!{Code} solve";
				if (Solver.UnsupportedModule)
					_unsupportedComponents.Add(this);

				StartCoroutine(AutoAssignModule());
			}
		}
		catch (Exception e)
		{
			DebugHelper.LogException(e);
			CanvasGroupMultiDecker.alpha = 0.0f;
			_unsupportedComponents.Add(this);
			Solver = null;

			CanvasGroupUnsupported.gameObject.SetActive(true);
			IDTextUnsupported.gameObject.SetActive(false);

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

	public static bool UnsupportedModulesPresent()
	{
		return _unsupportedComponents.Any(x => x.Solver == null || !x.Solved);
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
			handle.SolveSilently();
		}

		if(componentsToRemove.Count > 1)	//Forget me not become unsolvable if MORE than one module is solved at once.
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
		CanvasGroupMultiDecker.alpha = 0.0f;
		Solved = true;
		if (PlayerName != null)
		{
			ClaimedList.Remove(PlayerName);
		}
		else
		{
			PlayerName = userNickname;
			CanClaimNow(userNickname, true, true);
		}
		if (TakeInProgress != null)
		{
			StopCoroutine(TakeInProgress);
			TakeInProgress = null;
		}
		if (PlayerName == null) return;
		try
		{
			if (!bombCommander.SolvedModules.ContainsKey(Solver.modInfo.moduleDisplayName))
				bombCommander.SolvedModules[Solver.modInfo.moduleDisplayName] = new List<TwitchComponentHandle>();
			bombCommander.SolvedModules[Solver.modInfo.moduleDisplayName].Add(this);
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could not add entry to solved modules list due to an exception:");
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
		if (TakeModuleSound != null)
		{
			TakeModuleSound.time = 0.0f;
			TakeModuleSound.Play();
		}
		yield return new WaitForSecondsRealtime(60.0f);
		SetBannerColor(unclaimedBackgroundColor);
		if (PlayerName != null)
		{
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.ModuleAbandoned, targetModule, PlayerName, HeaderText);
			ClaimedList.Remove(PlayerName);
			PlayerName = null;
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

	public IEnumerator EndClaimCooldown()
	{
		if (TwitchPlaySettings.data.InstantModuleClaimCooldown > 0)
		{
			yield return new WaitForSeconds(TwitchPlaySettings.data.InstantModuleClaimCooldown);
		}
		claimCooldown = false;
	}

	public Tuple<bool, double> CanClaimNow(string userNickName, bool updatePreviousClaim, bool force=false)
	{
		if (string.IsNullOrEmpty(userNickName)) return new Tuple<bool, double>(false, DateTime.Now.TotalSeconds());

		if (BombMessageResponder.Instance.LastClaimedModule == null)
		{
			BombMessageResponder.Instance.LastClaimedModule = new Dictionary<string, Dictionary<string, double>>();
		}

		if (!BombMessageResponder.Instance.LastClaimedModule.TryGetValue(Solver.modInfo.moduleID, out Dictionary<string, double> value) || value == null)
		{
			value = new Dictionary<string, double>();
			BombMessageResponder.Instance.LastClaimedModule[Solver.modInfo.moduleID] = value;
		}
		if (claimCooldown && !force && value.TryGetValue(userNickName, out double seconds) &&
		    (DateTime.Now.TotalSeconds() - seconds) < TwitchPlaySettings.data.InstantModuleClaimCooldownExpiry)
		{
			return new Tuple<bool, double>(false, seconds + TwitchPlaySettings.data.InstantModuleClaimCooldownExpiry);
		}
		if (updatePreviousClaim || force)
		{
			value[userNickName] = DateTime.Now.TotalSeconds();
		}
		return new Tuple<bool, double>(true, DateTime.Now.TotalSeconds());
	}

	public void AddToClaimQueue(string userNickname)
	{
		double seconds = CanClaimNow(userNickname, false).Second;
		if (ClaimQueue.Any(x => x.First.Equals(userNickname, StringComparison.InvariantCultureIgnoreCase))) return;
		for (int i = 0; i < ClaimQueue.Count; i++)
		{
			if (ClaimQueue[i].Second < seconds) continue;
			ClaimQueue.Insert(i, new Tuple<string, double>(userNickname, seconds));
			return;
		}
		ClaimQueue.Add(new Tuple<string, double>(userNickname, seconds));
	}

	public void RemoveFromClaimQueue(string userNickname)
	{
		ClaimQueue.RemoveAll(x => x.First.Equals(userNickname, StringComparison.InvariantCultureIgnoreCase));
	}

	public IEnumerator AutoAssignModule()
	{
		StartCoroutine(EndClaimCooldown());
		while (!Solved)
		{
			yield return new WaitForSeconds(0.1f);
			if (PlayerName != null || ClaimQueue.Count == 0) continue;

			for (int i = 0; i < ClaimQueue.Count; i++)
			{
				Tuple<bool, string> claim = ClaimModule(ClaimQueue[i].First, Code);
				if (!claim.First) continue;
				IRCConnection.Instance.SendMessage(claim.Second);
				ClaimQueue.RemoveAt(i);
				break;
			}
		}
	}

	public Tuple<bool, string> ClaimModule(string userNickName, string targetModule)
	{
		if (PlayerName != null)
		{
			if(!PlayerName.Equals(userNickName))
				AddToClaimQueue(userNickName);
			return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.ModulePlayer, targetModule, PlayerName, HeaderText));
		}
		if (ClaimedList.Count(nick => nick.Equals(userNickName, StringComparison.InvariantCultureIgnoreCase)) >= TwitchPlaySettings.data.ModuleClaimLimit && !Solved && (!UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true) || !TwitchPlaySettings.data.SuperStreamerIgnoreClaimLimit))
		{
			AddToClaimQueue(userNickName);
			return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.TooManyClaimed, userNickName, TwitchPlaySettings.data.ModuleClaimLimit));
		}
		else
		{
			Tuple<bool, double> claim = CanClaimNow(userNickName, true);
			if (!claim.First)
			{
				AddToClaimQueue(userNickName);
				return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.ClaimCooldown, Code, TwitchPlaySettings.data.InstantModuleClaimCooldown, userNickName, HeaderText));
			}

			ClaimedList.Add(userNickName);
			SetBannerColor(ClaimedBackgroundColour);
			PlayerName = userNickName;
			return new Tuple<bool, string>(true, string.Format(TwitchPlaySettings.data.ModuleClaimed, targetModule, PlayerName, HeaderText));
		}
	}

	public Tuple<bool, string> UnclaimModule(string userNickName, string targetModule)
	{
		RemoveFromClaimQueue(userNickName);
		if (PlayerName.Equals(userNickName, StringComparison.InvariantCultureIgnoreCase) || UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
		{
			if (TakeInProgress != null)
			{
				StopCoroutine(TakeInProgress);
				TakeInProgress = null;
			}
			StartCoroutine(ReleaseModule(PlayerName, userNickName));
			SetBannerColor(unclaimedBackgroundColor);
			string messageOut = string.Format(TwitchPlaySettings.data.ModuleUnclaimed, targetModule, PlayerName, HeaderText);
			PlayerName = null;

			return new Tuple<bool, string>(true, messageOut);
		}
		else
		{
			return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.AlreadyClaimed, targetModule, PlayerName, userNickName, HeaderText));
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
				messageOut = string.Format("{0} : {1} : {2}", HeaderText, Solver.modInfo.helpText, UrlHelper.Instance.ManualFor(manualText, manualType, VanillaRuleModifier.GetModuleRuleSeed(Solver.modInfo.moduleID) != 1));
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
					messageOut = ClaimModule(userNickName, targetModule).Second;
				}
				else if (internalCommand.ToLowerInvariant().EqualsAny("release", "unclaim"))
				{
					messageOut = UnclaimModule(userNickName, targetModule).Second;
				}
				else if (internalCommand.ToLowerInvariant().EqualsAny("claim view", "view claim", "claimview", "viewclaim", "cw", "wc", 
					"claim view pin", "view pin claim", "claimviewpin", "viewpinclaim", "cwp", "wpc"))
				{
					Tuple<bool, string> response = ClaimModule(userNickName, Code);
					if (response.First)
					{
						IRCConnection.Instance.SendMessage(response.Second);
						internalCommand = text.Contains("p") ? "view pin" : "view";
					}
					else
					{
						messageOut = response.Second;
					}
				}
				else if (internalCommand.ToLowerInvariant().EqualsAny("unclaim unview", "unview unclaim", "unclaimview", "unviewclaim", "uncw", "unwc"))
				{
					Tuple<bool, string> response = UnclaimModule(userNickName, Code);
					if (response.First)
					{
						IRCConnection.Instance.SendMessage(response.Second);
						internalCommand = "unview";
					}
					else
					{
						messageOut = response.Second;
					}
				}
				else if (internalCommand.Equals("solved", StringComparison.InvariantCultureIgnoreCase))
				{
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						SetBannerColor(SolvedBackgroundColor);
						PlayerName = null;
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
						if (PlayerName != null)
						{
							ClaimedList.Remove(PlayerName);
						}
						string newplayerName = internalCommand.Remove(0, 7).Trim();
						PlayerName = newplayerName;
						ClaimedList.Add(PlayerName);
						RemoveFromClaimQueue(userNickName);
						CanClaimNow(userNickName, true, true);
						SetBannerColor(ClaimedBackgroundColour);
						messageOut = string.Format(TwitchPlaySettings.data.AssignModule, targetModule, PlayerName, userNickName, HeaderText);
					}
					else
					{
						return null;
					}
				}
				else if (internalCommand.Equals("take", StringComparison.InvariantCultureIgnoreCase))
				{
					if (PlayerName != null && userNickName != PlayerName)
					{
						AddToClaimQueue(userNickName);
						if (TakeInProgress == null)
						{
							messageOut = string.Format(TwitchPlaySettings.data.TakeModule, PlayerName, userNickName, targetModule, HeaderText);
							TakeInProgress = TakeModule(userNickName, targetModule);
							StartCoroutine(TakeInProgress);
						}
						else
						{
							messageOut = string.Format(TwitchPlaySettings.data.TakeInProgress, userNickName, targetModule, HeaderText);
						}
					}
                    else if (PlayerName != null)
					{
						if (!PlayerName.Equals(userNickName))
							AddToClaimQueue(userNickName);
						messageOut = string.Format(TwitchPlaySettings.data.ModuleAlreadyOwned, userNickName, targetModule, HeaderText);
					}
					else
					{
					    messageOut = ClaimModule(userNickName, targetModule).Second;
					}
				}
				else if (internalCommand.Equals("mine", StringComparison.InvariantCultureIgnoreCase))
				{
					if (PlayerName == userNickName && TakeInProgress != null)
					{
						messageOut = string.Format(TwitchPlaySettings.data.ModuleIsMine, PlayerName, targetModule, HeaderText);
						StopCoroutine(TakeInProgress);
						TakeInProgress = null;
					}
					else if (PlayerName == null)
					{
						messageOut = ClaimModule(userNickName, targetModule).Second;
					}
					else
					{
						messageOut = string.Format(TwitchPlaySettings.data.AlreadyClaimed, targetModule, PlayerName, userNickName, HeaderText);
					}
				}
				else if (internalCommand.Equals("mark", StringComparison.InvariantCultureIgnoreCase))
				{
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						SetBannerColor(MarkedBackgroundColor);
					}

					return null;
				}
			}
		}

		if (internalCommand.Equals("player", StringComparison.InvariantCultureIgnoreCase))
		{
			messageOut = PlayerName != null
				? string.Format(TwitchPlaySettings.data.ModulePlayer, targetModule, PlayerName, HeaderText)
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
			moduleAlreadyClaimed &= PlayerName != null;
			moduleAlreadyClaimed &= PlayerName != userNickName;
			moduleAlreadyClaimed &= !internalCommand.Equals("take", StringComparison.InvariantCultureIgnoreCase);
			moduleAlreadyClaimed &= !(internalCommand.Equals("view pin", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.Mod, true));
			moduleAlreadyClaimed &= !(internalCommand.Equals("solve", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.Admin, true));
			if (moduleAlreadyClaimed)
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.AlreadyClaimed, targetModule, PlayerName, userNickName, HeaderText);
				return null;
			}
			else
			{
				// Twitch allows newlines in messages, even though they show up in the chat window as spaces, so pretend they’re spaces
				return RespondToCommandCoroutine(userNickName, internalCommand.Replace("\n", " ").Replace("\r", ""));
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
	    return MessageResponder.IsAuthorizedDefuser(userNickName, !sendMessage);
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
		CanvasGroupMultiDecker.GetComponent<Image>().color = color;
		ClaimedUserMultiDecker.color = color;
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
	public string PlayerName
	{
		private set
		{
			_playerName = value;

			Image claimedDisplay = ClaimedUserMultiDecker;
			if (value != null) claimedDisplay.transform.Find("Username").GetComponent<Text>().text = value;
			claimedDisplay.gameObject.SetActive(value != null);
		}
		get => _playerName;
	}
	#endregion
}
