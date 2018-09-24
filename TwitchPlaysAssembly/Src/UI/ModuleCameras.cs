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

	public class ModuleItem
	{
		public Dictionary<Transform, int> OriginalLayers = new Dictionary<Transform, int>();
		public BombComponent Component;
		public TwitchComponentHandle Handle;
		public int Priority;
		public int Index;
		public int NonInteractiveCameraLayer = CameraLayer;
		public bool EnableCamera;

		public ModuleItem(BombComponent c, TwitchComponentHandle h, int p)
		{
			Component = c;
			Handle = h;
			Priority = p;

			UpdateLayerData();
		}

		public void UpdateLayerData()
		{
			if (Component != null)
			{
				foreach (Transform trans in Component.gameObject.GetComponentsInChildren<Transform>(true))
				{
					try
					{
						if (OriginalLayers.ContainsKey(trans)) continue;
						OriginalLayers.Add(trans, trans.gameObject.layer);
						if (EnableCamera)
							trans.gameObject.layer = NonInteractiveCameraLayer;
					}
					catch
					{
						//continue;
					}
				}
			}

			if (Handle == null) return;

			foreach (Transform trans in Handle.gameObject.GetComponentsInChildren<Transform>(true))
			{
				try
				{
					if (OriginalLayers.ContainsKey(trans)) continue;
					OriginalLayers.Add(trans, trans.gameObject.layer);
					if (EnableCamera)
						trans.gameObject.layer = NonInteractiveCameraLayer;
				}
				catch
				{
					//continue;
				}
			}
		}

		public void SetRenderLayer(bool enableCamera)
		{
			EnableCamera = enableCamera;
			foreach (KeyValuePair<Transform, int> kvp in OriginalLayers)
			{
				try
				{
					kvp.Key.gameObject.layer = EnableCamera
						? NonInteractiveCameraLayer
						: kvp.Value;
				}
				catch
				{
					//continue;
				}
			}

			Light[] lights = Component.GetComponentsInChildren<Light>(true);
			if (lights == null) return;
			foreach (Light light in lights)
			{
				light.enabled = !light.enabled;
				light.enabled = !light.enabled;
			}
		}
	}

	public class ModuleCamera : MonoBehaviour
	{
		public Camera CameraInstance;
		public int NonInteractiveCameraLayer;
		public int Priority = CameraNotInUse;
		public int Index;
		public ModuleItem Module;
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

		public IEnumerator ZoomCamera(float duration=1.0f)
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

		public IEnumerator UnZoomCamera(float duration = 1.0f)
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

		public void Refresh()
		{
			Deactivate();

			while (Module == null)
			{
				Module = _parent.NextInStack;
				if (Module == null)
				{
					/*
					if (!TakeFromBackupCamera())
					{
						break;
					}*/
					break;
				}
				if (ModuleIsSolved)
				{
					Module = null;
					continue;
				}

				if (Module.Index > 0)
				{
					Index = Module.Index;
				}
				else
				{
					Index = ++ModuleCameras.Index;
					Module.Index = Index;
				}
				Priority = Module.Priority;

				int layer = (LastInteractiveState ? CameraLayer : NonInteractiveCameraLayer);

				CameraInstance.cullingMask = (1 << layer) | (1 << 31);
				Module.NonInteractiveCameraLayer = layer;
				Debug.LogFormat("[ModuleCameras] Switching component's layer from {0} to {1}", Module.Component.gameObject.layer, layer);
				Module.SetRenderLayer(true);
				Transform t = Module.Component.transform.Find("TwitchPlayModuleCamera");
				if (t == null)
				{
					t = new GameObject().transform;
					t.name = "TwitchPlayModuleCamera";
					t.SetParent(Module.Component.transform, false);
				}
				CameraInstance.transform.SetParent(t, false);
				CameraInstance.gameObject.SetActive(true);

				Debug.LogFormat("[ModuleCameras] Component's layer is {0}. Camera's bitmask is {1}", Module.Component.gameObject.layer, CameraInstance.cullingMask);

				Vector3 lossyScale = CameraInstance.transform.lossyScale;
				CameraInstance.nearClipPlane = 1.0f * lossyScale.y;
				CameraInstance.farClipPlane = 3.0f * lossyScale.y;
				Debug.LogFormat("[ModuleCameras] Camera's lossyScale is {0}; Setting near plane to {1}, far plane to {2}", lossyScale, CameraInstance.nearClipPlane, CameraInstance.farClipPlane);
			}
		}

		private void LateUpdate()
		{
			if (Input.GetKey(KeyCode.Escape))
				EscapePressed = true;
			bool currentInteraciveState = (!TwitchPlaySettings.data.EnableTwitchPlaysMode || TwitchPlaySettings.data.EnableInteractiveMode);
			currentInteraciveState |= IRCConnection.Instance.State != IRCConnectionState.Connected;
			currentInteraciveState |= EscapePressed;
			currentInteraciveState &= !(GameRoom.Instance is ElevatorGameRoom);

			if (LastInteractiveState != currentInteraciveState)
			{
				LastInteractiveState = currentInteraciveState;
				int layer = LastInteractiveState ? CameraLayer : NonInteractiveCameraLayer;
				if (Module != null)
				{
					CameraInstance.cullingMask = (1 << layer) | (1 << 31);
					Module.NonInteractiveCameraLayer = layer;
					Module.UpdateLayerData();
					Module.SetRenderLayer(true);
				}
			}
			else
			{
				Module?.UpdateLayerData();
			}
		}

		public void Deactivate()
		{
			Module?.SetRenderLayer(false);
			if (CameraInstance != null)
			{
				CameraInstance.gameObject.SetActive(false);
				if(CameraInstance.transform != null)
					CameraInstance.transform.SetParent(transform, false);
			}

			Module = null;
			Priority = CameraNotInUse;
		}

		private bool ModuleIsSolved => Module.Component.IsSolved;
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
	public int FirstBackupCamera { get => _data.firstBackupCamera; set => _data.firstBackupCamera = value; }
	public Text[] NotesTexts { get => _data.notesTexts; set => _data.notesTexts = value; }
	#endregion

	#region Private Fields
	private ModuleCamerasData _data;
	private readonly Dictionary<BombComponent, ModuleItem> _moduleItems = new Dictionary<BombComponent, ModuleItem>();
	private readonly Stack<ModuleItem>[] _stacks = new Stack<ModuleItem>[4];
	private readonly Stack<ModuleItem> _moduleStack = new Stack<ModuleItem>();
	private readonly Stack<ModuleItem> _claimedModuleStack = new Stack<ModuleItem>();
	private readonly Stack<ModuleItem> _priorityModuleStack = new Stack<ModuleItem>();
	private readonly Stack<ModuleItem> _pinnedModuleStack = new Stack<ModuleItem>();
	private readonly List<ModuleCamera> _cameras = new List<ModuleCamera>();
	private readonly List<ModuleCamera> _camerasQueue = new List<ModuleCamera>();
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

		//Wall of Cameras
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

	#region Public Constants
	public const int CameraNotInUse = 0;
	public const int CameraInUse = 1;
	public const int CameraClaimed = 2;
	public const int CameraPrioritised = 3;
	public const int CameraPinned = 4;
	#endregion

	#region Public Statics
	public static int Index;
	#endregion

	#region Private Static Readonlys
	private const string LogPrefix = "[ModuleCameras] ";
	private static readonly Vector3 HudScale = new Vector3(0.7f, Mathf.Round(1), Mathf.Round(1));
	#endregion

	#region Unity Lifecycle
	private void Awake()
	{
		_data = GetComponent<ModuleCamerasData>();
	}

	private void InstantiateCamera(int layer)
	{
		Camera instantiatedCamera = Instantiate(CameraPrefab);
		instantiatedCamera.rect = _cameraLocations[layer];
		instantiatedCamera.aspect = 1f;
		instantiatedCamera.depth = 99;
		ModuleCamera cam = ModuleCamera.CreateModuleCamera(instantiatedCamera, this, 8 + layer);
		_cameras.Add(cam);
		_camerasQueue.Add(cam);
	}

	private void Start()
	{
		for (int i = 0; i < 6; i++)
		{
			InstantiateCamera(i);
		}
		_stacks[0] = _pinnedModuleStack;
		_stacks[1] = _priorityModuleStack;
		_stacks[2] = _claimedModuleStack;
		_stacks[3] = _moduleStack;
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
			return cam.UnZoomCamera(delay);
		}
		return null;
	}

	public void AttachToModule(BombComponent component, TwitchComponentHandle handle, int priority = CameraInUse)
	{
		if ( handle != null && (handle.Claimed) && (priority == CameraClaimed) )
		{
			priority = CameraClaimed;
		}
		int existingCamera = CurrentModulesContains(component);
		if (existingCamera > -1)
		{
			ModuleCamera cam = _cameras[existingCamera];
			if (cam.Priority < priority)
			{
				cam.Priority = priority;
				cam.Module.Priority = priority;
			}
			cam.Index = ++Index;
			cam.Module.Index = cam.Index;
			return;
		}
		ModuleCamera camera = AvailableCamera(priority);
		try
		{
			// If the camera is in use, return its module to the appropriate stack
			if ((camera.Priority > CameraNotInUse) && (camera.Module.Component != null))
			{
				camera.Module.Index = camera.Index;
				AddModuleToStack(camera.Module.Component, camera.Module.Handle, camera.Priority);
				camera.Priority = CameraNotInUse;
			}

			// Add the new module to the stack
			AddModuleToStack(component, handle, priority);

			// Refresh the camera
			camera.Refresh();
			
		}
		catch (Exception e)
		{
			Debug.Log(LogPrefix + "Error: " + e.Message);
		}
	}

	public void AttachToModules(List<TwitchComponentHandle> handles, int priority = CameraInUse)
	{
		foreach (TwitchComponentHandle handle in Enumerable.Reverse(handles))
		{
			AddModuleToStack(handle.bombComponent, handle, priority);
		}
		foreach (ModuleCamera camera in AvailableCameras(priority - 1))
		{
			camera.Refresh();
		}
	}

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

	public void DetachFromModule(MonoBehaviour component, bool delay = false)
	{
		StartCoroutine(DetachFromModuleCoroutine(component, delay));
	}

	public void Hide()
	{
		SetCameraVisibility(false);
	}

	public void Show()
	{
		SetCameraVisibility(true);
	}

	public void HideHud()
	{
		BombStatus.localScale = Vector3.zero;
	}

	public void ShowHud()
	{
		BombStatus.localScale = HudScale;
	}

	public void UpdateHeader()
	{
		HeaderPrefab.text = _currentBomb.twitchBombHandle.bombName;
	}

	public void UpdateStrikes(bool delay = false)
	{
		StartCoroutine(UpdateStrikesCoroutine(delay));
	}

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
			if(_currentBomb != null)
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
			string pts = "+" + $"{TwitchPlaySettings.GetRewardBonus():0}";
			ConfidencePrefab.text = pts;
		}
	}

	public void EnableWallOfCameras()
	{
		DebugHelper.Log("Enabling Wall of Cameras");
		if (FirstBackupCamera == 6)
		{
			DebugHelper.Log("Wall of Cameras Already enabled");
			return;
		}
		FirstBackupCamera = 6;
		for (int i = 6; i < _cameraLocations.Length; i++)
		{
			InstantiateCamera(i);
			_cameras[i].Refresh();
		}
		DebugHelper.Log("Wall of Cameras Enabled");
	}

	public void DisableWallOfCameras()
	{
		DebugHelper.Log("Disabling Wall of Cameras");
		if (FirstBackupCamera == 3)
		{
			DebugHelper.Log("Wall of Cameras already disabled");
			return;
		}
		FirstBackupCamera = 3;
		while (_cameras.Count > 6)
		{
			ModuleCamera camera = _cameras[6];
			_cameras.RemoveAt(6);
			_camerasQueue.Remove(camera);
			
			if (camera.Module != null)	//Return the module back to the appropriate stack if applicable.
				AddModuleToStack(camera.Module.Component, camera.Module.Handle, camera.Module.Priority);
			camera.Deactivate();
			Destroy(camera.CameraInstance);
			Destroy(camera.gameObject);
		}
		for (int i = 0; i < 6; i++)
		{
			//Now, just in case there were any active view pins on the camera wall, refresh the side cameras so that the view pins remain.
			if (_cameras[i].Module != null)
			{
				AddModuleToStack(_cameras[i].Module.Component, _cameras[i].Module.Handle, _cameras[i].Module.Priority);
			}
			_cameras[i].Refresh();
		}

		DebugHelper.Log("Wall of Cameras Disabled");
	}

	public void ChangeBomb(BombCommander bomb)
	{
		Debug.Log(LogPrefix + "Switching bomb");
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

	private void AddModuleToStack(BombComponent component, TwitchComponentHandle handle, int priority = CameraInUse)
	{
		if (component == null || handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID))
		{
			return;
		}

		if (!_moduleItems.TryGetValue(component, out ModuleItem item))
		{
			item = new ModuleItem(component, handle, priority);
			_moduleItems.Add(component, item);
		}
		else
		{
			item.Priority = priority;
		}

		if (priority >= CameraPinned)
		{
			_pinnedModuleStack.Push(item);
		}
		else if (priority >= CameraPrioritised)
		{
			_priorityModuleStack.Push(item);
		}
		else
		{
			_moduleStack.Push(item);
		}
	}

	private IEnumerator DetachFromModuleCoroutine(MonoBehaviour component, bool delay)
	{
		foreach (ModuleCamera camera in _cameras)
		{
			if ((camera.Module == null) || (!ReferenceEquals(camera.Module.Component, component))) continue;
			if (delay)
			{
				yield return new WaitForSeconds(1.0f);
			}
			// This second check is necessary, in case another module has moved in during the delay
			// As long as the delay ends before the current move does, this won't be an issue for most modules
			// But some modules with delayed solves would fall foul of it
			if ((camera.Module != null) &&
				(ReferenceEquals(camera.Module.Component, component)))
			{
				camera.Refresh();
			}
		}
	}

	private ModuleCamera AvailableCamera(int priority = CameraInUse)
	{
		ModuleCamera bestCamera = null;
		int minPriority = CameraPinned + 1;

		ModuleCamera initialCamera = _camerasQueue.First();
		do
		{
			ModuleCamera cam = _camerasQueue.First();
			_camerasQueue.RemoveAt(0);
			_camerasQueue.Add(cam);

			if (cam.Priority == CameraNotInUse)
			{
				return cam;
				// And we're done!
			}

			//Find the lowest priority in use camera slot who's current module priority is lower than or equal
			//to the requested priority level.
			if (cam.Priority >= minPriority || cam.Priority > priority) continue;
			minPriority = cam.Priority;
			bestCamera = cam;
		} while (initialCamera != _camerasQueue.First());

		if (bestCamera == null) return null;

		//Advance the queue so that the next request to view at the same priority level doesn't cause the next
		//view to occupy the exact same camera slot.
		while (_camerasQueue.IndexOf(bestCamera) != _camerasQueue.Count - 1)
		{
			_camerasQueue.Add(_camerasQueue.First());
			_camerasQueue.RemoveAt(0);
		}

		return bestCamera;
	}

	private IEnumerable<ModuleCamera> AvailableCameras(int priority = CameraInUse)
	{
		return _cameras.Where(c => c.Priority <= priority);
	}

	private int CurrentModulesContains(MonoBehaviour component)
	{
		int i = 0;
		foreach (ModuleCamera camera in _cameras)
		{
			if ( (camera.Module != null) &&
				(ReferenceEquals(camera.Module.Component, component)) )
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
		{
			if (camera.Priority > CameraNotInUse)
			{
				camera.CameraInstance.gameObject.SetActive(visible);
			}
		}
	}
	#endregion

	#region Properties
	private ModuleItem NextInStack
	{
		get
		{
			foreach (Stack<ModuleItem> stack in _stacks)
			{
				while (stack.Count > 0)
				{
					ModuleItem module = stack.Pop();
					int existing = CurrentModulesContains(module.Component);
					if (existing > -1)
					{
						_cameras[existing].Index = ++Index;
					}
					else
					{
						return module;
					}
				}
			}
		   
			/*
			while (priorityModuleStack.Count > 0)
			{
				ModuleItem module = priorityModuleStack.Pop();
				int existing = CurrentModulesContains(module.component);
				if (existing > -1)
				{
					cameras[existing].index = ++index;
				}
				else
				{
					return module;
				}
			}
			while (moduleStack.Count > 0)
			{
				ModuleItem module = moduleStack.Pop();
				int existing = CurrentModulesContains(module.component);
				if (existing > -1)
				{
					cameras[existing].index = ++index;
				}
				else
				{
					return module;
				}
			}
			*/

			return null;
		}
	}

	#endregion
}
