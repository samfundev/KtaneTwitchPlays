using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class BombCommander : ICommandResponder
{
    #region Constructors
    static BombCommander()
    {
        _floatingHoldableType = ReflectionHelper.FindType("FloatingHoldable");
        if (_floatingHoldableType == null)
        {
            return;
        }
        _focusMethod = _floatingHoldableType.GetMethod("Focus", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(Transform), typeof(float), typeof(bool), typeof(bool), typeof(float) }, null);
        _defocusMethod = _floatingHoldableType.GetMethod("Defocus", BindingFlags.Public | BindingFlags.Instance);
        _focusTimeField = _floatingHoldableType.GetField("FocusTime", BindingFlags.Public | BindingFlags.Instance);
        _pickupTimeField = _floatingHoldableType.GetField("PickupTime", BindingFlags.Public | BindingFlags.Instance);
        _holdStateProperty = _floatingHoldableType.GetProperty("HoldState", BindingFlags.Public | BindingFlags.Instance);

        _selectableType = ReflectionHelper.FindType("Selectable");
        _handleSelectMethod = _selectableType.GetMethod("HandleSelect", BindingFlags.Public | BindingFlags.Instance);
        _handleSelectableInteractMethod = _selectableType.GetMethod("HandleInteract", BindingFlags.Public | BindingFlags.Instance);
        _handleSelectableCancelMethod = _selectableType.GetMethod("HandleCancel", BindingFlags.Public | BindingFlags.Instance);
        _onInteractEndedMethod = _selectableType.GetMethod("OnInteractEnded", BindingFlags.Public | BindingFlags.Instance);

        _selectableManagerType = ReflectionHelper.FindType("SelectableManager");
        if (_selectableManagerType == null)
        {
            return;
        }
        _selectMethod = _selectableManagerType.GetMethod("Select", BindingFlags.Public | BindingFlags.Instance);
        _handleInteractMethod = _selectableManagerType.GetMethod("HandleInteract", BindingFlags.Public | BindingFlags.Instance);
        _handleCancelMethod = _selectableManagerType.GetMethod("HandleCancel", BindingFlags.Public | BindingFlags.Instance);
        _setZSpinMethod = _selectableManagerType.GetMethod("SetZSpin", BindingFlags.Public | BindingFlags.Instance);
        _setControlsRotationMethod = _selectableManagerType.GetMethod("SetControlsRotation", BindingFlags.Public | BindingFlags.Instance);
        _getBaseHeldObjectTransformMethod = _selectableManagerType.GetMethod("GetBaseHeldObjectTransform", BindingFlags.Public | BindingFlags.Instance);
        _handleFaceSelectionMethod = _selectableManagerType.GetMethod("HandleFaceSelection", BindingFlags.Public | BindingFlags.Instance);

        _recordManagerType = ReflectionHelper.FindType("Assets.Scripts.Records.RecordManager");
        _recordManagerInstanceProperty = _recordManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        _recordStrikeMethod = _recordManagerType.GetMethod("RecordStrike", BindingFlags.Public | BindingFlags.Instance);
        
        _strikeSourceType = ReflectionHelper.FindType("Assets.Scripts.Records.StrikeSource");
        _componentTypeField = _strikeSourceType.GetField("ComponentType", BindingFlags.Public | BindingFlags.Instance);
        _componentNameField = _strikeSourceType.GetField("ComponentName", BindingFlags.Public | BindingFlags.Instance);
        _interactionTypeField = _strikeSourceType.GetField("InteractionType", BindingFlags.Public | BindingFlags.Instance);
        _timeField = _strikeSourceType.GetField("Time", BindingFlags.Public | BindingFlags.Instance);

        _interactionTypeEnumType = ReflectionHelper.FindType("Assets.Scripts.Records.InteractionTypeEnum");

        _inputManagerType = ReflectionHelper.FindType("KTInputManager");
        if (_inputManagerType == null)
        {
            return;
        }
        _inputManagerInstanceProperty = _inputManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        _selectableManagerProperty = _inputManagerType.GetProperty("SelectableManager", BindingFlags.Public | BindingFlags.Instance);

        _inputManager = (MonoBehaviour)_inputManagerInstanceProperty.GetValue(null, null);

        _onStrikeMethod = CommonReflectedTypeInfo.BombType.GetMethod("OnStrike", BindingFlags.Public | BindingFlags.Instance);
    }

    public BombCommander(MonoBehaviour bomb)
    {
        Bomb = bomb;
        Selectable = (MonoBehaviour)Bomb.GetComponent(_selectableType);
        FloatingHoldable = (MonoBehaviour)Bomb.GetComponent(_floatingHoldableType);
        SelectableManager = (MonoBehaviour)_selectableManagerProperty.GetValue(_inputManager, null);
        BombTimeStamp = DateTime.Now;
        _bombStartingTimer = CurrentTimer;
    }
    #endregion

    #region Interface Implementation
    public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier)
    {
        if (message.Equals("hold", StringComparison.InvariantCultureIgnoreCase) ||
            message.Equals("pick up", StringComparison.InvariantCultureIgnoreCase))
        {
            responseNotifier.ProcessResponse(CommandResponse.Start);

            IEnumerator holdCoroutine = HoldBomb(_heldFrontFace);
            while (holdCoroutine.MoveNext())
            {
                yield return holdCoroutine.Current;
            }

            responseNotifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (message.Equals("turn", StringComparison.InvariantCultureIgnoreCase) ||
                 message.Equals("turn round", StringComparison.InvariantCultureIgnoreCase) ||
                 message.Equals("turn around", StringComparison.InvariantCultureIgnoreCase) ||
                 message.Equals("rotate", StringComparison.InvariantCultureIgnoreCase) ||
                 message.Equals("flip", StringComparison.InvariantCultureIgnoreCase) ||
                 message.Equals("spin", StringComparison.InvariantCultureIgnoreCase))
        {
            responseNotifier.ProcessResponse(CommandResponse.Start);

            IEnumerator holdCoroutine = HoldBomb(!_heldFrontFace);
            while (holdCoroutine.MoveNext())
            {
                yield return holdCoroutine.Current;
            }

            responseNotifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (message.Equals("drop", StringComparison.InvariantCultureIgnoreCase) ||
                 message.Equals("let go", StringComparison.InvariantCultureIgnoreCase) ||
                 message.Equals("put down", StringComparison.InvariantCultureIgnoreCase))
        {
            responseNotifier.ProcessResponse(CommandResponse.Start);

            IEnumerator letGoCoroutine = LetGoBomb();
            while (letGoCoroutine.MoveNext())
            {
                yield return letGoCoroutine.Current;
            }

            responseNotifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (Regex.IsMatch(message, "^(edgework( 45|-45)?)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || 
                 Regex.IsMatch(message, "^(edgework( 45|-45)? )?(top|top right|right top|right|right bottom|bottom right|bottom|bottom left|left bottom|left|left top|top left|)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            responseNotifier.ProcessResponse(CommandResponse.Start);
            bool _45Degrees = Regex.IsMatch(message, "^(edgework(-45| 45)).*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            IEnumerator edgeworkCoroutine = ShowEdgework(message.Replace("edgework", "").Replace(" 45", "").Replace("-45","").Trim().ToLowerInvariant(), _45Degrees);
            while (edgeworkCoroutine.MoveNext())
            {
                yield return edgeworkCoroutine.Current;
            }

            responseNotifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (message.Equals("unview", StringComparison.InvariantCultureIgnoreCase))
        {
            BombMessageResponder.moduleCameras.DetachFromModule(_timerComponent);
        }
        else if (message.StartsWith("view", StringComparison.InvariantCultureIgnoreCase))
        {
            int priority = (message.Equals("view pin", StringComparison.InvariantCultureIgnoreCase)) ? ModuleCameras.CameraPinned : ModuleCameras.CameraPrioritised;
            BombMessageResponder.moduleCameras.AttachToModule(_timerComponent, null, priority);
        }
        else
        {
            responseNotifier.ProcessResponse(CommandResponse.NoResponse);
        }
    }
    #endregion

    #region Helper Methods
    public IEnumerator HoldBomb(bool frontFace = true)
    {
        int holdState = (int)_holdStateProperty.GetValue(FloatingHoldable, null);
        bool doForceRotate = false;

        if (holdState != 0)
        {
            SelectObject(Selectable);
            doForceRotate = true;
            BombMessageResponder.moduleCameras.ChangeBomb(this);
        }
        else if (frontFace != _heldFrontFace)
        {
            doForceRotate = true;
        }

        if (doForceRotate)
        {
            float holdTime = (float)_pickupTimeField.GetValue(FloatingHoldable);
            IEnumerator forceRotationCoroutine = ForceHeldRotation(frontFace, holdTime);
            while (forceRotationCoroutine.MoveNext())
            {
                yield return forceRotationCoroutine.Current;
            }
        }
    }

    public IEnumerator TurnBomb()
    {
        IEnumerator holdBombCoroutine = HoldBomb(!_heldFrontFace);
        while (holdBombCoroutine.MoveNext())
        {
            yield return holdBombCoroutine.Current;
        }
    }

    public IEnumerator LetGoBomb()
    {
        int holdState = (int)_holdStateProperty.GetValue(FloatingHoldable, null);
        if (holdState == 0)
        {
            IEnumerator turnBombCoroutine = HoldBomb(true);
            while (turnBombCoroutine.MoveNext())
            {
                yield return turnBombCoroutine.Current;
            }

            DeselectObject(Selectable);
        }
    }

    public IEnumerator ShowEdgework(string edge, bool _45Degrees)
    {
        BombMessageResponder.moduleCameras.Hide();

        IEnumerator holdCoroutine = HoldBomb(_heldFrontFace);
        while (holdCoroutine.MoveNext())
        {
            yield return holdCoroutine.Current;
        }
        IEnumerator returnToFace;
        float offset = _45Degrees ? 0.0f : 45.0f;

        if (edge == "" || edge == "right")
        {
            IEnumerator firstEdge = DoFreeYRotate(0.0f, 0.0f, 90.0f, 90.0f, 0.3f);
            while (firstEdge.MoveNext())
            {
                yield return firstEdge.Current;
            }
            yield return new WaitForSeconds(2.0f);
        }

        if ((edge == "" && _45Degrees) || edge == "bottom right" || edge == "right bottom")
        {
            IEnumerator firstSecondEdge = edge == ""
                ? DoFreeYRotate(90.0f, 90.0f, 45.0f, 90.0f, 0.3f)
                : DoFreeYRotate(0.0f, 0.0f, 45.0f, 90.0f, 0.3f);
            while (firstSecondEdge.MoveNext())
            {
                yield return firstSecondEdge.Current;
            }
            yield return new WaitForSeconds(2.0f);
        }

        if (edge == "" || edge == "bottom")
        {

            IEnumerator secondEdge = edge == ""
                ? DoFreeYRotate(45.0f + offset, 90.0f, 0.0f, 90.0f, 0.3f)
                : DoFreeYRotate(0.0f, 0.0f, 0.0f, 90.0f, 0.3f);
            while (secondEdge.MoveNext())
            {
                yield return secondEdge.Current;
            }
            yield return new WaitForSeconds(2.0f);
        }

        if ((edge == "" && _45Degrees) || edge == "bottom left" || edge == "left bottom")
        {
            IEnumerator secondThirdEdge = edge == ""
                ? DoFreeYRotate(0.0f, 90.0f, -45.0f, 90.0f, 0.3f)
                : DoFreeYRotate(0.0f, 0.0f, -45.0f, 90.0f, 0.3f);
            while (secondThirdEdge.MoveNext())
            {
                yield return secondThirdEdge.Current;
            }
            yield return new WaitForSeconds(2.0f);
        }

        if (edge == "" || edge == "left")
        {
            IEnumerator thirdEdge = edge == ""
                ? DoFreeYRotate(-45.0f + offset, 90.0f, -90.0f, 90.0f, 0.3f)
                : DoFreeYRotate(0.0f, 0.0f, -90.0f, 90.0f, 0.3f);
            while (thirdEdge.MoveNext())
            {
                yield return thirdEdge.Current;
            }
            yield return new WaitForSeconds(2.0f);
        }

        if ((edge == "" && _45Degrees) || edge == "top left" || edge == "left top")
        {
            IEnumerator thirdFourthEdge = edge == ""
                ? DoFreeYRotate(-90.0f, 90.0f, -135.0f, 90.0f, 0.3f)
                : DoFreeYRotate(0.0f, 0.0f, -135.0f, 90.0f, 0.3f);
            while (thirdFourthEdge.MoveNext())
            {
                yield return thirdFourthEdge.Current;
            }
            yield return new WaitForSeconds(2.0f);
        }

        if (edge == "" || edge == "top")
        {
            IEnumerator fourthEdge = edge == ""
                ? DoFreeYRotate(-135.0f + offset, 90.0f, -180.0f, 90.0f, 0.3f)
                : DoFreeYRotate(0.0f, 0.0f, -180.0f, 90.0f, 0.3f);
            while (fourthEdge.MoveNext())
            {
                yield return fourthEdge.Current;
            }
            yield return new WaitForSeconds(2.0f);
        }

        if ((edge == "" && _45Degrees) || edge == "top right" || edge == "right top")
        {
            IEnumerator fourthFirstEdge = edge == ""
                ? DoFreeYRotate(-180.0f, 90.0f, -225.0f, 90.0f, 0.3f)
                : DoFreeYRotate(0.0f, 0.0f, -225.0f, 90.0f, 0.3f);
            while (fourthFirstEdge.MoveNext())
            {
                yield return fourthFirstEdge.Current;
            }
            yield return new WaitForSeconds(2.0f);
        }

        switch (edge)
        {
            case "right":
                returnToFace = DoFreeYRotate(90.0f, 90.0f, 0.0f, 0.0f, 0.3f);
                break;
            case "right bottom":
            case "bottom right":
                returnToFace = DoFreeYRotate(45.0f, 90.0f, 0.0f, 0.0f, 0.3f);
                break;
            case "bottom":
                returnToFace = DoFreeYRotate(0.0f, 90.0f, 0.0f, 0.0f, 0.3f);
                break;
            case "left bottom":
            case "bottom left":
                returnToFace = DoFreeYRotate(-45.0f, 90.0f, 0.0f, 0.0f, 0.3f);
                break;
            case "left":
                returnToFace = DoFreeYRotate(-90.0f, 90.0f, 0.0f, 0.0f, 0.3f);
                break;
            case "left top":
            case "top left":
                returnToFace = DoFreeYRotate(-135.0f, 90.0f, 0.0f, 0.0f, 0.3f);
                break;
            case "top":
                returnToFace = DoFreeYRotate(-180.0f, 90.0f, 0.0f, 0.0f, 0.3f);
                break;
            default:
            case "top right":
            case "right top":
                returnToFace = DoFreeYRotate(-225.0f + offset, 90.0f, 0.0f, 0.0f, 0.3f);
                break;
        }
        
        while (returnToFace.MoveNext())
        {
            yield return returnToFace.Current;
        }

        BombMessageResponder.moduleCameras.Show();
    }

    public IEnumerator Focus(MonoBehaviour selectable, float focusDistance, bool frontFace)
    {
        IEnumerator holdCoroutine = HoldBomb(frontFace);
        while (holdCoroutine.MoveNext())
        {
            yield return holdCoroutine.Current;
        }

        float focusTime = (float)_focusTimeField.GetValue(FloatingHoldable);
        _focusMethod.Invoke(FloatingHoldable, new object[] { selectable.transform, focusDistance, false, false, focusTime });
        _handleSelectableInteractMethod.Invoke(selectable, null);
    }

    public IEnumerator Defocus(MonoBehaviour selectable, bool frontFace)
    {
        _defocusMethod.Invoke(FloatingHoldable, new object[] { false, false });
        _handleSelectableCancelMethod.Invoke(selectable, null);
        yield break;
    }

    public void RotateByLocalQuaternion(Quaternion localQuaternion)
    {
        Transform baseTransform = (Transform)_getBaseHeldObjectTransformMethod.Invoke(SelectableManager, null);

        float currentZSpin = _heldFrontFace ? 0.0f : 180.0f;

        _setControlsRotationMethod.Invoke(SelectableManager, new object[] { baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, currentZSpin) * localQuaternion });
        _handleFaceSelectionMethod.Invoke(SelectableManager, null);
    }

    public void CauseStrikesToExplosion(string reason)
    {
        for (int strikesToMake = StrikeLimit - StrikeCount; strikesToMake > 0; --strikesToMake)
        {
            CauseStrike(reason);
        }
    }

    public void CauseInstantStrike(string reason)
    {
        CauseStrike(reason);
    }

    public void CauseStrike(string reason)
    {
        object strikeSource = Activator.CreateInstance(_strikeSourceType);
        _componentTypeField.SetValue(strikeSource, Enum.ToObject(CommonReflectedTypeInfo.ComponentTypeEnumType, (int)ComponentTypeEnum.Mod));
        _interactionTypeField.SetValue(strikeSource, Enum.ToObject(_interactionTypeEnumType, (int)InteractionTypeEnum.Other));
        _timeField.SetValue(strikeSource, CurrentTimerElapsed);
        _componentNameField.SetValue(strikeSource, reason);

        object recordManager = _recordManagerInstanceProperty.GetValue(null, null);
        _recordStrikeMethod.Invoke(recordManager, new object[] { strikeSource });

        _onStrikeMethod.Invoke(Bomb, new object[] { null });
    }

    private void SelectObject(MonoBehaviour selectable)
    {
        _handleSelectMethod.Invoke(selectable, new object[] { true });
        _selectMethod.Invoke(SelectableManager, new object[] { selectable, true });
        _handleInteractMethod.Invoke(SelectableManager, null);
        _onInteractEndedMethod.Invoke(selectable, null);
    }

    private void DeselectObject(MonoBehaviour selectable)
    {
        _handleCancelMethod.Invoke(SelectableManager, null);
    }

    private IEnumerator ForceHeldRotation(bool frontFace, float duration)
    {
        Transform baseTransform = (Transform)_getBaseHeldObjectTransformMethod.Invoke(SelectableManager, null);

        float oldZSpin = _heldFrontFace ? 0.0f : 180.0f;
        float targetZSpin = frontFace ? 0.0f : 180.0f;

        float initialTime = Time.time;
        while (Time.time - initialTime < duration)
        {
            float lerp = (Time.time - initialTime) / duration;
            float currentZSpin = Mathf.SmoothStep(oldZSpin, targetZSpin, lerp);

            Quaternion currentRotation = Quaternion.Euler(0.0f, 0.0f, currentZSpin);

            _setZSpinMethod.Invoke(SelectableManager, new object[] { currentZSpin });
            _setControlsRotationMethod.Invoke(SelectableManager, new object[] { baseTransform.rotation * currentRotation });
            _handleFaceSelectionMethod.Invoke(SelectableManager, null);
            yield return null;
        }

        _setZSpinMethod.Invoke(SelectableManager, new object[] { targetZSpin });
        _setControlsRotationMethod.Invoke(SelectableManager, new object[] { baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, targetZSpin) });
        _handleFaceSelectionMethod.Invoke(SelectableManager, null);

        _heldFrontFace = frontFace;
    }

    private IEnumerator DoFreeYRotate(float initialYSpin, float initialPitch, float targetYSpin, float targetPitch, float duration)
    {
        if (!_heldFrontFace)
        {
            initialPitch *= -1;
            initialYSpin *= -1;
            targetPitch *= -1;
            targetYSpin *= -1;
        }

        float initialTime = Time.time;
        while (Time.time - initialTime < duration)
        {
            float lerp = (Time.time - initialTime) / duration;
            float currentYSpin = Mathf.SmoothStep(initialYSpin, targetYSpin, lerp);
            float currentPitch = Mathf.SmoothStep(initialPitch, targetPitch, lerp);

            Quaternion currentRotation = Quaternion.Euler(currentPitch, 0, 0) * Quaternion.Euler(0, currentYSpin, 0);
            RotateByLocalQuaternion(currentRotation);
            yield return null;
        }
        Quaternion target = Quaternion.Euler(targetPitch, 0, 0) * Quaternion.Euler(0, targetYSpin, 0);
        RotateByLocalQuaternion(target);
    }

    public float CurrentTimerElapsed
    {
        get
        {
            MonoBehaviour timerComponent = (MonoBehaviour)CommonReflectedTypeInfo.GetTimerMethod.Invoke(Bomb, null);
            return (float)CommonReflectedTypeInfo.TimeElapsedProperty.GetValue(timerComponent, null);
        }
    }

    public float CurrentTimer
    {
        get
        {
            MonoBehaviour timerComponent = (MonoBehaviour)CommonReflectedTypeInfo.GetTimerMethod.Invoke(Bomb, null);
            return (float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent);
        }
    }

    public string CurrentTimerFormatted
    {
        get
        {
            return (string)CommonReflectedTypeInfo.GetFormattedTimeMethod.Invoke(null, new object[] { CurrentTimer, true });
        }
    }

    public string StartingTimerFormatted
    {
        get
        {
            return (string)CommonReflectedTypeInfo.GetFormattedTimeMethod.Invoke(null, new object[] { _bombStartingTimer, true });
        }
    }

    public string GetFullFormattedTime
    {
        get
        {
            string formattedTime = CurrentTimerFormatted;
            if (CurrentTimer >= 3600.0f)
            {
                int hours = (int) (CurrentTimer / 3600);
                formattedTime = hours + ":" + formattedTime;
            }
            return formattedTime;
        }
    }

	private static string[] solveBased = new string[] { "MemoryV2", "SouvenirModule", "TurnTheKeyAdvanced" };
	private static bool removedSolveBasedModules = false;
	public void RemoveSolveBasedModules()
	{
		if (removedSolveBasedModules) return;
		removedSolveBasedModules = true;

		foreach (KMBombModule module in MonoBehaviour.FindObjectsOfType<KMBombModule>())
		{
			if (solveBased.Contains(module.ModuleType)) {
				module.HandlePass();
			}
		}
	}
	
    public string GetFullStartingTime
    {
        get
        {
            string formattedTime = StartingTimerFormatted;
            if (_bombStartingTimer >= 3600.0f)
            {
                int hours = (int) (_bombStartingTimer / 3600);
               formattedTime = hours + ":" + formattedTime;
            }
            return formattedTime;
        }
    }
	
    public int StrikeCount
    {
        get
        {
            return (int)CommonReflectedTypeInfo.NumStrikesField.GetValue(Bomb);
        }
    }

    public int StrikeLimit
    {
        get
        {
            return (int)CommonReflectedTypeInfo.NumStrikesToLoseField.GetValue(Bomb);
        }
    }

    public int NumberModules
    {
        get
        {
            return _bombSolvableModules;
        }
    }
	#endregion

	#region Readonly Fields
	public readonly MonoBehaviour Bomb = null;
    public readonly MonoBehaviour Selectable = null;
    public readonly MonoBehaviour FloatingHoldable = null;
    private readonly MonoBehaviour SelectableManager = null;
    #endregion

    #region Private Static Fields
    private static Type _floatingHoldableType = null;
    private static MethodInfo _focusMethod = null;
    private static MethodInfo _defocusMethod = null;
    private static FieldInfo _focusTimeField = null;
    private static FieldInfo _pickupTimeField = null;
    private static PropertyInfo _holdStateProperty = null;

    private static Type _selectableType = null;
    private static MethodInfo _handleSelectMethod = null;
    private static MethodInfo _handleSelectableInteractMethod = null;
    private static MethodInfo _handleSelectableCancelMethod = null;
    private static MethodInfo _onInteractEndedMethod = null;

    private static Type _selectableManagerType = null;
    private static MethodInfo _selectMethod = null;
    private static MethodInfo _handleInteractMethod = null;
    private static MethodInfo _handleCancelMethod = null;
    private static MethodInfo _setZSpinMethod = null;
    private static MethodInfo _setControlsRotationMethod = null;
    private static MethodInfo _getBaseHeldObjectTransformMethod = null;
    private static MethodInfo _handleFaceSelectionMethod = null;

    private static Type _recordManagerType = null;
    private static PropertyInfo _recordManagerInstanceProperty = null;
    private static MethodInfo _recordStrikeMethod = null;

    private static Type _strikeSourceType = null;
    private static FieldInfo _componentTypeField = null;
    private static FieldInfo _componentNameField = null;
    private static FieldInfo _interactionTypeField = null;
    private static FieldInfo _timeField = null;

    private static Type _interactionTypeEnumType = null;

    private static MethodInfo _onStrikeMethod = null;

    private static Type _inputManagerType = null;
    private static PropertyInfo _inputManagerInstanceProperty = null;
    private static PropertyInfo _selectableManagerProperty = null;

    private static MonoBehaviour _inputManager = null;
    #endregion

    private bool _heldFrontFace = true;
    public int _bombSolvableModules;
    public int _bombSolvedModules;
    public float _bombStartingTimer;
    public bool _multiDecker = false;
    public MonoBehaviour _timerComponent = null;
    public DateTime BombTimeStamp;
}

