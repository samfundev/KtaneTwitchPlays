using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TwitchPlays.ScoreMethods;
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
	public bool CanBeClaimed => !(Solver != null && Solver.ModInfo.unclaimable);

	[HideInInspector]
	public int BombID;

	public bool Solved => BombComponent.IsSolved;

	[HideInInspector]
	public bool Unsupported;

	[HideInInspector]
	public bool Votesolving;

	[HideInInspector]
	public List<ClaimQueueItem> ClaimQueue = new List<ClaimQueueItem>();

	public string Code { get; set; }
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

		CameraPriority |= CameraPriority.Viewed;

		if (pin && (UserAccess.HasAccess(user, AccessLevel.Mod, true) || Solver.ModInfo.CameraPinningAlwaysAllowed || BombComponent is NeedyComponent || TwitchPlaySettings.data.AnarchyMode))
			CameraPriority |= CameraPriority.Pinned;
		else
			CameraPriority &= ~CameraPriority.Pinned;

		LastUsed = DateTime.UtcNow;
	}

	public ComponentSolver Solver { get; private set; } = null;

	public List<ScoreMethod> ScoreMethods;
	public List<ScoreMethod> RewardBonusMethods;

	public bool IsMod => BombComponent is ModBombComponent || BombComponent is ModNeedyComponent;

	public bool Hidden => !BombComponent.gameObject.activeInHierarchy || MysteryModuleShim.IsHidden(BombComponent);

	public static bool ClaimsEnabled = true;

	private string _headerText;
	public string HeaderText
	{
		get => _headerText ?? (BombComponent != null ? BombComponent.GetModuleDisplayName() ?? string.Empty : string.Empty);
		set => _headerText = value;
	}

	public Coroutine TakeInProgress;
	public string TakeUser;
	public static List<string> ClaimedList = new List<string>();
	#endregion

	#region Private Fields
	public Color unclaimedBackgroundColor = new Color(0, 0, 0);
	private TwitchModuleData _data;
	private StatusLightPosition _statusLightPosition;
	private Vector3 _originalIDPosition = Vector3.zero;
	private bool _anarchyMode;
	private bool _hidden;
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

		if (_hidden != Hidden)
		{
			_hidden = Hidden;
			CanvasGroupMultiDecker.alpha = Hidden ? 0.0f : 1.0f;

			if (!Hidden)
			{
				GetStatusLightY();
				SetStatusLightPosition(Solver.ModInfo.statusLightPosition);
			}
		}

		if (_originalIDPosition == Vector3.zero) return;
		if (Solver.ModInfo.statusLightPosition != _statusLightPosition)
			SetStatusLightPosition(Solver.ModInfo.statusLightPosition);

		if (Solver.ModInfo.ShouldSerializeunclaimedColor() && unclaimedBackgroundColor != Solver.ModInfo.unclaimedColor)
		{
			unclaimedBackgroundColor = Solver.ModInfo.unclaimedColor;
			if (!Claimed || Solved) SetBannerColor(unclaimedBackgroundColor);
		}

		if (!Solver.ModInfo.ShouldSerializeunclaimedColor() && unclaimedBackgroundColor != TwitchPlaySettings.data.UnclaimedColor && !(TwitchPlaySettings.data.ShowModuleType || TwitchPlaySettings.data.ShowModuleDifficulty))
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

		unclaimedBackgroundColor = TwitchPlaySettings.data.UnclaimedColor;

		try
		{
			Solver = ComponentSolverFactory.CreateSolver(this);
			if (Solver != null)
			{
				var ModInfo = Solver.ModInfo;

				ScoreMethods = ModInfo.GetScoreMethods(this);

				if (ComponentSolverFactory.rewardBonuses.TryGetValue(ModInfo.moduleID, out string scoreString))
					RewardBonusMethods = ModuleInformation.ConvertScoreString(scoreString, this);

				// Set the display name for built in modules
				if (ModInfo.builtIntoTwitchPlays)
				{
					var displayName = BombComponent.GetModuleDisplayName();
					ModuleData.DataHasChanged |= displayName != ModInfo.moduleDisplayName;
					ModInfo.moduleDisplayName = displayName;

					ModuleData.WriteDataToFile();
				}

				if (ModInfo.ShouldSerializeunclaimedColor()) unclaimedBackgroundColor = ModInfo.unclaimedColor;
				else if (TwitchPlaySettings.data.ShowModuleType)
				{
					unclaimedBackgroundColor = Color.HSVToRGB(
						BombComponent.ComponentType.ToString().EndsWith("Mod") ? (ModInfo.announceModule ? 0.18f : 0.6f) : 0.3f,
						ModInfo.announceModule ? 0.84f : 1f,
						ModInfo.unclaimable ? 0 : (ModInfo.announceModule ? 0.92f : 0.637f)
					);
				}

				var bar = _data.bar;
				bar.transform.parent.gameObject.SetActive(TwitchPlaySettings.data.ShowModuleDifficulty && !ModInfo.unclaimable);

				var value = Mathf.InverseLerp(4, 25, GetPoints<BaseScore>());
				if (ScoreMethods.Any(method => method.GetType() == typeof(PerModule)))
				{
					value = Mathf.InverseLerp(0.25f, 4, GetPoints<PerModule>());
				}

				bar.transform.localScale = new Vector3(value, 1, 1);

				Solver.Code = Code;
				// Account for modules whose status lights change on the y-axis
				if (ModInfo.moduleID.EqualsAny("Coinage"))
					StartCoroutine(RepeatStatusLightY(ModInfo));
				else
				{
					GetStatusLightY();
					SetStatusLightPosition(ModInfo.statusLightPosition);
				}

				CanvasGroupUnsupported.gameObject.SetActive(Solver.UnsupportedModule);

				IDTextUnsupported.text = BombComponent is ModBombComponent
					? $"To solve this\nmodule, use\n!{Code} solve"
					: $"To disarm this\nneedy, use\n!{Code} solve";
				if (Solver.UnsupportedModule)
					UnsupportedComponents.Add(this);

				var needyComponent = BombComponent.GetComponent<NeedyComponent>();
				if (needyComponent != null)
				{
					needyComponent.CountdownTime += ModInfo.additionalNeedyTime;
					needyComponent.GetValue<NeedyTimer>("timer").TotalTime += ModInfo.additionalNeedyTime;
				}
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

			if (!TwitchPlaySettings.data.EnableInteractiveMode)
				DebugHelper.Log("An unimplemented module was added to a bomb, solving module.");

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

		StartCoroutine(DelayAnnouncement());

		SetBannerColor(unclaimedBackgroundColor);
		TakeUser = null;
	}

	private IEnumerator DelayAnnouncement()
	{
		yield return null;
		yield return null;

		// Don't announce any module that isn't marked for announcement, needy or hidden
		if (Solver?.ModInfo.announceModule != true && BombComponent.GetComponent<NeedyComponent>() == null && !Hidden)
			yield break;

		yield return new WaitUntil(() => !Hidden);

		IRCConnection.SendMessage($"Module {Code} is {HeaderText}");

		if (TwitchPlaySettings.data.EnableBossAutoViewPin)
		{
			CameraPriority |= CameraPriority.Viewed;
			CameraPriority |= CameraPriority.Pinned;
		}
	}

	private void GetStatusLightY(float offset = 0.03514f)
	{
		Vector3 pos = CanvasGroupMultiDecker.transform.localPosition;
		// This sets the Y position of ID tag to be right above the status light for modules where the status light has been moved.
		// Which is done by getting the status light's position in world space, converting it to the tag's local space, taking the Y and adding 0.03514 (unless otherwise specified).
		StatusLightParent statusLightParent = BombComponent.GetComponentInChildren<StatusLightParent>();
		if (statusLightParent != null)
		{
			float y = CanvasGroupMultiDecker.transform.parent.InverseTransformPoint(statusLightParent.transform.position).y + offset;
			if (y >= 0) // Make sure the Y position wouldn't be inside the module.
			{
				pos.y = y;
			}
		}

		_originalIDPosition = pos;
	}

	private IEnumerator RepeatStatusLightY(ModuleInformation ModInfo)
	{
		while (true)
		{
			switch (ModInfo.moduleID)
			{
				case "Coinage":
					GetStatusLightY(0.0432f);
					break;
				default:
					GetStatusLightY();
					break;
			}
			SetStatusLightPosition(ModInfo.statusLightPosition);
			yield return null;
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

		UnsupportedComponents.Clear();
		return true;
	}

	public void SolveSilently() => Solver.SolveSilently();

	public static void ClearUnsupportedModules() => UnsupportedComponents.Clear();

	public void OnPass(string userNickname)
	{
		CanvasGroupMultiDecker.alpha = TwitchPlaySettings.data.AnarchyMode ? 0.5f : 0.0f;
		if (PlayerName == null)
			PlayerName = userNickname;
		if (TakeInProgress != null)
		{
			StopCoroutine(TakeInProgress);
			TakeInProgress = null;
			TakeUser = null;
		}

		// The camera wall needs to be updated whenever a module is solved.
		ModuleCameras.Instance.UpdateAutomaticCameraWall();
	}

	private void OnDestroy() => StopAllCoroutines();
	#endregion

	#region Public Methods
	public void AddToClaimQueue(string userNickname, bool viewRequested = false, bool viewPinRequested = false)
	{
		if (!ClaimQueue.Any(x => x.UserNickname.Equals(userNickname, StringComparison.InvariantCultureIgnoreCase)))
		{
			ClaimQueue.Add(new ClaimQueueItem(userNickname, viewRequested, viewPinRequested));

			TwitchGame.Instance.StartCoroutine(TwitchGame.Instance.ProcessClaimQueue());
		}
	}

	public void RemoveFromClaimQueue(string userNickname) => ClaimQueue.RemoveAll(x => x.UserNickname.Equals(userNickname, StringComparison.InvariantCultureIgnoreCase));

	public IEnumerator ProcessTakeover()
	{
		if (TakeModuleSound != null)
		{
			TakeModuleSound.time = 0.0f;
			TakeModuleSound.Play();
		}
		yield return new WaitForSecondsRealtime(60.0f);

		// Takeover attempt successful! Just unclaim the module, ProcessClaimQueue() will reassign it
		SetUnclaimed();
	}

	public void ProcessClaimQueue()
	{
		if (Solved || Solver.AttemptedForcedSolve)
		{
			ClaimQueue.Clear();
			return;
		}

		// Module is already claimed
		if (PlayerName != null)
			return;

		// Give priority to a player trying to take over the module
		if (TakeUser != null && TryClaim(TakeUser) is ClaimResult result && result.Claimed)
		{
			if (TakeInProgress != null)
			{
				StopCoroutine(TakeInProgress);
				TakeInProgress = null;
			}
			TakeUser = null;
			IRCConnection.SendMessage(result.Message);
			return;
		}

		// Check if the claim queue contains a suitable player
		for (int i = 0; i < ClaimQueue.Count; i++)
		{
			var item = ClaimQueue[i];
			if (TryClaim(item.UserNickname, item.ViewRequested, item.ViewPinRequested) is ClaimResult claimResult && claimResult.Claimed)
			{
				if (item.ViewRequested)
					ViewPin(item.UserNickname, item.ViewPinRequested);
				ClaimQueue.RemoveAt(i);
				IRCConnection.SendMessage(claimResult.Message);
				break;
			}
		}
	}

	public sealed class ClaimResult
	{
		public bool Claimed { get; }
		public string Message { get; }
		public ClaimResult(bool claimed, string message)
		{
			Claimed = claimed;
			Message = message;
		}
	}

	public ClaimResult TryClaim(string userNickName, bool viewRequested = false, bool viewPinRequested = false)
	{
		if (Votes.Active && Votes.CurrentVoteType == VoteTypes.Solve && Votes.voteModule == this)
			return new ClaimResult(false, $"@{userNickName}, module {Code} ({HeaderText}) is being votesolved.");

		if (Solver.AttemptedForcedSolve)
			return new ClaimResult(false, $"@{userNickName}, module {Code} ({HeaderText}) is being solved automatically.");

		if (TwitchPlaySettings.data.AnarchyMode)
			return new ClaimResult(false, $"@{userNickName}, claiming modules is not allowed in anarchy mode.");

		if (!ClaimsEnabled && !UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
			return new ClaimResult(false, $"@{userNickName}, claims have been disabled.");

		if (Solved)
			return new ClaimResult(false, $"@{userNickName}, module {Code} ({HeaderText}) is already solved.");

		if (!CanBeClaimed)
			return new ClaimResult(false, $"@{userNickName}, module {Code} ({HeaderText}) cannot be claimed.");

		// Already claimed by the same user
		if (userNickName.Equals(PlayerName))
			return new ClaimResult(false, string.Format(TwitchPlaySettings.data.ModuleAlreadyOwned, userNickName, Code, HeaderText));

		// Claimed by someone else ⇒ queue
		if (PlayerName != null)
		{
			AddToClaimQueue(userNickName, viewRequested, viewPinRequested);
			return new ClaimResult(false, string.Format(TwitchPlaySettings.data.AlreadyClaimed, Code, PlayerName, userNickName, HeaderText));
		}

		// Would violate the claim limit ⇒ queue
		if (TwitchGame.Instance.Modules.Count(md => md.PlayerName != null && md.PlayerName.EqualsIgnoreCase(userNickName) && !md.Solved) >= TwitchPlaySettings.data.ModuleClaimLimit
			&& (TwitchGame.Instance.Modules.Any(module => module.PlayerName != null && module.PlayerName.EqualsIgnoreCase(userNickName) && !module.Solved && TwitchGame.Instance.CommandQueue.All(item => !item.Message.Text.StartsWith($"!{module.Code} ")) && !module.Solver.ModInfo.announceModule && !(module.BombComponent is ModNeedyComponent)) || !TwitchPlaySettings.data.QueuedClaimOverride)
			&& (!UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true) || !TwitchPlaySettings.data.SuperStreamerIgnoreClaimLimit))
		{
			AddToClaimQueue(userNickName, viewRequested, viewPinRequested);
			return new ClaimResult(false, string.Format(TwitchPlaySettings.data.QueuedClaimOverride ? TwitchPlaySettings.data.TooManyClaimedOverride : TwitchPlaySettings.data.TooManyClaimed,
				userNickName, TwitchPlaySettings.data.ModuleClaimLimit));
		}

		// Check the claim cooldown at the start of the bomb
		if (TwitchGame.Instance.ClaimCooldown)
		{
			var lastClaimedTime = TwitchGame.Instance.GetLastClaimedTime(Solver.ModInfo.moduleID, userNickName);
			if (lastClaimedTime != null && DateTime.UtcNow.TotalSeconds() < TwitchPlaySettings.data.InstantModuleClaimCooldownExpiry + lastClaimedTime.Value)
			{
				AddToClaimQueue(userNickName, viewRequested, viewPinRequested);
				return new ClaimResult(false, string.Format(TwitchPlaySettings.data.ClaimCooldown, Code, TwitchPlaySettings.data.InstantModuleClaimCooldown, userNickName, HeaderText));
			}
		}

		// We are actually claiming the module!
		SetClaimedBy(userNickName);
		if (viewRequested)
			ViewPin(userNickName, viewPinRequested);
		return new ClaimResult(true, string.Format(TwitchPlaySettings.data.ModuleClaimed, Code, userNickName, HeaderText));
	}

	public void SetClaimedBy(string userNickName)
	{
		TwitchGame.Instance.SetLastClaimedTime(Solver.ModInfo.moduleID, userNickName, DateTime.UtcNow.TotalSeconds());
		SetBannerColor(ClaimedBackgroundColour);
		PlayerName = userNickName;
		CameraPriority |= CameraPriority.Claimed;
	}

	public void SetUnclaimed()
	{
		if (PlayerName == null)
			return;
		IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.ModuleUnclaimed, Code, PlayerName, HeaderText));
		RemoveFromClaimQueue(PlayerName);
		SetBannerColor(unclaimedBackgroundColor);
		PlayerName = null;
		CameraPriority &= ~CameraPriority.Claimed;
	}

	public void CommandError(string userNickName, string message) => IRCConnection.SendMessageFormat(TwitchPlaySettings.data.CommandError, userNickName, Code, HeaderText, message);

	public void CommandInvalid(string userNickName) => IRCConnection.SendMessageFormat(TwitchPlaySettings.data.InvalidCommand, userNickName, Code, HeaderText);

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

				// Sets the camera layers of the cameras in Backdoor Hacking to the proper new TP layer to prevent blackscreening
				Camera cam = kvp.Key.gameObject.GetComponent<Camera>();
				if (BombComponent.GetModuleDisplayName() == "Backdoor Hacking" && cam != null)
					cam.cullingMask = (1 << setToLayer) | (1 << 31);
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

	public float GetPoints<T>() where T : ScoreMethod
	{
		foreach (var method in ScoreMethods)
		{
			if (method.GetType() == typeof(T))
				return method.Points;
		}

		return 0;
	}

	public void SetClaimedUserMultidecker(string playerName)
	{
		Image claimedDisplay = ClaimedUserMultiDecker;
		if (playerName != null) claimedDisplay.transform.Find("Username").GetComponent<Text>().text = playerName;
		claimedDisplay.gameObject.SetActive(playerName != null);

		// The camera wall needs to be updated whenever a module's claim changes.
		ModuleCameras.Instance.UpdateAutomaticCameraWall();
	}
	#endregion

	#region Private Methods
	public void SetBannerColor(Color color)
	{
		CanvasGroupMultiDecker.GetComponent<Image>().color = color;
		ClaimedUserMultiDecker.color = color;
	}

	private void SetStatusLightPosition(StatusLightPosition newPos)
	{
		_statusLightPosition = newPos;

		if (newPos == StatusLightPosition.Default)
		{
			StatusLightParent statusLightParent = BombComponent.GetComponentInChildren<StatusLightParent>();
			if (statusLightParent != null)
			{
				Vector3 position = BombComponent.transform.InverseTransformPoint(statusLightParent.transform.position);
				bool left = Math.Round(position.x, 5) < 0;
				bool down = Math.Round(position.z, 5) < 0;

				newPos = left && down ? StatusLightPosition.BottomLeft :
					left ? StatusLightPosition.TopLeft :
					down ? StatusLightPosition.BottomRight :
					StatusLightPosition.TopRight;
			}
			// Else, it'll be left at "Default", which will behave the same as "TopRight".
		}

		RectTransform rectTransform = ClaimedUserMultiDecker.rectTransform;

		switch (newPos)
		{
			case StatusLightPosition.Center:
				CanvasGroupMultiDecker.transform.localPosition = new Vector3(0, _originalIDPosition.y, 0);
				rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(0.5f, 1);
				rectTransform.pivot = new Vector2(0.5f, 0);
				break;
			case StatusLightPosition.TopLeft:
				CanvasGroupMultiDecker.transform.localPosition = new Vector3(-_originalIDPosition.x, _originalIDPosition.y, _originalIDPosition.z);
				rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(1, 1);
				rectTransform.pivot = new Vector2(0, 1);
				break;
			case StatusLightPosition.BottomRight:
				CanvasGroupMultiDecker.transform.localPosition = new Vector3(_originalIDPosition.x, _originalIDPosition.y, -_originalIDPosition.z);
				rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(0, 0);
				rectTransform.pivot = new Vector2(1, 0);
				break;
			case StatusLightPosition.BottomLeft:
				CanvasGroupMultiDecker.transform.localPosition = new Vector3(-_originalIDPosition.x, _originalIDPosition.y, -_originalIDPosition.z);
				rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(1, 0);
				rectTransform.pivot = new Vector2(0, 0);
				break;
			default:
				CanvasGroupMultiDecker.transform.localPosition = new Vector3(_originalIDPosition.x, _originalIDPosition.y, _originalIDPosition.z);
				rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(0, 1);
				rectTransform.pivot = new Vector2(1, 1);
				break;
		}
	}
	#endregion

	#region Properties
	private string _playerName;
	public string PlayerName
	{
		set
		{
			_playerName = value;
			SetClaimedUserMultidecker(value);
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
