using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class TwitchModule : MonoBehaviour
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
	public BombCommander BombCommander;

	[HideInInspector]
	public BombComponent BombComponent;

	[HideInInspector]
	public Vector3 BasePosition = Vector3.zero;

	[HideInInspector]
	public Vector3 IdealHandlePositionOffset = Vector3.zero;

	[HideInInspector]
	public CoroutineQueue CoroutineQueue;

	public bool Claimed => PlayerName != null;

	[HideInInspector]
	public int BombID;

	public bool Solved => BombComponent.IsSolved;

	[HideInInspector]
	public bool Unsupported;

	[HideInInspector]
	public List<Tuple<string, double, bool, bool>> ClaimQueue = new List<Tuple<string, double, bool, bool>>();

	public string Code { get; set; }
	public bool IsKey { get; set; }
	public CameraPriority CameraPriority
	{
		get => _cameraPriority;
		set
		{
			if (_cameraPriority != value)
			{
				_cameraPriority = value;
				BombMessageResponder.ModuleCameras.TryViewModule(this);
			}
		}
	}
	public DateTime LastUsed;   // when the module was last viewed or received a command
	private CameraPriority _cameraPriority = CameraPriority.Unviewed;

	public ComponentSolver Solver { get; private set; }

	public bool IsMod => BombComponent is ModBombComponent || BombComponent is ModNeedyComponent;

	public static bool ClaimsEnabled = true;

	private string _headerText;
	public string HeaderText
	{
		get => _headerText ?? (BombComponent != null ? BombComponent.GetModuleDisplayName() ?? string.Empty : string.Empty);
		set => _headerText = value;
	}

	public IEnumerator TakeInProgress;
	public static List<string> ClaimedList = new List<string>();
	#endregion

	#region Private Fields
	private Color _unclaimedBackgroundColor = new Color(0, 0, 0);
	private TwitchModuleData _data;
	private bool _claimCooldown = true;
	private bool _statusLightLeft;
	private bool _statusLightDown;
	private Vector3 _originalIDPosition = Vector3.zero;
	private bool _anarchyMode;
	private readonly Dictionary<Transform, int> _originalLayers = new Dictionary<Transform, int>();
	private int? _currentLayer;
	#endregion

	#region Private Statics
	private static readonly List<TwitchModule> UnsupportedComponents = new List<TwitchModule>();
	private static readonly List<BombCommander> BombCommanders = new List<BombCommander>();
	#endregion

	#region Unity Lifecycle
	private void Update()
	{
		if (_anarchyMode != TwitchPlaySettings.data.AnarchyMode)
		{
			_anarchyMode = TwitchPlaySettings.data.AnarchyMode;
			if (_anarchyMode)
			{
				CanvasGroupMultiDecker.alpha = Solved ? 0.5f : 1.0f;
				SetBannerColor(_unclaimedBackgroundColor);
			}
			else
			{
				CanvasGroupMultiDecker.alpha = Solved ? 0.0f : 1.0f;
				SetBannerColor(Claimed && !Solved ? ClaimedBackgroundColour : _unclaimedBackgroundColor);
			}
		}

		if (_originalIDPosition == Vector3.zero) return;
		if (Solver.ModInfo.statusLightLeft != _statusLightLeft || Solver.ModInfo.statusLightDown != _statusLightDown)
		{
			Vector3 pos = _originalIDPosition;
			CanvasGroupMultiDecker.transform.localPosition = new Vector3(Solver.ModInfo.statusLightLeft ? -pos.x : pos.x, pos.y, Solver.ModInfo.statusLightDown ? -pos.z : pos.z);
			_statusLightLeft = Solver.ModInfo.statusLightLeft;
			_statusLightDown = Solver.ModInfo.statusLightDown;
		}

		if (Solver.ModInfo.ShouldSerializeunclaimedColor() && _unclaimedBackgroundColor != Solver.ModInfo.unclaimedColor)
		{
			_unclaimedBackgroundColor = Solver.ModInfo.unclaimedColor;
			if (!Claimed || Solved) SetBannerColor(_unclaimedBackgroundColor);
		}

		if (!Solver.ModInfo.ShouldSerializeunclaimedColor() && _unclaimedBackgroundColor != TwitchPlaySettings.data.UnclaimedColor)
		{
			_unclaimedBackgroundColor = TwitchPlaySettings.data.UnclaimedColor;
			if (!Claimed || Solved) SetBannerColor(_unclaimedBackgroundColor);
		}
	}

	private void Awake() => _data = GetComponent<TwitchModuleData>();

	private void Start()
	{
		_anarchyMode = TwitchPlaySettings.data.AnarchyMode;

		IDTextMultiDecker.text = Code;

		CanvasGroupMultiDecker.alpha = 1.0f;

		_unclaimedBackgroundColor = TwitchPlaySettings.data.UnclaimedColor;//idBannerPrefab.GetComponent<Image>().color;

		try
		{
			Solver = ComponentSolverFactory.CreateSolver(BombCommander, BombComponent);
			if (Solver != null)
			{
				if (Solver.ModInfo.ShouldSerializeunclaimedColor()) _unclaimedBackgroundColor = Solver.ModInfo.unclaimedColor;

				Solver.Code = Code;
				Solver.ComponentHandle = this;
				Vector3 pos = CanvasGroupMultiDecker.transform.localPosition;
				_originalIDPosition = pos;
				CanvasGroupMultiDecker.transform.localPosition = new Vector3(Solver.ModInfo.statusLightLeft ? -pos.x : pos.x, pos.y, Solver.ModInfo.statusLightDown ? -pos.z : pos.z);
				_statusLightLeft = Solver.ModInfo.statusLightLeft;
				_statusLightDown = Solver.ModInfo.statusLightDown;
				RectTransform rectTransform = ClaimedUserMultiDecker.rectTransform;
				rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(Solver.ModInfo.statusLightLeft ? 1 : 0, Solver.ModInfo.statusLightDown ? 0 : 1);
				rectTransform.pivot = new Vector2(Solver.ModInfo.statusLightLeft ? 0 : 1, Solver.ModInfo.statusLightDown ? 0 : 1);

				CanvasGroupUnsupported.gameObject.SetActive(Solver.UnsupportedModule);

				IDTextUnsupported.text = BombComponent is ModBombComponent
					? $"To solve this\nmodule, use\n!{Code} solve"
					: $"To disarm this\nneedy, use\n!{Code} solve";
				if (Solver.UnsupportedModule)
					UnsupportedComponents.Add(this);

				StartCoroutine(AutoAssignModule());
			}
		}
		catch (Exception e)
		{
			DebugHelper.LogException(e);
			CanvasGroupMultiDecker.alpha = 0.0f;
			UnsupportedComponents.Add(this);
			Solver = null;

			CanvasGroupUnsupported.gameObject.SetActive(true);
			IDTextUnsupported.gameObject.SetActive(false);

			if (TwitchPlaySettings.data.EnableTwitchPlaysMode && !TwitchPlaySettings.data.EnableInteractiveMode)
			{
				DebugHelper.Log("An unimplemented module was added to a bomb, solving module.");
			}

			if (BombComponent != null)
			{
				if (BombComponent.GetComponent<KMBombModule>() != null)
				{
					KMBombModule module = BombComponent.GetComponent<KMBombModule>();
					module.OnPass += delegate
					{
						BombCommander.BombSolvedModules++;
						if (BombMessageResponder.ModuleCameras != null)
						{
							BombMessageResponder.ModuleCameras.UpdateSolves();
							OnPass(null);
							BombMessageResponder.ModuleCameras.UnviewModule(this);
						}
						else
						{
							OnPass(null);
						}
						return false;
					};

					module.OnStrike += delegate
					{
						if (BombMessageResponder.ModuleCameras != null)
							BombMessageResponder.ModuleCameras.UpdateStrikes();
						return false;
					};
				}
				else if (BombComponent.GetComponent<KMNeedyModule>() != null)
				{
					BombComponent.GetComponent<KMNeedyModule>().OnStrike += delegate
					{
						if (BombMessageResponder.ModuleCameras != null)
							BombMessageResponder.ModuleCameras.UpdateStrikes();
						return false;
					};
				}
			}
		}

		SetBannerColor(_unclaimedBackgroundColor);

		if (!BombCommanders.Contains(BombCommander))
		{
			BombCommanders.Add(BombCommander);
		}
	}

	public static void DeactivateNeedyModule(TwitchModule handle)
	{
		IRCConnection.SendMessage(TwitchPlaySettings.data.UnsupportedNeedyWarning);
		KMNeedyModule needyModule = handle.BombComponent.GetComponent<KMNeedyModule>();
		needyModule.OnNeedyActivation = () => { needyModule.StopAllCoroutines(); needyModule.gameObject.SetActive(false); needyModule.HandlePass(); needyModule.gameObject.SetActive(true); };
		needyModule.OnNeedyDeactivation = () => { needyModule.StopAllCoroutines(); needyModule.gameObject.SetActive(false); needyModule.HandlePass(); needyModule.gameObject.SetActive(true); };
		needyModule.OnTimerExpired = () => { needyModule.StopAllCoroutines(); needyModule.gameObject.SetActive(false); needyModule.HandlePass(); needyModule.gameObject.SetActive(true); };
		needyModule.WarnAtFiveSeconds = false;
	}

	public static bool UnsupportedModulesPresent() => UnsupportedComponents.Any(x => x.Solver == null || !x.Solved);

	public static bool SolveUnsupportedModules(bool bombStartup = false)
	{
		List<TwitchModule> componentsToRemove = bombStartup
			? UnsupportedComponents.Where(x => x.Solver == null).ToList()
			: UnsupportedComponents.Where(x => x.Solver == null || !x.Solved).ToList();

		if (componentsToRemove.Count == 0) return false;

		foreach (TwitchModule handle in componentsToRemove)
		{
			if (handle.BombComponent.GetComponent<KMNeedyModule>() != null)
			{
				DeactivateNeedyModule(handle);
			}
			handle.SolveSilently();
		}

		if (componentsToRemove.Count > 1) //Forget Me Not and Forget Everything become unsolvable if MORE than one module is solved at once.
			RemoveSolveBasedModules();

		UnsupportedComponents.Clear();
		return true;
	}

	public static void RemoveSolveBasedModules()
	{
		foreach (BombCommander commander in BombCommanders)
		{
			commander.RemoveSolveBasedModules();
		}
	}

	public void SolveSilently() => Solver.SolveSilently();

	public static void ClearUnsupportedModules()
	{
		BombCommanders.Clear();
		UnsupportedComponents.Clear();
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
			if (!BombCommander.SolvedModules.ContainsKey(Solver.ModInfo.moduleDisplayName))
				BombCommander.SolvedModules[Solver.ModInfo.moduleDisplayName] = new List<TwitchModule>();
			BombCommander.SolvedModules[Solver.ModInfo.moduleDisplayName].Add(this);
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
		SetBannerColor(_unclaimedBackgroundColor);
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
		_claimCooldown = false;
	}

	public Tuple<bool, double> CanClaimNow(string userNickName, bool updatePreviousClaim, bool force = false)
	{
		if (TwitchPlaySettings.data.AnarchyMode) return new Tuple<bool, double>(false, DateTime.Now.TotalSeconds());

		if (string.IsNullOrEmpty(userNickName)) return new Tuple<bool, double>(false, DateTime.Now.TotalSeconds());

		if (BombMessageResponder.Instance.LastClaimedModule == null)
		{
			BombMessageResponder.Instance.LastClaimedModule = new Dictionary<string, Dictionary<string, double>>();
		}

		if (!BombMessageResponder.Instance.LastClaimedModule.TryGetValue(Solver.ModInfo.moduleID, out Dictionary<string, double> value) || value == null)
		{
			value = new Dictionary<string, double>();
			BombMessageResponder.Instance.LastClaimedModule[Solver.ModInfo.moduleID] = value;
		}
		if (_claimCooldown && !force && value.TryGetValue(userNickName, out double seconds) &&
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
			return new Tuple<bool, string>(false, $"Sorry {userNickName}, claiming modules is not allowed in anarchy mode.");
		}

		if (!ClaimsEnabled && !UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
		{
			return new Tuple<bool, string>(false, $"Sorry {userNickName}, claims have been disabled.");
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
			SetBannerColor(_unclaimedBackgroundColor);
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

	public void CommandError(string userNickName, string message) => IRCConnection.SendMessage(TwitchPlaySettings.data.CommandError, userNickName, Code, HeaderText, message);

	public void CommandInvalid(string userNickName) => IRCConnection.SendMessage(TwitchPlaySettings.data.InvalidCommand, userNickName, Code, HeaderText);

	public void UpdateLayerData()
	{
		foreach (var trans in BombComponent.gameObject.GetComponentsInChildren<Transform>(true))
		{
			if (_originalLayers.ContainsKey(trans))
				continue;
			_originalLayers.Add(trans, trans.gameObject.layer);
			try
			{
				if (_currentLayer != null)
					trans.gameObject.layer = _currentLayer.Value;
			}
			catch
			{
				//continue;
			}
		}

		foreach (var trans in gameObject.GetComponentsInChildren<Transform>(true))
		{
			if (_originalLayers.ContainsKey(trans))
				continue;
			_originalLayers.Add(trans, trans.gameObject.layer);
			try
			{
				if (_currentLayer != null)
					trans.gameObject.layer = _currentLayer.Value;
			}
			catch
			{
				//continue;
			}
		}
	}

	public void SetRenderLayer(int? layer)
	{
		_currentLayer = layer;
		foreach (var kvp in _originalLayers)
		{
			try
			{
				kvp.Key.gameObject.layer = _currentLayer ?? kvp.Value;
			}
			catch
			{
				//continue;
			}
		}

		Light[] lights = BombComponent.GetComponentsInChildren<Light>(true);
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
			string manualType = "html";
			if ((internalCommand.Length > 7) && (internalCommand.Substring(7) == "pdf"))
			{
				manualType = "pdf";
			}

			string manualText = string.IsNullOrEmpty(Solver.ModInfo.manualCode) ? HeaderText : Solver.ModInfo.manualCode;

			if (manualText.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
				manualText.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
			{
				messageOut = $"{HeaderText} : {Solver.ModInfo.helpText} : {manualText}";
			}
			else
			{
				messageOut = $"{HeaderText} : {Solver.ModInfo.helpText} : {UrlHelper.Instance.ManualFor(manualText, manualType, VanillaRuleModifier.GetModuleRuleSeed(Solver.ModInfo.moduleID) != 1)}";
			}
		}
		else if (!Solved)
		{
			if (IsAuthorizedDefuser(userNickName, false))
			{
				if (Regex.IsMatch(internalCommand, "^(bomb|queue) (turn( a?round)?|flip|spin)$", RegexOptions.IgnoreCase))
				{
					if (!Solver.TurnQueued)
					{
						Solver.TurnQueued = true;
						StartCoroutine(Solver.TurnBombOnSolve());
					}
					messageOut = string.Format(TwitchPlaySettings.data.TurnBombOnSolve, Code, HeaderText);
				}
				else if (Regex.IsMatch(internalCommand, "^cancel (bomb|queue) (turn( a?round)?|flip|spin)$", RegexOptions.IgnoreCase))
				{
					Solver.TurnQueued = false;
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
							messageOut = $"Sorry {userNickName}, assigning modules is not allowed in anarchy mode.";
						}
						else
						{
							if (PlayerName != null)
								ClaimedList.Remove(PlayerName);
							if (TakeInProgress != null)
							{
								StopCoroutine(TakeInProgress);
								TakeInProgress = null;
							}

							string newPlayerName = unprocessedCommand.Remove(0, 7).Trim();
							PlayerName = newPlayerName;
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
						messageOut = $"Sorry {userNickName}, taking modules is not allowed in anarchy mode.";
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
					IRCConnection.SendMessage("{0} ({1}) Current score: {2}", HeaderText, Code, Solver.ModInfo.moduleScore);

					return null;
				}
				else if (internalCommand.Equals("unmark", StringComparison.InvariantCultureIgnoreCase))
				{
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						SetBannerColor(!Claimed ? _unclaimedBackgroundColor : ClaimedBackgroundColour);
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

			bool moduleAlreadyClaimed = BombCommander.CurrentTimer > TwitchPlaySettings.data.MinTimeLeftForClaims;
			moduleAlreadyClaimed &= BombMessageResponder.Instance.ComponentHandles.Count(x => !x.Solved && GameRoom.Instance.IsCurrentBomb(x.BombID)) >= TwitchPlaySettings.data.MinUnsolvedModulesLeftForClaims;
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

	// ReSharper disable once UnusedParameter.Local
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
	#endregion

	#region Private Properties
	private string _playerName;
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
