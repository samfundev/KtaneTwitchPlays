using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
	public Vector3 basePosition = Vector3.zero;

	[HideInInspector]
	public Vector3 idealHandlePositionOffset = Vector3.zero;

	[HideInInspector]
	public CoroutineQueue coroutineQueue = null;

	[HideInInspector]
	public bool Claimed => PlayerName != null;

	[HideInInspector]
	public int bombID;

	[HideInInspector]
	public bool Solved => bombComponent.IsSolved;

	[HideInInspector]
	public bool Unsupported = false;

	[HideInInspector]
	public List<Tuple<string, double, bool, bool>> ClaimQueue = new List<Tuple<string, double, bool, bool>>();

	public string Code { get; set; } = null;
	public bool IsKey { get; set; } = false;
	public CameraPriority CameraPriority
	{
		get => _cameraPriority;
		set
		{
			if (_cameraPriority != value)
			{
				_cameraPriority = value;
				BombMessageResponder.moduleCameras.TryViewModule(this);
			}
		}
	}
	public DateTime LastUsed;   // when the module was last viewed or received a command
	private CameraPriority _cameraPriority = CameraPriority.Unviewed;

	public ComponentSolver Solver { get; private set; } = null;

	public bool IsMod => bombComponent is ModBombComponent || bombComponent is ModNeedyComponent;

	public static bool ClaimsEnabled = true;

	private string _headerText;
	public string HeaderText
	{
		get => _headerText ?? bombComponent?.GetModuleDisplayName() ?? string.Empty;
		set => _headerText = value;
	}

	public IEnumerator TakeInProgress = null;
	public static List<string> ClaimedList = new List<string>();
	#endregion

	#region Private Fields
	private Color unclaimedBackgroundColor = new Color(0, 0, 0);
	private TwitchComponentHandleData _data;
	private bool claimCooldown = true;
	private bool statusLightLeft = false;
	private bool statusLightDown = false;
	private Vector3 originalIDPosition = Vector3.zero;
	private bool anarchyMode;
	private Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();
	private int? currentLayer;
	#endregion

	#region Private Statics
	private static List<TwitchComponentHandle> _unsupportedComponents = new List<TwitchComponentHandle>();
	private static List<BombCommander> _bombCommanders = new List<BombCommander>();
	#endregion

	#region Unity Lifecycle
	private void Update()
	{
		if (anarchyMode != TwitchPlaySettings.data.AnarchyMode)
		{
			anarchyMode = TwitchPlaySettings.data.AnarchyMode;
			if (anarchyMode)
			{
				CanvasGroupMultiDecker.alpha = Solved ? 0.5f : 1.0f;
				SetBannerColor(unclaimedBackgroundColor);
			}
			else
			{
				CanvasGroupMultiDecker.alpha = Solved ? 0.0f : 1.0f;
				SetBannerColor(Claimed && !Solved ? ClaimedBackgroundColour : unclaimedBackgroundColor);
			}
		}

		if (originalIDPosition == Vector3.zero) return;
		if (Solver.modInfo.statusLightLeft != statusLightLeft || Solver.modInfo.statusLightDown != statusLightDown)
		{
			Vector3 pos = originalIDPosition;
			CanvasGroupMultiDecker.transform.localPosition = new Vector3(Solver.modInfo.statusLightLeft ? -pos.x : pos.x, pos.y, Solver.modInfo.statusLightDown ? -pos.z : pos.z);
			statusLightLeft = Solver.modInfo.statusLightLeft;
			statusLightDown = Solver.modInfo.statusLightDown;
		}

		if (Solver.modInfo.ShouldSerializeunclaimedColor() && unclaimedBackgroundColor != Solver.modInfo.unclaimedColor)
		{
			unclaimedBackgroundColor = Solver.modInfo.unclaimedColor;
			if (!Claimed || Solved) SetBannerColor(unclaimedBackgroundColor);
		}

		if (!Solver.modInfo.ShouldSerializeunclaimedColor() && unclaimedBackgroundColor != TwitchPlaySettings.data.UnclaimedColor)
		{
			unclaimedBackgroundColor = TwitchPlaySettings.data.UnclaimedColor;
			if (!Claimed || Solved) SetBannerColor(unclaimedBackgroundColor);
		}
	}

	private void Awake() => _data = GetComponent<TwitchComponentHandleData>();

	private void Start()
	{
		anarchyMode = TwitchPlaySettings.data.AnarchyMode;

		IDTextMultiDecker.text = Code;

		CanvasGroupMultiDecker.alpha = 1.0f;

		unclaimedBackgroundColor = TwitchPlaySettings.data.UnclaimedColor;//idBannerPrefab.GetComponent<Image>().color;

		try
		{
			Solver = ComponentSolverFactory.CreateSolver(bombCommander, bombComponent);
			if (Solver != null)
			{
				if (Solver.modInfo.ShouldSerializeunclaimedColor()) unclaimedBackgroundColor = Solver.modInfo.unclaimedColor;

				Solver.Code = Code;
				Solver.ComponentHandle = this;
				Vector3 pos = CanvasGroupMultiDecker.transform.localPosition;
				originalIDPosition = pos;
				CanvasGroupMultiDecker.transform.localPosition = new Vector3(Solver.modInfo.statusLightLeft ? -pos.x : pos.x, pos.y, Solver.modInfo.statusLightDown ? -pos.z : pos.z);
				statusLightLeft = Solver.modInfo.statusLightLeft;
				statusLightDown = Solver.modInfo.statusLightDown;
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
						BombMessageResponder.moduleCameras?.UnviewModule(this);
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
		IRCConnection.SendMessage(TwitchPlaySettings.data.UnsupportedNeedyWarning);
		KMNeedyModule needyModule = handle.bombComponent.GetComponent<KMNeedyModule>();
		needyModule.OnNeedyActivation = () => { needyModule.StartCoroutine(KeepUnsupportedNeedySilent(needyModule)); };
		needyModule.OnNeedyDeactivation = () => { needyModule.StopAllCoroutines(); needyModule.HandlePass(); };
		needyModule.OnTimerExpired = () => { needyModule.StopAllCoroutines(); needyModule.HandlePass(); };
		needyModule.WarnAtFiveSeconds = false;
	}

	public static bool UnsupportedModulesPresent() => _unsupportedComponents.Any(x => x.Solver == null || !x.Solved);

	public static bool SolveUnsupportedModules(bool bombStartup = false)
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

		if (componentsToRemove.Count > 1) //Forget Me Not and Forget Everything become unsolvable if MORE than one module is solved at once.
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

	public void SolveSilently() => Solver.SolveSilently();

	public static void ClearUnsupportedModules()
	{
		_bombCommanders.Clear();
		_unsupportedComponents.Clear();
	}

	public void OnPass(string userNickname)
	{
		CanvasGroupMultiDecker.alpha = TwitchPlaySettings.data.AnarchyMode ? 0.5f : 0.0f;
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

	private void OnDestroy() => StopAllCoroutines();
	#endregion

	#region Public Methods
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
			IRCConnection.SendMessage(TwitchPlaySettings.data.ModuleAbandoned, targetModule, PlayerName, HeaderText);
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

	public Tuple<bool, double> CanClaimNow(string userNickName, bool updatePreviousClaim, bool force = false)
	{
		if (TwitchPlaySettings.data.AnarchyMode) return new Tuple<bool, double>(false, DateTime.Now.TotalSeconds());

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

	public void AddToClaimQueue(string userNickname, bool viewRequested = false, bool viewPinRequested = false)
	{
		double seconds = CanClaimNow(userNickname, false).Second;
		if (ClaimQueue.Any(x => x.First.Equals(userNickname, StringComparison.InvariantCultureIgnoreCase))) return;
		for (int i = 0; i < ClaimQueue.Count; i++)
		{
			if (ClaimQueue[i].Second < seconds) continue;
			ClaimQueue.Insert(i, new Tuple<string, double, bool, bool>(userNickname, seconds, viewRequested, viewPinRequested));
			return;
		}
		ClaimQueue.Add(new Tuple<string, double, bool, bool>(userNickname, seconds, viewRequested, viewPinRequested));
	}

	public void RemoveFromClaimQueue(string userNickname) => ClaimQueue.RemoveAll(x => x.First.Equals(userNickname, StringComparison.InvariantCultureIgnoreCase));

	public IEnumerator AutoAssignModule()
	{
		StartCoroutine(EndClaimCooldown());
		while (!Solved && !Solver.AttemptedForcedSolve)
		{
			yield return new WaitForSeconds(0.1f);
			if (PlayerName != null || ClaimQueue.Count == 0) continue;

			for (int i = 0; i < ClaimQueue.Count; i++)
			{
				Tuple<bool, string> claim = ClaimModule(ClaimQueue[i].First, Code, ClaimQueue[i].Third, ClaimQueue[i].Fourth);
				if (!claim.First) continue;
				IRCConnection.SendMessage(claim.Second);
				if (ClaimQueue[i].Third) IRCConnection.Instance.OnMessageReceived.Invoke(new Message(ClaimQueue[i].First, null, $"!{Code} view{(ClaimQueue[i].Fourth ? " pin" : "")}"));
				ClaimQueue.RemoveAt(i);
				break;
			}
		}
	}

	public Tuple<bool, string> ClaimModule(string userNickName, string targetModule, bool viewRequested = false, bool viewPinRequested = false)
	{
		if (Solver.AttemptedForcedSolve)
		{
			return new Tuple<bool, string>(false, string.Format("Sorry @{1}, module ID {0} ({2}) is being solved automatically.", targetModule, userNickName, HeaderText));
		}

		if (TwitchPlaySettings.data.AnarchyMode)
		{
			return new Tuple<bool, string>(false, string.Format("Sorry {0}, claiming modules is not allowed in anarchy mode.", userNickName));
		}

		if (!ClaimsEnabled && !UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
		{
			return new Tuple<bool, string>(false, string.Format("Sorry {0}, claims have been disabled.", userNickName));
		}

		if (PlayerName != null)
		{
			if (!PlayerName.Equals(userNickName))
				AddToClaimQueue(userNickName, viewRequested, viewPinRequested);
			return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.AlreadyClaimed, targetModule, PlayerName, HeaderText));
		}
		if (ClaimedList.Count(nick => nick.Equals(userNickName, StringComparison.InvariantCultureIgnoreCase)) >= TwitchPlaySettings.data.ModuleClaimLimit && !Solved && (!UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true) || !TwitchPlaySettings.data.SuperStreamerIgnoreClaimLimit))
		{
			AddToClaimQueue(userNickName, viewRequested, viewPinRequested);
			return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.TooManyClaimed, userNickName, TwitchPlaySettings.data.ModuleClaimLimit));
		}
		else
		{
			Tuple<bool, double> claim = CanClaimNow(userNickName, true);
			if (!claim.First)
			{
				AddToClaimQueue(userNickName, viewRequested, viewPinRequested);
				return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.ClaimCooldown, Code, TwitchPlaySettings.data.InstantModuleClaimCooldown, userNickName, HeaderText));
			}

			ClaimedList.Add(userNickName);
			SetBannerColor(ClaimedBackgroundColour);
			PlayerName = userNickName;
			if (CameraPriority < CameraPriority.Claimed)
				CameraPriority = CameraPriority.Claimed;
			return new Tuple<bool, string>(true, string.Format(TwitchPlaySettings.data.ModuleClaimed, targetModule, PlayerName, HeaderText));
		}
	}

	public Tuple<bool, string> UnclaimModule(string userNickName, string targetModule)
	{
		if (PlayerName == null)
		{
			bool wasQueued = ClaimQueue.Any(x => x.First == userNickName);
			if (wasQueued) RemoveFromClaimQueue(userNickName);

			return new Tuple<bool, string>(false, !wasQueued ? string.Format(TwitchPlaySettings.data.ModuleNotClaimed, userNickName, targetModule, HeaderText) : null);
		}

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
			if (CameraPriority > CameraPriority.Interacted)
				CameraPriority = CameraPriority.Interacted;
			return new Tuple<bool, string>(true, messageOut);
		}
		else
		{
			return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.AlreadyClaimed, targetModule, PlayerName, userNickName, HeaderText));
		}
	}

	public void CommandError(string userNickName, string message) => IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.CommandError, userNickName, Code, HeaderText, message));

	public void CommandInvalid(string userNickName) => IRCConnection.SendMessage(TwitchPlaySettings.data.InvalidCommand, userNickName, Code, HeaderText);

	public void UpdateLayerData()
	{
		foreach (var trans in bombComponent.gameObject.GetComponentsInChildren<Transform>(true))
		{
			if (originalLayers.ContainsKey(trans))
				continue;
			originalLayers.Add(trans, trans.gameObject.layer);
			try
			{
				if (currentLayer != null)
					trans.gameObject.layer = currentLayer.Value;
			}
			catch
			{
				//continue;
			}
		}

		foreach (var trans in gameObject.GetComponentsInChildren<Transform>(true))
		{
			if (originalLayers.ContainsKey(trans))
				continue;
			originalLayers.Add(trans, trans.gameObject.layer);
			try
			{
				if (currentLayer != null)
					trans.gameObject.layer = currentLayer.Value;
			}
			catch
			{
				//continue;
			}
		}
	}

	public void SetRenderLayer(int? layer)
	{
		currentLayer = layer;
		foreach (var kvp in originalLayers)
		{
			try
			{
				kvp.Key.gameObject.layer = currentLayer ?? kvp.Value;
			}
			catch
			{
				//continue;
			}
		}

		Light[] lights = bombComponent.GetComponentsInChildren<Light>(true);
		if (lights == null)
			return;
		foreach (var light in lights)
		{
			light.enabled = !light.enabled;
			light.enabled = !light.enabled;
		}
	}
	#endregion

	#region Message Interface
	public IEnumerator OnMessageReceived(Message message)
	{
		string unprocessedCommand = message.Text.Trim();
		string internalCommand = unprocessedCommand.ToLower().Trim();
		string userNickName = message.UserNickName;
		bool isWhisper = message.IsWhisper;

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
					messageOut = string.Format(TwitchPlaySettings.data.TurnBombOnSolve, Code, HeaderText);
				}
				else if (Regex.IsMatch(internalCommand, "^cancel (bomb|queue) (turn( a?round)?|flip|spin)$", RegexOptions.IgnoreCase))
				{
					Solver._turnQueued = false;
					messageOut = string.Format(TwitchPlaySettings.data.CancelBombTurn, Code, HeaderText);
				}
				else if (internalCommand.Equals("claim", StringComparison.InvariantCultureIgnoreCase))
				{
					if (isWhisper)
					{
						IRCConnection.SendMessage($"Sorry {userNickName}, claiming modules is not allowed in whispers", userNickName, false);
						return null;
					}
					messageOut = ClaimModule(userNickName, Code).Second;
				}
				else if (internalCommand.ToLowerInvariant().EqualsAny("release", "unclaim"))
				{
					if (isWhisper)
					{
						IRCConnection.SendMessage($"Sorry {userNickName}, unclaiming modules is not allowed in whispers", userNickName, false);
						return null;
					}
					messageOut = UnclaimModule(userNickName, Code).Second;

					// If UnclaimModule responds with a null message, someone tried to unclaim a module that no one has claimed but they were waiting to claim.
					// It's valid command and they were removed from the queue but no message is sent.
					if (messageOut == null) return null;
				}
				else if (internalCommand.ToLowerInvariant().EqualsAny("claim view", "view claim", "claimview", "viewclaim", "cv", "vc",
					"claim view pin", "view pin claim", "claimviewpin", "viewpinclaim", "cvp", "vpc"))
				{
					if (isWhisper)
					{
						IRCConnection.SendMessage($"Sorry {userNickName}, claiming modules is not allowed in whispers", userNickName, false);
						return null;
					}
					Tuple<bool, string> response = ClaimModule(userNickName, Code, true, internalCommand.Contains("p"));
					if (response.First)
					{
						IRCConnection.SendMessage(response.Second);
						internalCommand = internalCommand.Contains("p") ? "view pin" : "view";
					}
					else
					{
						messageOut = response.Second;
					}
				}
				else if (internalCommand.ToLowerInvariant().EqualsAny("unclaim unview", "unview unclaim", "unclaimview", "unviewclaim", "uncv", "unvc"))
				{
					if (isWhisper)
					{
						IRCConnection.SendMessage($"Sorry {userNickName}, unclaiming modules is not allowed in whispers", userNickName, false);
						return null;
					}
					Tuple<bool, string> response = UnclaimModule(userNickName, Code);

					// If UnclaimModule responds with a null message, someone tried to unclaim a module that no one has claimed but they were waiting to claim.
					// It's valid command and they were removed from the queue but no message is sent.
					if (response.Second == null) return null;

					if (response.First)
					{
						IRCConnection.SendMessage(response.Second);
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
						messageOut = string.Format(TwitchPlaySettings.data.ModuleReady, Code, userNickName, HeaderText);
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
						if (TwitchPlaySettings.data.AnarchyMode)
						{
							messageOut = string.Format("Sorry {0}, assigning modules is not allowed in anarchy mode.", userNickName);
						}
						else
						{
							if (PlayerName != null)
							{
								ClaimedList.Remove(PlayerName);
							}

							string newplayerName = unprocessedCommand.Remove(0, 7).Trim();
							PlayerName = newplayerName;
							ClaimedList.Add(PlayerName);
							RemoveFromClaimQueue(userNickName);
							CanClaimNow(userNickName, true, true);
							SetBannerColor(ClaimedBackgroundColour);
							messageOut = string.Format(TwitchPlaySettings.data.AssignModule, Code, PlayerName, userNickName, HeaderText);
						}
					}
					else
					{
						return null;
					}
				}
				else if (internalCommand.Equals("take", StringComparison.InvariantCultureIgnoreCase))
				{
					if (isWhisper)
					{
						IRCConnection.SendMessage($"Sorry {userNickName}, taking modules is not allowed in whispers", userNickName, false);
						return null;
					}
					if (TwitchPlaySettings.data.AnarchyMode)
					{
						messageOut = string.Format("Sorry {0}, taking modules is not allowed in anarchy mode.", userNickName);
					}
					else if (PlayerName != null && userNickName != PlayerName)
					{
						AddToClaimQueue(userNickName);
						if (TakeInProgress == null)
						{
							messageOut = string.Format(TwitchPlaySettings.data.TakeModule, PlayerName, userNickName, Code, HeaderText);
							TakeInProgress = TakeModule(userNickName, Code);
							StartCoroutine(TakeInProgress);
						}
						else
						{
							messageOut = string.Format(TwitchPlaySettings.data.TakeInProgress, userNickName, Code, HeaderText);
						}
					}
					else if (PlayerName != null)
					{
						if (!PlayerName.Equals(userNickName))
							AddToClaimQueue(userNickName);
						messageOut = string.Format(TwitchPlaySettings.data.ModuleAlreadyOwned, userNickName, Code, HeaderText);
					}
					else
					{
						messageOut = ClaimModule(userNickName, Code).Second;
					}
				}
				else if (internalCommand.Equals("mine", StringComparison.InvariantCultureIgnoreCase))
				{
					if (isWhisper)
					{
						IRCConnection.SendMessage($"Sorry {userNickName}, using mine on modules is not allowed in whispers", userNickName, false);
						return null;
					}
					if (PlayerName == userNickName && TakeInProgress != null)
					{
						messageOut = string.Format(TwitchPlaySettings.data.ModuleIsMine, PlayerName, Code, HeaderText);
						StopCoroutine(TakeInProgress);
						TakeInProgress = null;
					}
					else if (PlayerName == null)
					{
						messageOut = ClaimModule(userNickName, Code).Second;
					}
					else if (PlayerName == userNickName)
					{
						messageOut = string.Format(TwitchPlaySettings.data.NoTakes, userNickName, Code, HeaderText);
					}
					else
					{
						messageOut = string.Format(TwitchPlaySettings.data.AlreadyClaimed, Code, PlayerName, userNickName, HeaderText);
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
				else if (internalCommand.Equals("points", StringComparison.InvariantCultureIgnoreCase) || internalCommand.Equals("score", StringComparison.InvariantCultureIgnoreCase))
				{
					IRCConnection.SendMessage("{0} ({1}) Current score: {2}", HeaderText, Code, Solver.modInfo.moduleScore);

					return null;
				}
				else if (internalCommand.Equals("unmark", StringComparison.InvariantCultureIgnoreCase))
				{
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						if (!Claimed)
							SetBannerColor(unclaimedBackgroundColor);
						else
							SetBannerColor(ClaimedBackgroundColour);
					}

					return null;
				}
			}
		}

		if (internalCommand.Equals("player", StringComparison.InvariantCultureIgnoreCase))
		{
			messageOut = PlayerName != null
				? string.Format(TwitchPlaySettings.data.ModulePlayer, Code, PlayerName, HeaderText)
				: string.Format(TwitchPlaySettings.data.ModuleNotClaimed, userNickName, Code, HeaderText);
		}

		if (!string.IsNullOrEmpty(messageOut))
		{
			IRCConnection.SendMessage(messageOut, Code, HeaderText);
			return null;
		}

		if (Solver != null)
		{
			if (!IsAuthorizedDefuser(userNickName, false)) return null;

			if (Solved && !TwitchPlaySettings.data.AnarchyMode)
			{
				IRCConnection.SendMessage(TwitchPlaySettings.data.AlreadySolved, Code, PlayerName, userNickName, HeaderText);
				return null;
			}

			bool moduleAlreadyClaimed = bombCommander.CurrentTimer > TwitchPlaySettings.data.MinTimeLeftForClaims;
			moduleAlreadyClaimed &= BombMessageResponder.Instance.ComponentHandles.Count(x => !x.Solved && GameRoom.Instance.IsCurrentBomb(x.bombID)) >= TwitchPlaySettings.data.MinUnsolvedModulesLeftForClaims;
			moduleAlreadyClaimed &= PlayerName != null;
			moduleAlreadyClaimed &= !TwitchPlaySettings.data.AnarchyMode;
			moduleAlreadyClaimed &= PlayerName != userNickName;
			moduleAlreadyClaimed &= !internalCommand.Equals("take", StringComparison.InvariantCultureIgnoreCase);
			moduleAlreadyClaimed &= !(internalCommand.ToLowerInvariant().EqualsAny("viewpin", "view pin") && UserAccess.HasAccess(userNickName, AccessLevel.Mod, true));
			moduleAlreadyClaimed &= !(internalCommand.Equals("solve", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.Admin, true));
			if (moduleAlreadyClaimed)
			{
				IRCConnection.SendMessage(TwitchPlaySettings.data.AlreadyClaimed, Code, PlayerName, userNickName, HeaderText);
				return null;
			}

			// Twitch allows newlines in messages, even though they show up in the chat window as spaces, so pretend they’re spaces
			return RespondToCommandCoroutine(userNickName, internalCommand.Replace("\n", " ").Replace("\r", ""));
		}
		else
		{
			return null;
		}
	}
	#endregion

	#region Private Methods
	private bool IsAuthorizedDefuser(string userNickName, bool sendMessage = true) => MessageResponder.IsAuthorizedDefuser(userNickName, !sendMessage);

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
