using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	public TwitchBomb Bomb;

	[HideInInspector]
	public BombComponent BombComponent;

	[HideInInspector]
	public Vector3 BasePosition = Vector3.zero;

	[HideInInspector]
	public Vector3 IdealHandlePositionOffset = Vector3.zero;

	[HideInInspector]
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
			_cameraPriority = value;
			TwitchGame.ModuleCameras.TryViewModule(this);
		}
	}
	public DateTime LastUsed;   // when the module was last viewed or received a command
	private CameraPriority _cameraPriority = CameraPriority.Unviewed;

	public void ViewPin(string user, bool pin)
	{
		if (Solved && !TwitchPlaySettings.data.AnarchyMode) return;

		CameraPriority =
			pin && (UserAccess.HasAccess(user, AccessLevel.Mod, true) || Solver.ModInfo.CameraPinningAlwaysAllowed || TwitchPlaySettings.data.AnarchyMode)
				? CameraPriority.Pinned
				: CameraPriority.Viewed;
		LastUsed = DateTime.UtcNow;
	}

	public ComponentSolver Solver { get; private set; } = null;

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
	public Color unclaimedBackgroundColor = new Color(0, 0, 0);
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
				SetBannerColor(unclaimedBackgroundColor);
			}
			else
			{
				CanvasGroupMultiDecker.alpha = Solved ? 0.0f : 1.0f;
				SetBannerColor(Claimed && !Solved ? ClaimedBackgroundColour : unclaimedBackgroundColor);
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

		if (Solver.ModInfo.ShouldSerializeunclaimedColor() && unclaimedBackgroundColor != Solver.ModInfo.unclaimedColor)
		{
			unclaimedBackgroundColor = Solver.ModInfo.unclaimedColor;
			if (!Claimed || Solved) SetBannerColor(unclaimedBackgroundColor);
		}

		if (!Solver.ModInfo.ShouldSerializeunclaimedColor() && unclaimedBackgroundColor != TwitchPlaySettings.data.UnclaimedColor)
		{
			unclaimedBackgroundColor = TwitchPlaySettings.data.UnclaimedColor;
			if (!Claimed || Solved) SetBannerColor(unclaimedBackgroundColor);
		}
	}

	private void Awake() => _data = GetComponent<TwitchModuleData>();

	private void Start()
	{
		_anarchyMode = TwitchPlaySettings.data.AnarchyMode;

		IDTextMultiDecker.text = Code;

		CanvasGroupMultiDecker.alpha = 1.0f;

		unclaimedBackgroundColor = TwitchPlaySettings.data.UnclaimedColor;//idBannerPrefab.GetComponent<Image>().color;

		try
		{
			Solver = ComponentSolverFactory.CreateSolver(this);
			if (Solver != null)
			{
				if (Solver.ModInfo.ShouldSerializeunclaimedColor()) unclaimedBackgroundColor = Solver.ModInfo.unclaimedColor;

				Solver.Code = Code;
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
						TwitchGame.ModuleCameras?.UpdateSolves();
						OnPass(null);
						TwitchGame.ModuleCameras?.UnviewModule(this);
						return false;
					};

					module.OnStrike += delegate
					{
						TwitchGame.ModuleCameras?.UpdateStrikes();
						return false;
					};
				}
				else if (BombComponent.GetComponent<KMNeedyModule>() != null)
				{
					BombComponent.GetComponent<KMNeedyModule>().OnStrike += delegate
					{
						TwitchGame.ModuleCameras?.UpdateStrikes();
						return false;
					};
				}
			}
		}

		SetBannerColor(unclaimedBackgroundColor);
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

		// Forget Me Not and Forget Everything become unsolvable if more than one module is solved at once.
		if (componentsToRemove.Count > 1)
			TwitchGame.Instance.RemoveSolveBasedModules();

		UnsupportedComponents.Clear();
		return true;
	}

	public void SolveSilently() => Solver.SolveSilently();

	public static void ClearUnsupportedModules() => UnsupportedComponents.Clear();

	public void OnPass(string userNickname)
	{
		CanvasGroupMultiDecker.alpha = TwitchPlaySettings.data.AnarchyMode ? 0.5f : 0.0f;
		if (PlayerName == null)
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
			PlayerName = null;
			TakeInProgress = null;
		}
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

		if (TwitchGame.Instance.LastClaimedModule == null)
		{
			TwitchGame.Instance.LastClaimedModule = new Dictionary<string, Dictionary<string, double>>();
		}

		if (!TwitchGame.Instance.LastClaimedModule.TryGetValue(Solver.ModInfo.moduleID, out Dictionary<string, double> value) || value == null)
		{
			value = new Dictionary<string, double>();
			TwitchGame.Instance.LastClaimedModule[Solver.ModInfo.moduleID] = value;
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
				Tuple<bool, string> claim = ClaimModule(ClaimQueue[i].First, ClaimQueue[i].Third, ClaimQueue[i].Fourth);
				if (!claim.First) continue;
				IRCConnection.SendMessage(claim.Second);
				if (ClaimQueue[i].Third)
					ViewPin(user: ClaimQueue[i].First, pin: ClaimQueue[i].Fourth);
				ClaimQueue.RemoveAt(i);
				break;
			}
		}
	}

	public Tuple<bool, string> ClaimModule(string userNickName, bool viewRequested = false, bool viewPinRequested = false)
	{
		if (Solver.AttemptedForcedSolve)
		{
			return new Tuple<bool, string>(false, string.Format("Sorry @{1}, module ID {0} ({2}) is being solved automatically.", Code, userNickName, HeaderText));
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
			return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.AlreadyClaimed, Code, PlayerName, userNickName, HeaderText));
		}
		if (TwitchGame.Instance.Modules.Count(md => md.PlayerName != null && md.PlayerName.EqualsIgnoreCase(userNickName) && !md.Solved) >= TwitchPlaySettings.data.ModuleClaimLimit && !Solved && (!UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true) || !TwitchPlaySettings.data.SuperStreamerIgnoreClaimLimit))
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

			SetBannerColor(ClaimedBackgroundColour);
			PlayerName = userNickName;
			if (CameraPriority < CameraPriority.Claimed)
				CameraPriority = CameraPriority.Claimed;
			return new Tuple<bool, string>(true, string.Format(TwitchPlaySettings.data.ModuleClaimed, Code, PlayerName, HeaderText));
		}
	}

	public Tuple<bool, string> UnclaimModule(string userNickName)
	{
		if (Solved)
			return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.AlreadySolved, Code, PlayerName, userNickName, BombComponent.GetModuleDisplayName()));

		if (PlayerName == null)
		{
			bool wasQueued = ClaimQueue.Any(x => x.First == userNickName);
			if (wasQueued) RemoveFromClaimQueue(userNickName);

			return new Tuple<bool, string>(false, !wasQueued ? string.Format(TwitchPlaySettings.data.ModuleNotClaimed, userNickName, Code, HeaderText) : null);
		}

		RemoveFromClaimQueue(userNickName);
		if (PlayerName.Equals(userNickName, StringComparison.InvariantCultureIgnoreCase) || UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
		{
			RemoveFromClaimQueue(PlayerName);
			if (TakeInProgress != null)
			{
				StopCoroutine(TakeInProgress);
				TakeInProgress = null;
			}
			SetBannerColor(unclaimedBackgroundColor);
			string messageOut = string.Format(TwitchPlaySettings.data.ModuleUnclaimed, Code, PlayerName, HeaderText);
			PlayerName = null;
			if (CameraPriority > CameraPriority.Interacted)
				CameraPriority = CameraPriority.Interacted;
			return new Tuple<bool, string>(true, messageOut);
		}
		else
		{
			return new Tuple<bool, string>(false, string.Format(TwitchPlaySettings.data.AlreadyClaimed, Code, PlayerName, userNickName, HeaderText));
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
				var setToLayer = _currentLayer ?? kvp.Value;
				if (kvp.Key.gameObject.layer != setToLayer)
					kvp.Key.gameObject.layer = setToLayer;
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

	#region Private Methods
	private bool IsAuthorizedDefuser(string userNickName, bool sendMessage = true) => TwitchGame.IsAuthorizedDefuser(userNickName, !sendMessage);

	public void SetBannerColor(Color color)
	{
		CanvasGroupMultiDecker.GetComponent<Image>().color = color;
		ClaimedUserMultiDecker.color = color;
	}
	#endregion

	#region Properties
	private string _playerName;
	public string PlayerName
	{
		set
		{
			_playerName = value;

			Image claimedDisplay = ClaimedUserMultiDecker;
			if (value != null) claimedDisplay.transform.Find("Username").GetComponent<Text>().text = value;
			claimedDisplay.gameObject.SetActive(value != null);
		}
		get => _playerName;
	}

	public Selectable Selectable => BombComponent.GetComponent<Selectable>();
	public float FocusDistance => Selectable.GetFocusDistance();
	public bool FrontFace
	{
		get
		{
			Vector3 componentUp = transform.up;
			Vector3 bombUp = Bomb.Bomb.transform.up;
			float angleBetween = Vector3.Angle(componentUp, bombUp);
			return angleBetween < 90.0f;
		}
	}

	#endregion
}
