using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ModuleCameras : MonoBehaviour
{
	/// <summary>
	///     Camera layer used on all modules/cameras when interactive mode is enabled.</summary>
	/// <remarks>
	///     This needs to be 11 because that’s what KTANE expects for interactables.</remarks>
	public const int DefaultCameraLayer = 11;

	public class ModuleCamera : MonoBehaviour
	{
		public TwitchModule Module;
		public TwitchModule PreviousModule;
		public Camera CameraInstance;
		public bool ZoomActive;

		/// <summary>Camera layer used when interactive mode is NOT enabled.</summary>
		public int CameraLayer;

		public bool LastInteractiveState;
		public bool EscapePressed;

		private static readonly Rect ZoomCameraLocation = new Rect(0.2738095f, 0.12f, 0.452381f, 0.76f);
		public Rect OriginalCameraRect;

		public static ModuleCamera CreateModuleCamera(ModuleCameras parentInstance, int cameraIx)
		{
			ModuleCamera moduleCamera = new GameObject().AddComponent<ModuleCamera>();
			moduleCamera.transform.parent = parentInstance.transform;

			Camera camera = Instantiate(parentInstance.CameraPrefab);
			camera.rect = parentInstance._cameraLocations[cameraIx];
			camera.aspect = 1f;
			camera.depth = 99;

			moduleCamera.CameraInstance = camera;
			moduleCamera.OriginalCameraRect = camera.rect;
			moduleCamera.CameraLayer = CameraLayers[cameraIx];
			return moduleCamera;
		}

		public IEnumerator ZoomCamera(float duration = 1.0f, SuperZoomData zoomData = default)
		{
			CameraInstance.depth = 100;
			ZoomActive = true;
			yield return null;
			foreach (float lerp in duration.TimedAnimation())
			{
				CameraInstance.rect = new Rect(Mathf.Lerp(OriginalCameraRect.x, ZoomCameraLocation.x, lerp),
					Mathf.Lerp(OriginalCameraRect.y, ZoomCameraLocation.y, lerp),
					Mathf.Lerp(OriginalCameraRect.width, ZoomCameraLocation.width, lerp),
					Mathf.Lerp(OriginalCameraRect.height, ZoomCameraLocation.height, lerp));

				CameraInstance.fieldOfView = Mathf.Lerp(5, 5 / zoomData.factor, lerp);
				CameraInstance.transform.localPosition = Vector3.Lerp(new Vector3(0.001f, 2.25f, 0), new Vector3(0.001f + (zoomData.x - 0.5f) * 0.2f, 2.25f, (zoomData.y - 0.5f) * 0.2f), lerp);

				yield return null;
			}
		}

		public IEnumerator UnzoomCamera(float duration = 1.0f, SuperZoomData zoomData = default)
		{
			yield return null;
			foreach (float lerp in duration.TimedAnimation())
			{
				CameraInstance.rect = new Rect(Mathf.Lerp(ZoomCameraLocation.x, OriginalCameraRect.x, lerp),
					Mathf.Lerp(ZoomCameraLocation.y, OriginalCameraRect.y, lerp),
					Mathf.Lerp(ZoomCameraLocation.width, OriginalCameraRect.width, lerp),
					Mathf.Lerp(ZoomCameraLocation.height, OriginalCameraRect.height, lerp));

				CameraInstance.fieldOfView = Mathf.Lerp(5, 5 / zoomData.factor, 1 - lerp);
				CameraInstance.transform.localPosition = Vector3.Lerp(new Vector3(0.001f, 2.25f, 0), new Vector3(0.001f + (zoomData.x - 0.5f) * 0.2f, 2.25f, (zoomData.y - 0.5f) * 0.2f), 1 - lerp);

				yield return null;
			}
			CameraInstance.depth = 99;
			ZoomActive = false;

			if (PreviousModule != null)
			{
				// Return the camera back to the component that WAS there unless that module is now solved.
				ViewModule(PreviousModule.Solved ? Instance.PreferredToView : PreviousModule);
				PreviousModule = null;
			}
		}

		public void ViewModule(TwitchModule module)
		{
			Deactivate();
			if (module == null || module.Hidden)
				return;
			Module = module;

			Transform t = Module.BombComponent.transform.Find("TwitchPlayModuleCamera");
			if (t == null)
			{
				t = new GameObject().transform;
				t.name = "TwitchPlayModuleCamera";
				t.SetParent(Module.BombComponent.transform, false);
			}
			CameraInstance.transform.SetParent(t, false);
			CameraInstance.gameObject.SetActive(true);

			Vector3 lossyScale = CameraInstance.transform.lossyScale;
			CameraInstance.nearClipPlane = 1.0f * lossyScale.y;
			CameraInstance.farClipPlane = 3.0f * lossyScale.y;

			// Light sources in modules don’t show in the camera if we change the layer immediately.
			// Delaying that by 1 frame by using a coroutine seems to fix that.
			StartCoroutine(SetModuleLayer());
		}

		private IEnumerator SetModuleLayer()
		{
			yield return null;
			// LastInteractiveState doesn't get updated until LateUpdate, so it can only be accurately used after that runs.
			int layer = LastInteractiveState ? DefaultCameraLayer : CameraLayer;

			CameraInstance.cullingMask = (1 << layer) | (1 << 31);
			Module.SetRenderLayer(layer);
		}

		private void LateUpdate()
		{
			if (Input.GetKey(KeyCode.Escape))
				EscapePressed = true;

			if (Module != null) Module.UpdateLayerData();

			bool interactiveState = TwitchPlaySettings.data.EnableInteractiveMode;
			interactiveState |= IRCConnection.Instance.State != IRCConnectionState.Connected;
			interactiveState |= EscapePressed;
			interactiveState &= !(GameRoom.Instance is ElevatorGameRoom);

			if (LastInteractiveState != interactiveState)
			{
				LastInteractiveState = interactiveState;
				if (Module != null)
				{
					int layer = interactiveState ? DefaultCameraLayer : CameraLayer;
					CameraInstance.cullingMask = (1 << layer) | (1 << 31);
					Module.SetRenderLayer(layer);
				}
			}
		}

		public void Deactivate()
		{
			if (Module != null) Module.SetRenderLayer(null);
			if (CameraInstance != null)
			{
				CameraInstance.gameObject.SetActive(false);
				if (CameraInstance.transform != null)
					CameraInstance.transform.SetParent(transform, false);
			}

			Module = null;
		}

		// Swap the properties of a camera to another one.
		// Used so that a zoom can continue if the camera wall gets disabled interupting the sequence.
		public void SwapTo(ModuleCamera otherCamera)
		{
			// Copy over the properties that get changed while doing a zoom
			var otherInstance = otherCamera.CameraInstance;
			CameraInstance.rect = otherInstance.rect;

			CameraInstance.fieldOfView = otherInstance.fieldOfView;
			CameraInstance.transform.localPosition = otherInstance.transform.localPosition;

			// Copy everything else over
			otherCamera.PreviousModule = Module;
			otherCamera.CameraInstance = CameraInstance;
			otherCamera.ViewModule(otherCamera.Module);
			CameraInstance = otherInstance;

			otherCamera.CameraLayer = CameraLayer;

			otherCamera.OriginalCameraRect = OriginalCameraRect;
		}
	}

	#region Public Fields
	public Text HeaderPrefab => _data.HeaderPrefab;
	public Text TimerPrefab => _data.TimerPrefab;
	public Text TimerShadowPrefab => _data.TimerShadowPrefab;
	public Text StrikesPrefab => _data.StrikesPrefab;
	public Text SolvesPrefab => _data.SolvesPrefab;
	public Text NeediesPrefab => _data.NeediesPrefab;
	public Text ConfidencePrefab => _data.ConfidencePrefab;
	public Camera CameraPrefab => _data.CameraPrefab;
	public RectTransform BombStatus => _data.BombStatus;
	public Text[] NotesTexts;
	public Image[] NotesTextBackgrounds;
	public Text[] NotesTextIDs;
	public Image[] NotesTextIDsBackgrounds;

	[HideInInspector]
	public bool CameraWallEnabled;

	private Mode _cameraWallMode;
	public Mode CameraWallMode
	{
		get => _cameraWallMode;
		set
		{
			_cameraWallMode = value;

			switch (_cameraWallMode)
			{
				case Mode.Enabled:
					EnableCameraWall();
					break;
				case Mode.Disabled:
					DisableCameraWall();
					break;
				case Mode.Automatic:
					UpdateAutomaticCameraWall();
					break;
			}
		}
	}

	public enum Mode
	{
		Enabled,
		Disabled,
		Automatic
	}

	public static ModuleCameras Instance;
	#endregion

	#region Private Fields
	private ModuleCamerasData _data;
	private readonly List<ModuleCamera> _moduleCameras = new List<ModuleCamera>();
	private TwitchBomb _currentBomb;

	private int _currentSolves;
	private int _currentStrikes;
	private int _currentTotalModules;
	private int _currentTotalStrikes;

	private readonly Rect[] _cameraLocations =
	{
		new Rect(0.8333333f, 0.56f, 0.1666667f, 0.28f),
		new Rect(0.8333333f, 0.28f, 0.1666667f, 0.28f),
		new Rect(0.8333333f, 0.00f, 0.1666667f, 0.28f),

		new Rect(0.0000000f, 0.00f, 0.1666667f, 0.28f),
		new Rect(0.0000000f, 0.28f, 0.1666667f, 0.28f),
		new Rect(0.0000000f, 0.56f, 0.1666667f, 0.28f),

		// Camera wall
		new Rect(0.1666667f, 0.643f, 0.1666667f, 0.28f),
		new Rect(0.3333333f, 0.643f, 0.1666667f, 0.28f),
		new Rect(0.5000000f, 0.643f, 0.1666667f, 0.28f),
		new Rect(0.6666667f, 0.643f, 0.1666667f, 0.28f),

		new Rect(0.1666667f, 0.363f, 0.1666667f, 0.28f),
		new Rect(0.3333333f, 0.363f, 0.1666667f, 0.28f),
		new Rect(0.5000000f, 0.363f, 0.1666667f, 0.28f),
		new Rect(0.6666667f, 0.363f, 0.1666667f, 0.28f),

		new Rect(0.1666667f, 0.083f, 0.1666667f, 0.28f),
		new Rect(0.3333333f, 0.083f, 0.1666667f, 0.28f),
		new Rect(0.5000000f, 0.083f, 0.1666667f, 0.28f),
		new Rect(0.6666667f, 0.083f, 0.1666667f, 0.28f)
	};

	//private float currentSuccess;
	#endregion

	#region Private Static Readonlys
	private static readonly Vector3 HudScale = new Vector3(0.7f, Mathf.Round(1), Mathf.Round(1));
	#endregion

	#region Unity Lifecycle
	// These are the layers used by the 18 module cameras.
	// Layer 11 is the interactive layer (highlightables).
	// Layer 13 is used by KTANE for mouse rendering and 14–15 for VR.
	private static readonly int[] CameraLayers = { 8, 9, 10, 12, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };

	private static readonly Dictionary<TwitchPlaysMode, Color> ModeColors = new Dictionary<TwitchPlaysMode, Color>
	{
		{ TwitchPlaysMode.Normal, Color.red },
		{ TwitchPlaysMode.Time, new Color(1.0f, 0.5f, 0.0f) },
		{ TwitchPlaysMode.VS, Color.green },
		{ TwitchPlaysMode.Zen, Color.cyan },
		{ TwitchPlaysMode.Training, Color.gray }
	};

	private void Awake()
	{
		_data = GetComponent<ModuleCamerasData>();

		NotesTexts = _data.NotesTexts;
		NotesTextBackgrounds = _data.NotesTextBackgrounds;
		NotesTextIDs = _data.NotesTextIDs;
		NotesTextIDsBackgrounds = _data.NotesTextIDsBackgrounds;
	}

	private void Start()
	{
		Instance = this;

		_cameraWallMode = TwitchPlaySettings.data.EnableAutomaticCameraWall ? Mode.Automatic : Mode.Disabled;

		// Create the first 6 module cameras (more will be created if the camera wall gets enabled)
		for (int i = 0; i < 6; i++)
			_moduleCameras.Add(ModuleCamera.CreateModuleCamera(this, i));

		// Change timer/strike colors according to current game mode (normal mode, time mode, Zen mode, etc.)
		TimerComponent timer = _currentBomb.Bomb.GetTimer();
		Color modeColor = ModeColors[OtherModes.currentMode];
		TimerPrefab.color = modeColor;
		timer.text.color = modeColor;
		timer.StrikeIndicator.RedColour = modeColor;
	}

	private void LateUpdate()
	{
		if (_currentBomb == null) return;
		string formattedTime = _currentBomb.GetFullFormattedTime;
		TimerPrefab.text = formattedTime;
		TimerShadowPrefab.text = Regex.Replace(formattedTime, @"\d", "8");
		UpdateConfidence();
	}
	#endregion

	#region Public Methods
	public IEnumerator ZoomCamera(TwitchModule component, SuperZoomData zoomData, float delay)
	{
		if (component.CameraPriority == CameraPriority.Unviewed)
			component.CameraPriority = CameraPriority.Interacted;

		int existingCamera = CurrentModulesContains(component);
		if (existingCamera == -1) existingCamera = BorrowCameraForZoom(component);
		if (existingCamera > -1)
		{
			ModuleCamera cam = _moduleCameras[existingCamera];
			return cam.ZoomCamera(delay, zoomData);
		}
		return null;
	}

	public IEnumerator UnzoomCamera(TwitchModule component, SuperZoomData zoomData, float delay)
	{
		int existingCamera = CurrentModulesContains(component);
		if (existingCamera == -1) existingCamera = BorrowCameraForZoom(component);
		if (existingCamera > -1)
		{
			ModuleCamera cam = _moduleCameras[existingCamera];
			return cam.UnzoomCamera(delay, zoomData);
		}
		return null;
	}

	public bool TryViewModule(TwitchModule module)
	{
		if (module == null || module.Hidden)
			return false;

		module.LastUsed = DateTime.UtcNow;

		// Is the module already viewed?
		int existingCamera = CurrentModulesContains(module);
		if (existingCamera > -1)
			return false;

		// Find a camera
		var camera = AvailableCamera(module.CameraPriority);
		if (camera == null)
			return false;

		// If we can replace a LOWER-priority camera (or an unused slot), do so before enabling the camera wall.
		if (camera.Module == null || camera.Module.CameraPriority < module.CameraPriority)
			camera.ViewModule(module);
		// If we can and should enable the camera wall, enable it and then view the module.
		else if (UpdateAutomaticCameraWall())
			TryViewModule(module);
		// If the camera is already enabled, replace a suitable SAME-priority camera.
		else
			camera.ViewModule(module);
		return true;
	}

	private ModuleCamera AvailableCamera(CameraPriority maxPriority) => _moduleCameras
				.Where(c => c.Module == null || (c.Module.CameraPriority <= maxPriority && !c.ZoomActive))
				.OrderBy(c => c.Module != null)
				.ThenBy(c => c.Module?.Solved ?? false)
				.ThenBy(c => c.Module != null ? (CameraPriority?) c.Module.CameraPriority : null)
				.ThenBy(c => c.Module != null ? (DateTime?) c.Module.LastUsed : null)
				.FirstOrDefault();

	public void SetNotes()
	{
		float hue = TwitchPlaySettings.data.EnableWhiteList ? .1f : .72f;
		for (int ix = 0; ix < 4; ix++)
		{
			if (Votes.Active && ix == 2)
			{
				NotesTexts[ix].text = $"{Votes.PossibleVotes[Votes.CurrentVoteType].name}: {Votes.NumVoters} vote{(Votes.NumVoters == 1 ? "" : "s")}\n{Votes.TimeLeft} second{(Votes.TimeLeft == 1 ? "" : "s")} left.";
				if (!TwitchPlaySettings.data.DarkMode) NotesTextBackgrounds[ix].color = Color.HSVToRGB(0f, .246f, .93f);
				else NotesTextBackgrounds[ix].color = new Color32(0xE9, 0x19, 0x16, 0xFF);
				NotesTextIDs[ix].text = "!vote";
			}
			else if (OtherModes.TrainingModeOn && TwitchGame.Instance.TrainingModeRemainingTime > 0 && ix == 2)
			{
				string _plural = TwitchGame.Instance.TrainingModeRemainingTime == 1 ? "" : "s";
				NotesTexts[ix].text = $"Training Mode Auto-Detonation is enabled. This bomb will be detonated in {TwitchGame.Instance.TrainingModeRemainingTime} minute{_plural}.";
				if (!TwitchPlaySettings.data.DarkMode) NotesTextBackgrounds[ix].color = Color.HSVToRGB(0.38f, .246f, .93f);
				else NotesTextBackgrounds[ix].color = new Color32(0x00, 0xAD, 0x03, 0xFF);
				NotesTextIDs[ix].text = "";
			}
			else if ((TwitchGame.Instance.CommandQueue.Count > 0 || TwitchGame.Instance.QueueEnabled) && ix == 3)
			{
				var numNameless = TwitchGame.Instance.CommandQueue.Count(c => c.Name == null);
				var numNamed = TwitchGame.Instance.CommandQueue.Count - numNameless;
				NotesTexts[ix].text = "QUEUE: " +
					(TwitchGame.Instance.CommandQueue.Count == 0 ? "ACTIVE" : "") +
					(numNameless > 0 ? $"{numNameless} item{(numNameless == 1 ? "" : "s")}" : "") +
					(numNameless > 0 && numNamed > 0 ? " + " : "") +
					TwitchGame.Instance.CommandQueue.Where(c => c.Name != null).Select(c => $"“{c.Name}”").Join(", ");
				if (!TwitchPlaySettings.data.DarkMode) NotesTextBackgrounds[ix].color = Color.HSVToRGB(hue, .246f, .93f);
				else NotesTextBackgrounds[ix].color = TwitchPlaySettings.data.EnableWhiteList ? new Color32(0x64, 0x5E, 0x14, 0xFF) : new Color32(0x3A, 0x3A, 0x3D, 0xFF);
				NotesTextIDs[ix].text = "!q";
			}
			else
			{
				NotesTexts[ix].text = TwitchGame.Instance.NotesDictionary.TryGetValue(ix, out var text) ? text : (OtherModes.TrainingModeOn && ix == 3) ? TwitchPlaySettings.data.TrainingModeFreeSpace : TwitchPlaySettings.data.NotesSpaceFree;
				NotesTextBackgrounds[ix].color = !TwitchPlaySettings.data.DarkMode ? new Color32(0xEE, 0xEE, 0xEE, 0xFF) : new Color32(0x0E, 0x0E, 0x10, 0xFF);
				NotesTextIDs[ix].text = $"!notes{ix + 1}";
			}
			NotesTextIDsBackgrounds[ix].color = Color.HSVToRGB(hue, .6f, .62f);

			NotesTexts[ix].color = TwitchPlaySettings.data.DarkMode ? new Color32(0xEF, 0xEF, 0xEC, 0xFF) : new Color32(0x4F, 0x4F, 0x4F, 0xFF);
		}
	}

	public void UnviewModule(TwitchModule handle)
	{
		handle.CameraPriority = handle.Solved ? CameraPriority.Unviewed : handle.Claimed ? CameraPriority.Claimed : CameraPriority.Unviewed;
		TwitchPlaysService.Instance.StartCoroutine(UnviewModuleCoroutine(handle));
	}

	public void Hide() => SetCameraVisibility(false);

	public void Show() => SetCameraVisibility(true);

	public void HideHud() => BombStatus.localScale = Vector3.zero;

	public void ShowHud() => BombStatus.localScale = HudScale;

	public void UpdateHeader() => HeaderPrefab.text = _currentBomb.BombName;

	public void UpdateStrikes(bool delay = false) => StartCoroutine(UpdateStrikesCoroutine(delay));

	public void UpdateSolves()
	{
		if (_currentBomb == null) return;
		_currentSolves = _currentBomb.BombSolvedModules;
		_currentTotalModules = _currentBomb.BombSolvableModules;
		string solves = _currentSolves.ToString().PadLeft(_currentTotalModules.ToString().Length, char.Parse("0"));
		DebugHelper.Log("Updating solves to " + solves);
		SolvesPrefab.text = $"{solves}<size=25>/{_currentTotalModules}</size>";

		if (TwitchGame.Instance.Bombs.Sum(bomb => bomb.BombSolvableModules - bomb.BombSolvedModules) != 1 || _moduleCameras.Count == 0)
			return;

		var module = TwitchGame.Instance.Modules.First(m => !m.Solved && m.BombComponent.IsSolvable);
		TwitchPlaysService.Instance.CoroutineQueue.AddToQueue(ModuleCommands.Show(module, ZoomCamera(module, new SuperZoomData(1, 0.5f, 0.5f), 1)));
	}

	public void UpdateConfidence()
	{
		if (OtherModes.TimeModeOn)
		{
			float timedMultiplier = OtherModes.GetAdjustedMultiplier();
			ConfidencePrefab.color = Color.yellow;
			string conf = "x" + $"{timedMultiplier:0.0}";
			string pts = "+" + $"{TwitchPlaySettings.GetRewardBonus():0}";
			ConfidencePrefab.text = pts;
			StrikesPrefab.color = Color.yellow;
			StrikesPrefab.text = conf;
		}
		else if (OtherModes.Unexplodable)
		{
			ConfidencePrefab.color = Color.yellow;
			string pts = "+" + $"{TwitchPlaySettings.GetRewardBonus():0}";
			ConfidencePrefab.text = pts;
			StrikesPrefab.color = Color.red;
			if (_currentBomb != null)
				StrikesPrefab.text = _currentBomb.StrikeCount.ToString();
		}
		else if (OtherModes.VSModeOn)
		{
			int bossHealth = OtherModes.GetEvilHealth();
			int teamHealth = OtherModes.GetGoodHealth();
			StrikesPrefab.color = Color.cyan;
			ConfidencePrefab.color = Color.red;
			StrikesPrefab.text = $"{teamHealth} HP";
			ConfidencePrefab.text = $"{bossHealth} HP";
		}
		else
		{
			ConfidencePrefab.color = Color.yellow;
			string pts = $"+{TwitchPlaySettings.GetRewardBonus():0}";
			ConfidencePrefab.text = pts;
		}

		ConfidencePrefab.enabled = OtherModes.ScoreMultiplier != 0;
	}

	public void EnableCameraWall()
	{
		if (CameraWallEnabled)
		{
			DebugHelper.Log("Camera wall already enabled");
			return;
		}
		DebugHelper.Log("Enabling camera wall");
		CameraWallEnabled = true;
		GameRoom.HideCamera();

		for (int i = 6; i < _cameraLocations.Length; i++)
			_moduleCameras.Add(ModuleCamera.CreateModuleCamera(this, i));
		while (HasEmptySlot)
		{
			var preferredToView = PreferredToView;
			if (!TryViewModule(preferredToView))
				break;
		}

		DebugHelper.Log("Camera wall enabled");
	}

	public void DisableCameraWall()
	{
		if (!CameraWallEnabled)
		{
			DebugHelper.Log("Camera wall already disabled");
			return;
		}
		DebugHelper.Log("Disabling camera wall");
		CameraWallEnabled = false;
		GameRoom.ShowCamera();

		while (_moduleCameras.Count > 6)
		{
			ModuleCamera camera = _moduleCameras[6];

			// If the camera is zooming, we need to swap the zoom onto another camera.
			if (camera.ZoomActive)
			{
				var targetCamera = _moduleCameras[5];
				targetCamera.SwapTo(camera);

				_moduleCameras[5] = camera;
				_moduleCameras[6] = targetCamera;
				camera = targetCamera;
			}

			_moduleCameras.RemoveAt(6);

			camera.Deactivate();
			Destroy(camera.gameObject);
		}
		for (int i = 0; i < 6; i++)
			TryViewModule(PreferredToView);

		DebugHelper.Log("Camera wall disabled");
	}

	/// <summary>Updates the state of the camera wall.
	/// 7 or more claimed modules and input is disabled, enables the camera wall.
	/// 4 or fewer claimed modules, or 6 or fewer unsolved modules in total, disables the camera wall.</summary>
	/// <returns>Whether or not the camera wall just got enabled because of the update.</returns>
	public bool UpdateAutomaticCameraWall()
	{
		if (!AutomaticCameraWallEnabled)
			return false;

		if (!CameraWallEnabled && TwitchGame.Instance.Modules.Count(twitchModule => twitchModule.Claimed && !twitchModule.Solved) >= 7 && !InputInterceptor.IsInputEnabled)
		{
			EnableCameraWall();
			return true;
		}
		else if (CameraWallEnabled && (TwitchGame.Instance.Modules.Count(twitchModule => twitchModule.Claimed && !twitchModule.Solved) <= 4 || TwitchGame.Instance.Modules.Count(twitchModule => !twitchModule.Solved) <= 6))
		{
			DisableCameraWall();
			return false;
		}

		return false;
	}

	public void ChangeBomb(TwitchBomb bomb)
	{
		DebugHelper.Log("Switching bomb");
		if (_currentBomb != null) _currentBomb.CanvasGroup.alpha = 0;
		if (bomb != null) bomb.CanvasGroup.alpha = 1;

		_currentBomb = bomb;
		UpdateHeader();
		UpdateStrikes();
		UpdateSolves();
		UpdateConfidence();

		int needies = _currentBomb.Bomb.BombComponents.Count(bombComponent => bombComponent.GetComponent<NeedyComponent>() != null);
		NeediesPrefab.gameObject.SetActive(needies > 0);
		NeediesPrefab.text = needies.ToString();

		if (TwitchPlaySettings.data.EnableEdgeworkCameras)
			SetupEdgeworkCameras();
	}

	readonly List<GameObject> edgeworkCameras = new List<GameObject>();
	public void SetupEdgeworkCameras()
	{
		foreach (GameObject edgeworkCamera in edgeworkCameras)
			Destroy(edgeworkCamera);

		// Create widget cameras
		var widgets = _currentBomb.Bomb.WidgetManager.GetWidgets();
		var widgetTypes = new[] { "BatteryWidget", "IndicatorWidget", "EncryptedIndicator", "NumberInd", "PortWidget", "DayTimeWidget", "TwoFactorWidget", "MultipleWidgets", null, "SerialNumber", "RuleSeedWidget" };
		widgets.Sort((w1, w2) =>
		{
			var covered1 = w1.transform.Find("Cover(Clone)") != null;
			var covered2 = w2.transform.Find("Cover(Clone)") != null;
			if (covered1 && !covered2)
				return -1;
			else if (!covered1 && covered2)
				return 1;

			var i1 = widgetTypes.IndexOf(wt => wt != null && w1.GetComponent(wt) != null);
			if (i1 == -1)
				i1 = Array.IndexOf(widgetTypes, null);
			var i2 = widgetTypes.IndexOf(wt => wt != null && w2.GetComponent(wt) != null);
			if (i2 == -1)
				i2 = Array.IndexOf(widgetTypes, null);

			if (i1 < i2)
				return -1;
			if (i1 > i2)
				return 1;

			switch (w1)
			{
				case BatteryWidget batteries:
					return batteries.GetNumberOfBatteries().CompareTo(((BatteryWidget) w2).GetNumberOfBatteries());

				case IndicatorWidget indicator:
					return indicator.Label.CompareTo(((IndicatorWidget) w2).Label);

				case PortWidget port:
					var port2 = (PortWidget) w2;
					return (
						port.IsPortPresent(PortWidget.PortType.Parallel) || port.IsPortPresent(PortWidget.PortType.Serial) ? 0 :
						port.IsPortPresent(PortWidget.PortType.DVI) || port.IsPortPresent(PortWidget.PortType.PS2) || port.IsPortPresent(PortWidget.PortType.RJ45) || port.IsPortPresent(PortWidget.PortType.StereoRCA) ? 1 : 2
					).CompareTo(
						port2.IsPortPresent(PortWidget.PortType.Parallel) || port2.IsPortPresent(PortWidget.PortType.Serial) ? 0 :
						port2.IsPortPresent(PortWidget.PortType.DVI) || port2.IsPortPresent(PortWidget.PortType.PS2) || port2.IsPortPresent(PortWidget.PortType.RJ45) || port2.IsPortPresent(PortWidget.PortType.StereoRCA) ? 1 : 2
					);
				default: return w1.name.CompareTo(w2.name);
			}
		});

		const float availableWidth = 0.6666667f;
		const float availableHeight = .08f;

		// Find out how tall the widgets would be if we arrange them in one row
		var widgetWidths = widgets.Select(w => (float) w.SizeX / w.SizeZ * Screen.height / Screen.width).ToArray();
		var totalWidth = widgetWidths.Sum();
		var widgetMiddles = new float[widgetWidths.Length];
		for (int i = 0; i < widgetWidths.Length; i++)
			widgetMiddles[i] = (i == 0 ? 0 : widgetMiddles[i - 1] + widgetWidths[i - 1] / 2) + widgetWidths[i] / 2;
		var cutOffPoints = new int[] { widgetWidths.Length };
		var rowHeight = Mathf.Min(availableWidth / totalWidth, availableHeight);

		// See if we can make them bigger by wrapping them into multiple rows
		while (true)
		{
			var n = cutOffPoints.Length + 1;
			var newCutOffPoints = new int[n];
			for (int i = 0; i < n; i++)
			{
				newCutOffPoints[i] = widgetMiddles.IndexOf(w => w > (i + 1) * totalWidth / n);
				if (newCutOffPoints[i] == -1)
					newCutOffPoints[i] = widgetWidths.Length;
			}
			var rowWidths = Enumerable.Range(0, n).Select(i => widgetWidths.Skip(i == 0 ? 0 : newCutOffPoints[i - 1]).Take(newCutOffPoints[i] - (i == 0 ? 0 : newCutOffPoints[i - 1])).Sum()).ToArray();
			var newRowHeight = Mathf.Min(availableWidth / rowWidths.Max(), availableHeight / n);
			if (newRowHeight <= rowHeight)
				break;
			cutOffPoints = newCutOffPoints;
			rowHeight = newRowHeight;
		}

		// Move all lights off layer 2 to prevent them from affecting the widgets
		foreach (Light light in FindObjectsOfType<Light>())
		{
			light.cullingMask &= ~(1 << 2);
		}

		for (int i = 0; i < widgets.Count; i++)
		{
			// Setup the camera, using layer 2
			var camera = Instantiate(CameraPrefab);
			edgeworkCameras.Add(camera.gameObject);
			var row = cutOffPoints.IndexOf(ix => ix > i);
			var totalWidthInThisRow = widgetWidths.Skip(row == 0 ? 0 : cutOffPoints[row - 1]).Take(cutOffPoints[row] - (row == 0 ? 0 : cutOffPoints[row - 1])).Sum() * rowHeight;
			var widthBeforeThis = widgetWidths.Skip(row == 0 ? 0 : cutOffPoints[row - 1]).Take(i - (row == 0 ? 0 : cutOffPoints[row - 1])).Sum() * rowHeight;
			camera.rect = new Rect(.5f - totalWidthInThisRow / 2 + widthBeforeThis, 1 - (row + 1) * rowHeight, widgetWidths[i] * rowHeight, rowHeight);
			camera.aspect = (float) widgets[i].SizeX / widgets[i].SizeZ;
			camera.depth = 99;
			camera.fieldOfView = 3.25f;
			camera.transform.SetParent(widgets[i].transform, false);
			camera.transform.localPosition = new Vector3(.001f, 2.26f / widgets[i].SizeX * widgets[i].SizeZ, 0);
			if (widgets[i] is PortWidget || (widgets[i] is ModWidget mw && mw.name == "NumberInd(Clone)"))
				camera.transform.localEulerAngles = new Vector3(90, 180, 0);
			var lossyScale = camera.transform.lossyScale;
			camera.nearClipPlane = 1f * lossyScale.y;
			camera.farClipPlane = 3f / widgets[i].SizeX * widgets[i].SizeZ * lossyScale.y;
			camera.cullingMask = 1 << 2;
			camera.gameObject.SetActive(true);

			// Move the widget’s GameObjects to Layer 2
			foreach (var obj in widgets[i].gameObject.GetComponentsInChildren<Transform>(true))
				if (obj.gameObject.name != "LightGlow")
					obj.gameObject.layer = 2;

			// Add a light source
			var light = camera.gameObject.AddComponent<Light>();
			light.type = LightType.Spot;
			light.cullingMask = 1 << 2;
			light.range = (camera.transform.localPosition.y + 0.05f) * lossyScale.z;
			light.spotAngle = 7.25f;
			light.intensity = 75;
			light.enabled = true;
		}
	}

	public void DisableInteractive()
	{
		foreach (var camera in _moduleCameras)
			camera.EscapePressed = false;
	}

	public IEnumerator DisableCameras()
	{
		var zoomCamera = _moduleCameras.Find(camera => camera.ZoomActive);
		if (zoomCamera != null)
		{
			var enumerator = zoomCamera.UnzoomCamera(1, new SuperZoomData(1, 0.5f, 0.5f));
			while (enumerator.MoveNext())
				yield return enumerator.Current;
		}

		foreach (var alpha in 0.5f.TimedAnimation())
		{
			foreach (var camera in _moduleCameras)
			{
				var original = camera.OriginalCameraRect;
				var end = new Rect(original.x + original.width / 2, original.y + original.height / 2, 0, 0);
				camera.CameraInstance.rect = camera.OriginalCameraRect.Lerp(end, alpha);
			}

			yield return null;
		}

		gameObject.SetActive(false);
	}
	#endregion

	#region Private Methods
	private IEnumerator UpdateStrikesCoroutine(bool delay)
	{
		if (delay)
		{
			// Delay for a single frame if this has been called from an OnStrike method
			// Necessary since the bomb doesn't update its internal counter until all its OnStrike handlers are finished
			yield return 0;
		}
		if (_currentBomb == null) yield break;
		_currentStrikes = _currentBomb.StrikeCount;
		_currentTotalStrikes = _currentBomb.StrikeLimit;
		string strikesText = _currentStrikes.ToString().PadLeft(_currentTotalStrikes.ToString().Length, char.Parse("0"));
		DebugHelper.Log("Updating strikes to " + strikesText);
		StrikesPrefab.text = $"{strikesText}<size=25>/{_currentTotalStrikes}</size>";
	}

	private IEnumerator UnviewModuleCoroutine(TwitchModule handle)
	{
		var camera = _moduleCameras.Find(c => c.Module != null && c.Module == handle);
		if (camera == null)
			yield break;

		// Delayed by 3 second when a module is solved
		if (handle.Solved)
			yield return new WaitForSeconds(3.0f);

		// This second check is necessary in case another module has moved in during the delay.
		// As long as the delay ends before the current move does, this won't be an issue for most modules
		// But some modules with delayed solves would fall foul of it
		if (camera.Module != null && ReferenceEquals(camera.Module, handle))
			camera.ViewModule(PreferredToView);

		// Update the automatic camera wall
		UpdateAutomaticCameraWall();
	}

	private int CurrentModulesContains(TwitchModule component)
	{
		int i = 0;
		foreach (ModuleCamera camera in _moduleCameras)
		{
			if ((camera.Module != null) &&
				(ReferenceEquals(camera.Module, component)))
			{
				return i;
			}

			i++;
		}
		return -1;
	}

	private int BorrowCameraForZoom(TwitchModule component)
	{
		int[] camerasIndexes = { 11, 12, 7, 16, 15, 8, 10, 13, 9, 14, 17, 6, 5, 4, 3, 2, 1, 0 };
		for (int i = _moduleCameras.Count > 6 ? 0 : 12; i < 18; i++)
		{
			int index = camerasIndexes[i];
			ModuleCamera camera = _moduleCameras[index];

			if ((camera.PreviousModule != null) &&
				(ReferenceEquals(camera.PreviousModule, component)))
			{
				//Already borrowed this camera, continue to use it.
				return index;
			}

			if ((camera.Module == null) || camera.Module.CameraPriority == CameraPriority.Pinned)
				continue;

			camera.PreviousModule = camera.Module;
			camera.ViewModule(component);
			return index;
		}

		//Could not even borrow an unpinned camera, to allow the requested zoom to happen.
		return -1;
	}

	private void SetCameraVisibility(bool visible)
	{
		foreach (ModuleCamera camera in _moduleCameras)
			if (!visible || (camera.Module && camera.Module.CameraPriority > CameraPriority.Unviewed))
				camera.CameraInstance.gameObject.SetActive(visible);
	}
	#endregion

	#region Properties
	public TwitchModule PreferredToView => TwitchGame.Instance.Modules
				.Where(module => !module.Solved && !module.Hidden && _moduleCameras.All(cam => cam.Module != module && cam.PreviousModule != module))
				.OrderByDescending(module => module.CameraPriority).ThenBy(module => module.LastUsed)
				.FirstOrDefault();

	public bool HasEmptySlot => _moduleCameras.Any(c => c.Module == null);

	// Make sure automatic camera wall is enabled and respect EnableFactoryZenModeCameraWall
	private static bool AutomaticCameraWallEnabled => Instance.CameraWallMode == Mode.Automatic && !(TwitchPlaySettings.data.EnableFactoryTrainingModeCameraWall && OtherModes.TrainingModeOn && GameRoom.Instance is Factory && IRCConnection.Instance.State == IRCConnectionState.Connected);
	#endregion
}
