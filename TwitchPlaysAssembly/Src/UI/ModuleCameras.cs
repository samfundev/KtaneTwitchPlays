using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ModuleCameras : MonoBehaviour
{
	public const int CameraLayer = 11;

	public class ModuleCamera : MonoBehaviour
	{
		public Camera CameraInstance;
		public int NonInteractiveCameraLayer;
		public TwitchComponentHandle Module;
		public bool LastInteractiveState;
		public bool EscapePressed;

		private ModuleCameras _parent;
		private readonly Rect _zoomCameraLocation = new Rect(0.2738095f, 0.12f, 0.452381f, 0.76f);
		private Rect _originalCameraRect;

		public static ModuleCamera CreateModuleCamera(Camera instantiatedCamera, ModuleCameras parentInstance, int layer)
		{
			ModuleCamera moduleCamera = new GameObject().AddComponent<ModuleCamera>();
			moduleCamera.transform.parent = parentInstance.transform;

			moduleCamera.CameraInstance = instantiatedCamera;
			moduleCamera._parent = parentInstance;
			moduleCamera._originalCameraRect = moduleCamera.CameraInstance.rect;
			moduleCamera.NonInteractiveCameraLayer = layer;
			return moduleCamera;
		}

		public IEnumerator ZoomCamera(float duration = 1.0f)
		{
			CameraInstance.depth = 100;
			yield return null;
			float initialTime = Time.time;
			while ((Time.time - initialTime) < duration)
			{
				float lerp = (Time.time - initialTime) / duration;
				CameraInstance.rect = new Rect(Mathf.Lerp(_originalCameraRect.x, _zoomCameraLocation.x, lerp),
					Mathf.Lerp(_originalCameraRect.y, _zoomCameraLocation.y, lerp),
					Mathf.Lerp(_originalCameraRect.width, _zoomCameraLocation.width, lerp),
					Mathf.Lerp(_originalCameraRect.height, _zoomCameraLocation.height, lerp));

				yield return null;
			}
			CameraInstance.rect = _zoomCameraLocation;
		}

		public IEnumerator UnzoomCamera(float duration = 1.0f)
		{
			yield return null;
			float initialTime = Time.time;
			while ((Time.time - initialTime) < duration)
			{
				float lerp = (Time.time - initialTime) / duration;
				CameraInstance.rect = new Rect(Mathf.Lerp(_zoomCameraLocation.x, _originalCameraRect.x, lerp),
					Mathf.Lerp(_zoomCameraLocation.y, _originalCameraRect.y, lerp),
					Mathf.Lerp(_zoomCameraLocation.width, _originalCameraRect.width, lerp),
					Mathf.Lerp(_zoomCameraLocation.height, _originalCameraRect.height, lerp));

				yield return null;
			}
			CameraInstance.rect = _originalCameraRect;
			CameraInstance.depth = 99;
		}

		public void ViewModule(TwitchComponentHandle module)
		{
			Deactivate();
			if (module == null)
				return;
			Module = module;

			int layer = (LastInteractiveState ? CameraLayer : NonInteractiveCameraLayer);

			CameraInstance.cullingMask = (1 << layer) | (1 << 31);
			Debug.LogFormat("[ModuleCameras] Switching component's layer from {0} to {1}", Module.bombComponent.gameObject.layer, layer);
			Module.SetRenderLayer(layer);
			Transform t = Module.bombComponent.transform.Find("TwitchPlayModuleCamera");
			if (t == null)
			{
				t = new GameObject().transform;
				t.name = "TwitchPlayModuleCamera";
				t.SetParent(Module.bombComponent.transform, false);
			}
			CameraInstance.transform.SetParent(t, false);
			CameraInstance.gameObject.SetActive(true);

			Debug.LogFormat("[ModuleCameras] Component's layer is {0}. Camera's bitmask is {1}", Module.bombComponent.gameObject.layer, CameraInstance.cullingMask);

			Vector3 lossyScale = CameraInstance.transform.lossyScale;
			CameraInstance.nearClipPlane = 1.0f * lossyScale.y;
			CameraInstance.farClipPlane = 3.0f * lossyScale.y;
			Debug.LogFormat("[ModuleCameras] Camera's lossyScale is {0}; Setting near plane to {1}, far plane to {2}", lossyScale, CameraInstance.nearClipPlane, CameraInstance.farClipPlane);
		}

		private void LateUpdate()
		{
			if (Input.GetKey(KeyCode.Escape))
				EscapePressed = true;

			if (Module != null) Module.UpdateLayerData();

			bool currentInteraciveState = (!TwitchPlaySettings.data.EnableTwitchPlaysMode || TwitchPlaySettings.data.EnableInteractiveMode);
			currentInteraciveState |= IRCConnection.Instance.State != IRCConnectionState.Connected;
			currentInteraciveState |= EscapePressed;
			currentInteraciveState &= !(GameRoom.Instance is ElevatorGameRoom);

			if (LastInteractiveState != currentInteraciveState)
			{
				LastInteractiveState = currentInteraciveState;
				if (Module != null)
				{
					int layer = LastInteractiveState ? CameraLayer : NonInteractiveCameraLayer;
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

		private bool ModuleIsSolved => Module.Solved;
	}

	#region Public Fields
	public Text HeaderPrefab { get => _data.headerPrefab; set => _data.headerPrefab = value; }
	public Text TimerPrefab { get => _data.timerPrefab; set => _data.timerPrefab = value; }
	public Text TimerShadowPrefab { get => _data.timerShadowPrefab; set => _data.timerShadowPrefab = value; }
	public Text StrikesPrefab { get => _data.strikesPrefab; set => _data.strikesPrefab = value; }
	public Text SolvesPrefab { get => _data.solvesPrefab; set => _data.solvesPrefab = value; }
	public Text ConfidencePrefab { get => _data.confidencePrefab; set => _data.confidencePrefab = value; }
	public Camera CameraPrefab { get => _data.cameraPrefab; set => _data.cameraPrefab = value; }
	public RectTransform BombStatus { get => _data.bombStatus; set => _data.bombStatus = value; }
	public bool CameraWallEnabled { get => _data.cameraWallEnabled; set => _data.cameraWallEnabled = value; }
	public Text[] NotesTexts { get => _data.notesTexts; set => _data.notesTexts = value; }
	#endregion

	#region Private Fields
	private ModuleCamerasData _data;
	private readonly List<ModuleCamera> _cameras = new List<ModuleCamera>();
	private BombCommander _currentBomb;

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
		new Rect(0.1666667f, 0.651f, 0.1666667f, 0.27f),
		new Rect(0.3333333f, 0.651f, 0.1666667f, 0.27f),
		new Rect(0.5000000f, 0.651f, 0.1666667f, 0.27f),
		new Rect(0.6666667f, 0.651f, 0.1666667f, 0.27f),

		new Rect(0.1666667f, 0.371f, 0.1666667f, 0.28f),
		new Rect(0.3333333f, 0.371f, 0.1666667f, 0.28f),
		new Rect(0.5000000f, 0.371f, 0.1666667f, 0.28f),
		new Rect(0.6666667f, 0.371f, 0.1666667f, 0.28f),

		new Rect(0.1666667f, 0.091f, 0.1666667f, 0.28f),
		new Rect(0.3333333f, 0.091f, 0.1666667f, 0.28f),
		new Rect(0.5000000f, 0.091f, 0.1666667f, 0.28f),
		new Rect(0.6666667f, 0.091f, 0.1666667f, 0.28f)
	};

	//private float currentSuccess;
	#endregion

	#region Public Statics
	public static int Index;
	#endregion

	#region Private Static Readonlys
	private const string LogPrefix = "[ModuleCameras] ";
	private static readonly Vector3 HudScale = new Vector3(0.7f, Mathf.Round(1), Mathf.Round(1));
	#endregion

	#region Unity Lifecycle
	private void Awake() => _data = GetComponent<ModuleCamerasData>();

	// These are the layers used by the 18 module cameras.
	// Layer 11 is the interactive layer (highlightables).
	// Layer 13 is used by KTANE for mouse rendering and 14–15 for VR.
	private static readonly int[] _cameraLayers = { 8, 9, 10, 12, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };

	private void InstantiateCamera(int cameraIx)
	{
		Camera instantiatedCamera = Instantiate(CameraPrefab);
		instantiatedCamera.rect = _cameraLocations[cameraIx];
		instantiatedCamera.aspect = 1f;
		instantiatedCamera.depth = 99;

		ModuleCamera cam = ModuleCamera.CreateModuleCamera(instantiatedCamera, this, _cameraLayers[cameraIx]);

		_cameras.Add(cam);
	}

	private void Start()
	{
		for (int i = 0; i < 6; i++)
			InstantiateCamera(i);
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
	public IEnumerator ZoomCamera(BombComponent component, float delay)
	{
		int existingCamera = CurrentModulesContains(component);
		if (existingCamera > -1)
		{
			ModuleCamera cam = _cameras[existingCamera];
			return cam.ZoomCamera(delay);
		}
		return null;
	}

	public IEnumerator UnzoomCamera(BombComponent component, float delay)
	{
		int existingCamera = CurrentModulesContains(component);
		if (existingCamera > -1)
		{
			ModuleCamera cam = _cameras[existingCamera];
			return cam.UnzoomCamera(delay);
		}
		return null;
	}

	public bool TryViewModule(TwitchComponentHandle module)
	{
		if (module == null)
			return false;

		module.LastUsed = DateTime.UtcNow;

		// Is the module already viewed?
		int existingCamera = CurrentModulesContains(module.bombComponent);
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
		else if (AutomaticCameraWallEnabled && !CameraWallEnabled && _cameras.Where(cam => cam.Module != null && cam.Module.Claimed).Select(cam => cam.Module.PlayerName).Distinct().Count() >= 6)
		{
			EnableCameraWall();
			TryViewModule(module);
		}
		// If the camera is already enabled, replace a suitable SAME-priority camera.
		else
			camera.ViewModule(module);
		return true;
	}

	private ModuleCamera AvailableCamera(CameraPriority maxPriority) => _cameras
				.Where(c => c.Module == null || (c.Module != null && c.Module.CameraPriority <= maxPriority))
				.OrderBy(c => c.Module != null)
				.ThenByDescending(c => c.Module != null ? (CameraPriority?) c.Module.CameraPriority : null)
				.ThenBy(c => c.Module != null ? (DateTime?) c.Module.LastUsed : null)
				.FirstOrDefault();

	public void SetNotes(int noteIndex, string noteText)
	{
		if (noteIndex < 0 || noteIndex > 3) return;
		NotesTexts[noteIndex].text = noteText;
	}

	public void AppendNotes(int noteIndex, string noteText)
	{
		if (noteIndex < 0 || noteIndex > 3) return;
		NotesTexts[noteIndex].text += " " + noteText;
	}

	public void UnviewModule(TwitchComponentHandle handle)
	{
		handle.CameraPriority = handle.Solved ? CameraPriority.Unviewed : handle.Claimed ? CameraPriority.Claimed : CameraPriority.Unviewed;
		StartCoroutine(UnviewModuleCoroutine(handle));
	}

	public void Hide() => SetCameraVisibility(false);

	public void Show() => SetCameraVisibility(true);

	public void HideHud() => BombStatus.localScale = Vector3.zero;

	public void ShowHud() => BombStatus.localScale = HudScale;

	public void UpdateHeader() => HeaderPrefab.text = _currentBomb.twitchBombHandle.bombName;

	public void UpdateStrikes(bool delay = false) => StartCoroutine(UpdateStrikesCoroutine(delay));

	public void UpdateSolves()
	{
		if (_currentBomb == null) return;
		_currentSolves = _currentBomb.bombSolvedModules;
		_currentTotalModules = _currentBomb.bombSolvableModules;
		string solves = _currentSolves.ToString().PadLeft(_currentTotalModules.ToString().Length, char.Parse("0"));
		Debug.Log(LogPrefix + "Updating solves to " + solves);
		SolvesPrefab.text = $"{solves}<size=25>/{_currentTotalModules}</size>";
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
		else if (OtherModes.ZenModeOn)
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
			int bossHealth = OtherModes.GetBossHealth();
			int teamHealth = OtherModes.GetTeamHealth();
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
	}

	public void EnableCameraWall()
	{
		if (CameraWallEnabled)
		{
			DebugHelper.Log("Camera Wall already enabled");
			return;
		}
		DebugHelper.Log("Enabling Camera Wall");
		CameraWallEnabled = true;
		GameRoom.HideCamera();

		for (int i = 6; i < _cameraLocations.Length; i++)
			InstantiateCamera(i);
		while (HasEmptySlot)
		{
			var preferredToView = PreferredToView;
			if (!TryViewModule(preferredToView))
				break;
		}

		DebugHelper.Log("Camera Wall enabled");
	}

	public void DisableCameraWall()
	{
		if (!CameraWallEnabled)
		{
			DebugHelper.Log("Camera Wall already disabled");
			return;
		}
		DebugHelper.Log("Disabling Camera Wall");
		CameraWallEnabled = false;
		GameRoom.ShowCamera();

		while (_cameras.Count > 6)
		{
			ModuleCamera camera = _cameras[6];
			_cameras.RemoveAt(6);

			camera.Deactivate();
			Destroy(camera.CameraInstance);
			Destroy(camera.gameObject);
		}
		for (int i = 0; i < 6; i++)
			TryViewModule(PreferredToView);

		DebugHelper.Log("Camera Wall disabled");
	}

	public void ChangeBomb(BombCommander bomb)
	{
		DebugHelper.Log("Switching bomb");
		_currentBomb = bomb;
		UpdateHeader();
		UpdateStrikes();
		UpdateSolves();
		UpdateConfidence();
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
		Debug.Log(LogPrefix + "Updating strikes to " + strikesText);
		StrikesPrefab.text = $"{strikesText}<size=25>/{_currentTotalStrikes}</size>";
	}

	private IEnumerator UnviewModuleCoroutine(TwitchComponentHandle handle)
	{
		var camera = _cameras.FirstOrDefault(c => c.Module != null && c.Module == handle);
		if (camera == null)
			yield break;

		// Delayed by 1 second when a module is solved
		if (handle.Solved)
			yield return new WaitForSeconds(1.0f);

		// This second check is necessary in case another module has moved in during the delay.
		// As long as the delay ends before the current move does, this won't be an issue for most modules
		// But some modules with delayed solves would fall foul of it
		if (camera.Module != null && ReferenceEquals(camera.Module, handle))
			camera.ViewModule(PreferredToView);

		// Make sure camera wall is supposed to be automatic
		if (!AutomaticCameraWallEnabled) yield break;

		// If there are now 6 or fewer claimed modules, disengage the camera wall
		if (CameraWallEnabled && _cameras.Count(c => c.Module != null && !c.Module.Solved && c.Module.CameraPriority >= CameraPriority.Claimed) <= 6)
			DisableCameraWall();
	}

	private int CurrentModulesContains(MonoBehaviour component)
	{
		int i = 0;
		foreach (ModuleCamera camera in _cameras)
		{
			if ((camera.Module != null) &&
				(ReferenceEquals(camera.Module.bombComponent, component)))
			{
				return i;
			}
			i++;
		}
		return -1;
	}

	private void SetCameraVisibility(bool visible)
	{
		foreach (ModuleCamera camera in _cameras)
			if (!visible || camera.Module.CameraPriority > CameraPriority.Unviewed)
				camera.CameraInstance.gameObject.SetActive(visible);
	}
	#endregion

	#region Properties
	private TwitchComponentHandle PreferredToView => BombMessageResponder.Instance.ComponentHandles
				.Where(module => !module.Solved && !_cameras.Any(cam => cam.Module == module))
				.OrderByDescending(module => module.CameraPriority).ThenBy(module => module.LastUsed)
				.FirstOrDefault();

	public bool HasEmptySlot => _cameras.Any(c => c.Module == null);

	// Make sure automatic camera wall is enabled and respect EnableFactoryZenModeCameraWall
	private bool AutomaticCameraWallEnabled => TwitchPlaySettings.data.EnableAutomaticCameraWall && !(TwitchPlaySettings.data.EnableFactoryZenModeCameraWall && OtherModes.ZenModeOn && GameRoom.Instance is Factory && IRCConnection.Instance.State == IRCConnectionState.Connected);
	#endregion
}
