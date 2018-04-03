using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ModuleCameras : MonoBehaviour
{
	public const int cameraLayer = 11;
	public bool LastInteractiveState;
	public bool EscapePressed;

	public class ModuleItem
	{
		public Dictionary<Transform, int> OriginalLayers = new Dictionary<Transform, int>();
		public BombComponent component = null;
		public TwitchComponentHandle handle = null;
		public int priority = CameraNotInUse;
		public int index = 0;
		public int nonInteractiveCameraLayer = cameraLayer;
		public bool EnableCamera = false;

		public ModuleItem(BombComponent c, TwitchComponentHandle h, int p)
		{
			component = c;
			handle = h;
			priority = p;

			UpdateLayerData();
		}

		public void UpdateLayerData()
		{
			if (component != null)
			{
				foreach (Transform trans in component.gameObject.GetComponentsInChildren<Transform>(true))
				{
					try
					{
						if (OriginalLayers.ContainsKey(trans)) continue;
						OriginalLayers.Add(trans, trans.gameObject.layer);
						if (EnableCamera)
							trans.gameObject.layer = nonInteractiveCameraLayer;
					}
					catch
					{
						continue;
					}
				}
			}

			if (handle == null) return;

			foreach (Transform trans in handle.gameObject.GetComponentsInChildren<Transform>(true))
			{
				try
				{
					if (OriginalLayers.ContainsKey(trans)) continue;
					OriginalLayers.Add(trans, trans.gameObject.layer);
					if (EnableCamera)
						trans.gameObject.layer = nonInteractiveCameraLayer;
				}
				catch
				{
					continue;
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
						? nonInteractiveCameraLayer
						: kvp.Value;
				}
				catch
				{
					continue;
				}
			}

			Light[] lights = component.GetComponentsInChildren<Light>(true);
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
		public Camera cameraInstance = null;
		public int nonInteractiveCameraLayer;
		public int priority = CameraNotInUse;
		public int index = 0;
		public ModuleItem module = null;

		private ModuleCameras parent = null;
		private Rect zoomCameraLocation = new Rect(0.2738095f, 0.12f, 0.452381f, 0.76f);
		private Rect originalCameraRect;

		public ModuleCamera(Camera instantiatedCamera, ModuleCameras parentInstance)
		{
			cameraInstance = instantiatedCamera;
			parent = parentInstance;
			originalCameraRect = cameraInstance.rect;
		}

		public IEnumerator ZoomCamera(float duration=1.0f)
		{
			cameraInstance.depth = 100;
			yield return null;
			float initialTime = Time.time;
			while ((Time.time - initialTime) < duration)
			{
				float lerp = (Time.time - initialTime) / duration;
				cameraInstance.rect = new Rect(Mathf.Lerp(originalCameraRect.x, zoomCameraLocation.x, lerp),
					Mathf.Lerp(originalCameraRect.y, zoomCameraLocation.y, lerp),
					Mathf.Lerp(originalCameraRect.width, zoomCameraLocation.width, lerp),
					Mathf.Lerp(originalCameraRect.height, zoomCameraLocation.height, lerp));

				yield return null;
			}
			cameraInstance.rect = zoomCameraLocation;
		}

		public IEnumerator UnZoomCamera(float duration = 1.0f)
		{
			yield return null;
			float initialTime = Time.time;
			while ((Time.time - initialTime) < duration)
			{
				float lerp = (Time.time - initialTime) / duration;
				cameraInstance.rect = new Rect(Mathf.Lerp(zoomCameraLocation.x, originalCameraRect.x, lerp),
					Mathf.Lerp(zoomCameraLocation.y, originalCameraRect.y, lerp),
					Mathf.Lerp(zoomCameraLocation.width, originalCameraRect.width, lerp),
					Mathf.Lerp(zoomCameraLocation.height, originalCameraRect.height, lerp));

				yield return null;
			}
			cameraInstance.rect = originalCameraRect;
			cameraInstance.depth = 99;
		}

		public void Refresh()
		{
			Deactivate();

			while (module == null)
			{
				module = parent.NextInStack;
				if (module == null)
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
					module = null;
					continue;
				}

				if (module.index > 0)
				{
					index = module.index;
				}
				else
				{
					index = ++ModuleCameras.index;
					module.index = index;
				}
				priority = module.priority;

				int layer = (parent.LastInteractiveState ? cameraLayer : nonInteractiveCameraLayer);

				cameraInstance.cullingMask = (1 << layer) | (1 << 31);
				module.nonInteractiveCameraLayer = layer;
				Debug.LogFormat("[ModuleCameras] Switching component's layer from {0} to {1}", module.component.gameObject.layer, layer);
				module.SetRenderLayer(true);
				Transform t = module.component.transform.Find("TwitchPlayModuleCamera");
				if (t == null)
				{
					t = new GameObject().transform;
					t.name = "TwitchPlayModuleCamera";
					t.SetParent(module.component.transform, false);
				}
				cameraInstance.transform.SetParent(t, false);
				cameraInstance.gameObject.SetActive(true);

				Debug.LogFormat("[ModuleCameras] Component's layer is {0}. Camera's bitmask is {1}", module.component.gameObject.layer, cameraInstance.cullingMask);

				Vector3 lossyScale = cameraInstance.transform.lossyScale;
				cameraInstance.nearClipPlane = 1.0f * lossyScale.y;
				cameraInstance.farClipPlane = 3.0f * lossyScale.y;
				Debug.LogFormat("[ModuleCameras] Camera's lossyScale is {0}; Setting near plane to {1}, far plane to {2}", lossyScale, cameraInstance.nearClipPlane, cameraInstance.farClipPlane);
			}
		}

		public void Deactivate()
		{
			module?.SetRenderLayer(false);
			cameraInstance?.gameObject?.SetActive(false);
			cameraInstance?.transform?.SetParent(parent.transform, false);
			module = null;
			priority = CameraNotInUse;
		}

		private bool ModuleIsSolved
		{
			get
			{
				return module.component.IsSolved;
			}
		}

	}

	#region Public Fields
	public Text TimerPrefab { get => _data.timerPrefab; set => _data.timerPrefab = value; }
	public Text TimerShadowPrefab { get => _data.timerShadowPrefab; set => _data.timerShadowPrefab = value; }
	public Text StrikesPrefab { get => _data.strikesPrefab; set => _data.strikesPrefab = value; }
	public Text StrikeLimitPrefab { get => _data.strikeLimitPrefab; set => _data.strikeLimitPrefab = value; }
	public Text SolvesPrefab { get => _data.solvesPrefab; set => _data.solvesPrefab = value; }
	public Text TotalModulesPrefab { get => _data.totalModulesPrefab; set => _data.totalModulesPrefab = value; }
	public Text ConfidencePrefab { get => _data.confidencePrefab; set => _data.confidencePrefab = value; }
	public Camera CameraPrefab { get => _data.cameraPrefab; set => _data.cameraPrefab = value; }
	public RectTransform BombStatus { get => _data.bombStatus; set => _data.bombStatus = value; }
	public int FirstBackupCamera { get => _data.firstBackupCamera; set => _data.firstBackupCamera = value; }
	public Text[] NotesTexts { get => _data.notesTexts; set => _data.notesTexts = value; }
	#endregion

	#region Private Fields
	private ModuleCamerasData _data = null;
	private Dictionary<BombComponent, ModuleItem> moduleItems = new Dictionary<BombComponent, ModuleItem>();
	private Stack<ModuleItem>[] stacks = new Stack<ModuleItem>[4];
	private Stack<ModuleItem> moduleStack = new Stack<ModuleItem>();
	private Stack<ModuleItem> claimedModuleStack = new Stack<ModuleItem>();
	private Stack<ModuleItem> priorityModuleStack = new Stack<ModuleItem>();
	private Stack<ModuleItem> pinnedModuleStack = new Stack<ModuleItem>();
	private List<ModuleCamera> cameras = new List<ModuleCamera>();
	private BombCommander currentBomb = null;

	private int currentSolves;
	private int currentStrikes;
	private int currentTotalModules;
	private int currentTotalStrikes;

	private Rect[] cameraLocations = new Rect[]
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
	public static int index = 0;
	#endregion

	#region Private Static Readonlys
	private const string LogPrefix = "[ModuleCameras] ";
	private static readonly Vector3 HUDScale = new Vector3(0.7f, Mathf.Round(1), Mathf.Round(1));
	#endregion

	#region Unity Lifecycle
	private void Awake()
	{
		_data = GetComponent<ModuleCamerasData>();
	}

	private void InstantiateCamera(int layer)
	{
		Camera instantiatedCamera = Instantiate<Camera>(CameraPrefab);
		instantiatedCamera.rect = cameraLocations[layer];
		instantiatedCamera.aspect = 1f;
		instantiatedCamera.depth = 99;
		cameras.Add(new ModuleCamera(instantiatedCamera, this) { nonInteractiveCameraLayer = 8 + layer });
	}

	private void Start()
	{
		for (int i = 0; i < 6; i++)
		{
			InstantiateCamera(i);
		}
		stacks[0] = pinnedModuleStack;
		stacks[1] = priorityModuleStack;
		stacks[2] = claimedModuleStack;
		stacks[3] = moduleStack;
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
			foreach (ModuleCamera camera in cameras)
			{
				int layer = LastInteractiveState ? cameraLayer : camera.nonInteractiveCameraLayer;
				if (camera.module == null) continue;

				camera.cameraInstance.cullingMask = (1 << layer) | (1 << 31);
				camera.module.nonInteractiveCameraLayer = layer;
				camera.module.UpdateLayerData();
				camera.module.SetRenderLayer(true);
			}
		}
		else
		{
			foreach (ModuleCamera camera in cameras)
			{
				camera.module?.UpdateLayerData();
			}
		}

		if (currentBomb == null) return;
		string formattedTime = currentBomb.GetFullFormattedTime;
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
			ModuleCamera cam = cameras[existingCamera];
			return cam.ZoomCamera(delay);
		}
		return null;
	}

	public IEnumerator UnzoomCamera(BombComponent component, float delay)
	{
		int existingCamera = CurrentModulesContains(component);
		if (existingCamera > -1)
		{
			ModuleCamera cam = cameras[existingCamera];
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
			ModuleCamera cam = cameras[existingCamera];
			if (cam.priority < priority)
			{
				cam.priority = priority;
				cam.module.priority = priority;
			}
			cam.index = ++index;
			cam.module.index = cam.index;
			return;
		}
		ModuleCamera camera = AvailableCamera(priority);
		try
		{
			// If the camera is in use, return its module to the appropriate stack
			if ((camera.priority > CameraNotInUse) && (camera.module.component != null))
			{
				camera.module.index = camera.index;
				AddModuleToStack(camera.module.component, camera.module.handle, camera.priority);
				camera.priority = CameraNotInUse;
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

	public void HideHUD()
	{
		BombStatus.localScale = Vector3.zero;
	}

	public void ShowHUD()
	{
		BombStatus.localScale = HUDScale;
	}

	public void UpdateStrikes(bool delay = false)
	{
		StartCoroutine(UpdateStrikesCoroutine(delay));
	}

	public void UpdateStrikeLimit()
	{
		if (currentBomb == null) return;
		currentTotalStrikes = currentBomb.StrikeLimit;
		string totalStrikesText = currentTotalStrikes.ToString();
		Debug.Log(LogPrefix + "Updating strike limit to " + totalStrikesText);
		StrikeLimitPrefab.text = "/" + totalStrikesText;
	}

	public void UpdateSolves()
	{
		if (currentBomb == null) return;
		currentSolves = currentBomb.bombSolvedModules;
		string solves = currentSolves.ToString().PadLeft(currentBomb.bombSolvableModules.ToString().Length, Char.Parse("0"));
		Debug.Log(LogPrefix + "Updating solves to " + solves);
		SolvesPrefab.text = solves;
	}

	public void UpdateTotalModules()
	{
		if (currentBomb == null) return;
		currentTotalModules = currentBomb.bombSolvableModules;
		string total = currentTotalModules.ToString();
		Debug.Log(LogPrefix + "Updating total modules to " + total);
		TotalModulesPrefab.text = "/" + total;
	}

	public void UpdateConfidence()
	{
		if (OtherModes.TimeModeOn)
		{
			float timedMultiplier = OtherModes.GetAdjustedMultiplier();
			ConfidencePrefab.color = Color.yellow;
			string conf = "x" + String.Format("{0:0.0}", timedMultiplier);
			string pts = "+" + String.Format("{0:0}", TwitchPlaySettings.GetRewardBonus());
			ConfidencePrefab.text = pts;
			StrikesPrefab.color = Color.yellow;
			StrikeLimitPrefab.color = Color.yellow;
			StrikesPrefab.text = conf;
			StrikeLimitPrefab.text = "";
		}
		else if (OtherModes.ZenModeOn)
		{
			ConfidencePrefab.color = Color.yellow;
			string pts = "+" + String.Format("{0:0}", TwitchPlaySettings.GetRewardBonus());
			ConfidencePrefab.text = pts;
			StrikesPrefab.color = Color.red;
			StrikeLimitPrefab.color = Color.red;
			if(currentBomb != null)
				StrikesPrefab.text = currentBomb.StrikeCount.ToString();
			StrikeLimitPrefab.text = "";
		}

		else if (OtherModes.VSModeOn)
		{
			int bossHealth = OtherModes.GetBossHealth();
			int teamHealth = OtherModes.GetTeamHealth();
			StrikesPrefab.color = Color.cyan;
			StrikeLimitPrefab.color = Color.cyan;
			ConfidencePrefab.color = Color.red;
			StrikeLimitPrefab.text = "";
			StrikesPrefab.text = $"{teamHealth} HP";
			ConfidencePrefab.text = $"{bossHealth} HP";
		}
		else
		{
			ConfidencePrefab.color = Color.yellow;
			string pts = "+" + String.Format("{0:0}", TwitchPlaySettings.GetRewardBonus());
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
		for (int i = 6; i < cameraLocations.Length; i++)
		{
			InstantiateCamera(i);
			cameras[i].Refresh();
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
		while (cameras.Count > 6)
		{
			ModuleCamera camera = cameras[6];
			cameras.RemoveAt(6);
			if (camera.module != null)	//Return the module back to the appropriate stack if applicable.
				AddModuleToStack(camera.module.component, camera.module.handle, camera.module.priority);
			camera.Deactivate();
			Destroy(camera.cameraInstance);
			Destroy(camera);
		}
		for (int i = 0; i < 6; i++)
		{
			//Now, just in case there were any active view pins on the camera wall, refresh the side cameras so that the view pins remain.
			if (cameras[i].module != null)
			{
				AddModuleToStack(cameras[i].module.component, cameras[i].module.handle, cameras[i].module.priority);
			}
			cameras[i].Refresh();
		}

		DebugHelper.Log("Wall of Cameras Disabled");
	}

	public void ChangeBomb(BombCommander bomb)
	{
		Debug.Log(LogPrefix + "Switching bomb");
		currentBomb = bomb;
		UpdateStrikes();
		UpdateStrikeLimit();
		UpdateSolves();
		UpdateTotalModules();
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
		if (currentBomb == null) yield break;
		currentStrikes = currentBomb.StrikeCount;
		currentTotalStrikes = currentBomb.StrikeLimit;
		string strikesText = currentStrikes.ToString().PadLeft(currentTotalStrikes.ToString().Length, Char.Parse("0"));
		Debug.Log(LogPrefix + "Updating strikes to " + strikesText);
		StrikesPrefab.text = strikesText;
	}

	private void AddModuleToStack(BombComponent component, TwitchComponentHandle handle, int priority = CameraInUse)
	{
		if (component == null || handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID))
		{
			return;
		}

		if (!moduleItems.TryGetValue(component, out ModuleItem item))
		{
			item = new ModuleItem(component, handle, priority);
			moduleItems.Add(component, item);
		}
		else
		{
			item.priority = priority;
		}

		if (priority >= CameraPinned)
		{
			pinnedModuleStack.Push(item);
		}
		else if (priority >= CameraPrioritised)
		{
			priorityModuleStack.Push(item);
		}
		else
		{
			moduleStack.Push(item);
		}
	}

	private IEnumerator DetachFromModuleCoroutine(MonoBehaviour component, bool delay)
	{
		foreach (ModuleCamera camera in cameras)
		{
			if ((camera.module == null) || (!object.ReferenceEquals(camera.module.component, component))) continue;
			if (delay)
			{
				yield return new WaitForSeconds(1.0f);
			}
			// This second check is necessary, in case another module has moved in during the delay
			// As long as the delay ends before the current move does, this won't be an issue for most modules
			// But some modules with delayed solves would fall foul of it
			if ((camera.module != null) &&
				(object.ReferenceEquals(camera.module.component, component)))
			{
				camera.Refresh();
			}
		}
		yield break;
	}

	private ModuleCamera AvailableCamera(int priority = CameraInUse)
	{
		ModuleCamera bestCamera = null;
		int minPriority = CameraPinned + 1;
		int minIndex = int.MaxValue;

		foreach (ModuleCamera cam in cameras)
		{
			// First available unused camera
			if (cam.priority == CameraNotInUse)
			{
				return cam;
				// And we're done!
			}
			else if ( (cam.priority < minPriority) ||
				( (cam.priority == minPriority) && (cam.index < minIndex) )  )
			{
				bestCamera = cam;
				minPriority = cam.priority;
				minIndex = cam.index;
			}
		}

		// If no unused camera...
		// return the "best" camera (topmost camera of lowest priority)
		// but not if it's already prioritised and we're not demanding priority
		return (minPriority <= priority) ? bestCamera : null;
	}

	private IEnumerable<ModuleCamera> AvailableCameras(int priority = CameraInUse)
	{
		return cameras.Where(c => c.priority <= priority);
	}

	private int CurrentModulesContains(MonoBehaviour component)
	{
		int i = 0;
		foreach (ModuleCamera camera in cameras)
		{
			if ( (camera.module != null) &&
				(object.ReferenceEquals(camera.module.component, component)) )
			{
				return i;
			}
			i++;
		}
		return -1;
	}

	private void SetCameraVisibility(bool visible)
	{
		foreach (ModuleCamera camera in cameras)
		{
			if (camera.priority > CameraNotInUse)
			{
				camera.cameraInstance.gameObject.SetActive(visible);
			}
		}
	}
	#endregion

	#region Properties
	private ModuleItem NextInStack
	{
		get
		{
			foreach (Stack<ModuleItem> stack in stacks)
			{
				while (stack.Count > 0)
				{
					ModuleItem module = stack.Pop();
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

	public float PlayerSuccessRating
	{
		get
		{
			float solvesMax = 0.5f;
			float strikesMax = 0.3f;
			float timeMax = 0.2f;

			float timeRemaining = currentBomb.CurrentTimer;
			float totalTime = currentBomb.bombStartingTimer;

			int strikesAvailable = (currentTotalStrikes - 1) - currentStrikes; // Strikes without exploding

			float solvesCounter = (float)currentSolves / (currentTotalModules - 1);
			float strikesCounter = (float)strikesAvailable / (currentTotalStrikes - 1);
			float timeCounter = timeRemaining / totalTime;

			float solvesScore = solvesCounter * solvesMax;
			float strikesScore = strikesCounter * strikesMax;
			float timeScore = timeCounter * timeMax;

			return solvesScore + strikesScore + timeScore;
		}
	}
	#endregion
}
