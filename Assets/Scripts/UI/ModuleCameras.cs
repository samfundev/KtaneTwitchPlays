using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ModuleCameras : MonoBehaviour
{
    public class ModuleItem
    {
        public MonoBehaviour component = null;
        public MonoBehaviour handle = null;
        public int priority = CameraNotInUse;
        public int index = 0;

        public ModuleItem(MonoBehaviour c, MonoBehaviour h, int p)
        {
            component = c;
            handle = h;
            priority = p;
        }
    }

    public class ModuleCamera : MonoBehaviour
    {
        public Camera cameraInstance = null;
        public int priority = CameraNotInUse;
        public int index = 0;
        public ModuleItem module = null;

        private ModuleCameras parent = null;
        private int originalLayer = 0;

        public ModuleCamera(Camera instantiatedCamera, ModuleCameras parentInstance)
        {
            cameraInstance = instantiatedCamera;
            parent = parentInstance;
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

                // We know the camera's culling mask is pointing at a single layer, so let's find out what that layer is
                int newLayer = (int)Math.Log(cameraInstance.cullingMask, 2);
                originalLayer = module.component.gameObject.layer;
                Debug.LogFormat("[ModuleCameras] Switching component's layer from {0} to {1}", originalLayer, newLayer);
                SetRenderLayer(newLayer);
                cameraInstance.transform.SetParent(module.component.transform, false);
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
            if (module != null)
            {
                SetRenderLayer(originalLayer);
            }
            cameraInstance.gameObject.SetActive(false);
            module = null;
            priority = CameraNotInUse;
        }

        private bool ModuleIsSolved
        {
            get
            {
                return (bool)CommonReflectedTypeInfo.IsSolvedField.GetValue(module.component);
            }
        }

        private void SetRenderLayer(int layer)
        {
            foreach (Transform trans in module.component.gameObject.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layer;
            }
            if (module.handle != null)
            {
                foreach (Transform trans in module.handle.gameObject.GetComponentsInChildren<Transform>(true))
                {
                    trans.gameObject.layer = layer;
                }
            }
        }

    }


    #region Public Fields
    public Text timerPrefab = null;
    public Text strikesPrefab = null;
    public Text strikeLimitPrefab = null;
    public Text solvesPrefab = null;
    public Text totalModulesPrefab = null;
    public Text confidencePrefab = null;
    public Camera[] cameraPrefabs = null;
    public int firstBackupCamera = 3;
    #endregion

    #region Private Fields
    private Stack<ModuleItem>[] stacks = new Stack<ModuleItem>[3];
    private Stack<ModuleItem> moduleStack = new Stack<ModuleItem>();
    private Stack<ModuleItem> priorityModuleStack = new Stack<ModuleItem>();
    private Stack<ModuleItem> pinnedModuleStack = new Stack<ModuleItem>();
    private List<ModuleCamera> cameras = new List<ModuleCamera>();
    private BombCommander currentBomb = null;

    private int currentSolves;
    private int currentStrikes;
    private int currentTotalModules;
    private int currentTotalStrikes;
    private float currentSuccess;
    #endregion

    #region Public Constants
    public const int CameraNotInUse = 0;
    public const int CameraInUse = 1;
    public const int CameraPrioritised = 2;
    public const int CameraPinned = 3;
    #endregion

    #region Public Statics
    public static int index = 0;
    #endregion

    #region Private Static Readonlys
    private const string LogPrefix = "[ModuleCameras] ";
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        
    }

    private void Start()
    {
        foreach (Camera camera in cameraPrefabs)
        {
            Camera instantiatedCamera = Instantiate<Camera>(camera);
            cameras.Add( new ModuleCamera(instantiatedCamera, this) );
        }
        stacks[0] = pinnedModuleStack;
        stacks[1] = priorityModuleStack;
        stacks[2] = moduleStack;
    }

    private void LateUpdate()
    {
        if (currentBomb != null)
        {
            timerPrefab.text = DisplayTime;
            UpdateConfidence();
        }
    }
    #endregion

    #region Public Methods
    public void AttachToModule(MonoBehaviour component, MonoBehaviour handle, int priority = CameraInUse)
    {
        int existingCamera = CurrentModulesContains(component);
        if (existingCamera > -1)
        {
            cameras[existingCamera].index = ++index;
            cameras[existingCamera].module.index = cameras[existingCamera].index;
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

    public void UpdateStrikes(bool delay = false)
    {
        StartCoroutine(UpdateStrikesCoroutine(delay));
    }

    public void UpdateStrikeLimit()
    {
        if (currentBomb != null)
        {
            currentTotalStrikes = (int)CommonReflectedTypeInfo.NumStrikesToLoseField.GetValue(currentBomb.Bomb);
            string totalStrikesText = currentTotalStrikes.ToString();
            Debug.Log(LogPrefix + "Updating strike limit to " + totalStrikesText);
            strikeLimitPrefab.text = "/" + totalStrikesText;
        }
    }

    public void UpdateSolves()
    {
        if (currentBomb != null)
        {
            currentSolves = currentBomb._bombSolvedModules;
            string solves = currentSolves.ToString().PadLeft(currentBomb._bombSolvableModules.ToString().Length, Char.Parse("0"));
            Debug.Log(LogPrefix + "Updating solves to " + solves);
            solvesPrefab.text = solves;
        }
    }

    public void UpdateTotalModules()
    {
        if (currentBomb != null)
        {
            currentTotalModules = currentBomb._bombSolvableModules;
            string total = currentTotalModules.ToString();
            Debug.Log(LogPrefix + "Updating total modules to " + total);
            totalModulesPrefab.text = "/" + total;
        }
    }

    public void UpdateConfidence()
    {
        int previousSuccess = (int)(currentSuccess * 100);
        currentSuccess = PlayerSuccessRating;

        if (previousSuccess != (int)(currentSuccess * 100))
        {
            float minHue = 0.0f; // red (0deg)
            float maxHue = (float)1 / 3; // green (120deg)
            float minBeforeValueDown = 0.25f;
            float maxBeforeSaturationDown = 0.75f;
            float minValue = 0.25f;
            float minSaturation = 0.0f;
            float lowSuccessDesaturationSpeed = 3.0f;

            float hueSuccessRange = maxBeforeSaturationDown - minBeforeValueDown;
            float hueRange = maxHue - minHue;
            float valueRange = 1.0f - minValue;
            float saturationRange = 1.0f - minSaturation;

            float hue, pointOnScale;
            float saturation = 1.0f;
            float value = 1.0f;

            if (currentSuccess < minBeforeValueDown)
            {
                // At very low ratings, move from red to dark grey
                hue = minHue;
                pointOnScale = (currentSuccess - (minBeforeValueDown / lowSuccessDesaturationSpeed)) * lowSuccessDesaturationSpeed;
                pointOnScale = Math.Max(pointOnScale, 0.0f) / minBeforeValueDown;
                saturation = minSaturation + (saturationRange * pointOnScale);
                pointOnScale = currentSuccess / maxBeforeSaturationDown;
                value = minValue + (valueRange * pointOnScale);
            }
            else if (currentSuccess > maxBeforeSaturationDown)
            {
                // At very high ratings, move from green to white
                hue = maxHue;
                pointOnScale = ((1.0f - currentSuccess) / (1.0f - maxBeforeSaturationDown));
                saturation = minSaturation + (saturationRange * pointOnScale);
            }
            else
            {
                // At moderate ratings, move between red and green
                pointOnScale = ((currentSuccess - minBeforeValueDown) / hueSuccessRange);
                hue = minHue + (hueRange * pointOnScale);
            }

            confidencePrefab.color = Color.HSVToRGB(hue, saturation, value);
        }

        string conf = string.Format("{0:00}%", currentSuccess * 100);
        confidencePrefab.text = conf;
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
        if (currentBomb != null)
        {
            currentStrikes = (int)CommonReflectedTypeInfo.NumStrikesField.GetValue(currentBomb.Bomb);
            currentTotalStrikes = (int)CommonReflectedTypeInfo.NumStrikesToLoseField.GetValue(currentBomb.Bomb);
            string strikesText = currentStrikes.ToString().PadLeft(currentTotalStrikes.ToString().Length, Char.Parse("0"));
            Debug.Log(LogPrefix + "Updating strikes to " + strikesText);
            strikesPrefab.text = strikesText;
        }
        yield break;
    }

    private void AddModuleToStack(MonoBehaviour component, MonoBehaviour handle, int priority = CameraInUse)
    {
        ModuleItem item = new ModuleItem(component, handle, priority);
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
            if ((camera.module != null) &&
                (object.ReferenceEquals(camera.module.component, component)))
            {
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

    private string DisplayTime
    {
        get
        {
            string output = currentBomb.GetFullFormattedTime;
            if (output.Length < 7)
            {
                output = "0:" + output;
            }
            return output;
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
            float totalTime = currentBomb._bombStartingTimer;

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
