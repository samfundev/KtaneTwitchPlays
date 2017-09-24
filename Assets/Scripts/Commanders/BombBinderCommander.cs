using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class BombBinderCommander : ICommandResponder
{
    #region Constructors
    static BombBinderCommander()
    {
        _floatingHoldableType = ReflectionHelper.FindType("FloatingHoldable");
        if (_floatingHoldableType == null)
        {
            return;
        }
        _pickupTimeField = _floatingHoldableType.GetField("PickupTime", BindingFlags.Public | BindingFlags.Instance);
        _holdStateProperty = _floatingHoldableType.GetProperty("HoldState", BindingFlags.Public | BindingFlags.Instance);

        _selectableType = ReflectionHelper.FindType("Selectable");
        _handleSelectMethod = _selectableType.GetMethod("HandleSelect", BindingFlags.Public | BindingFlags.Instance);
        _handleDeselectMethod = _selectableType.GetMethod("HandleDeselect", BindingFlags.Public | BindingFlags.Instance);
        _onInteractEndedMethod = _selectableType.GetMethod("OnInteractEnded", BindingFlags.Public | BindingFlags.Instance);
        _getCurrentChildMethod = _selectableType.GetMethod("GetCurrentChild", BindingFlags.Public | BindingFlags.Instance);
        _childrenField = _selectableType.GetField("Children", BindingFlags.Public | BindingFlags.Instance);

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

        _inputManagerType = ReflectionHelper.FindType("KTInputManager");
        if (_inputManagerType == null)
        {
            return;
        }
        _instanceProperty = _inputManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        _selectableManagerProperty = _inputManagerType.GetProperty("SelectableManager", BindingFlags.Public | BindingFlags.Instance);

        _inputManager = (MonoBehaviour)_instanceProperty.GetValue(null, null);

        _missionTableOfContentsMissionEntryType = ReflectionHelper.FindType("MissionTableOfContentsMissionEntry");
        _missionIDField = _missionTableOfContentsMissionEntryType.GetField("MissionID", BindingFlags.Public | BindingFlags.Instance);
        _missionEntryTextField = _missionTableOfContentsMissionEntryType.GetField("EntryText", BindingFlags.Public | BindingFlags.Instance);
        _missionSubsectionTextField = _missionTableOfContentsMissionEntryType.GetField("SubsectionText", BindingFlags.Public | BindingFlags.Instance);

        _missionManagerType = ReflectionHelper.FindType("Assets.Scripts.Missions.MissionManager");
        _missionManagerInstanceProperty = _missionManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        _getMissionMethod = _missionManagerType.GetMethod("GetMission", BindingFlags.Public | BindingFlags.Instance);

        _missionType = ReflectionHelper.FindType("Assets.Scripts.Missions.Mission");
        _isTutorialProperty = _missionType.GetProperty("IsTutorial", BindingFlags.Public | BindingFlags.Instance);
    }

    public BombBinderCommander(MonoBehaviour bombBinder)
    {
        BombBinder = bombBinder;
        Selectable = (MonoBehaviour)BombBinder.GetComponent(_selectableType);
        FloatingHoldable = (MonoBehaviour)BombBinder.GetComponent(_floatingHoldableType);
        SelectableManager = (MonoBehaviour)_selectableManagerProperty.GetValue(_inputManager, null);
    }
    #endregion

    #region Interface Implementation
    public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier)
    {
        if (message.Equals("hold", StringComparison.InvariantCultureIgnoreCase) ||
            message.Equals("pick up", StringComparison.InvariantCultureIgnoreCase))
        {
            IEnumerator holdCoroutine = HoldBombBinder();
            while (holdCoroutine.MoveNext())
            {
                yield return holdCoroutine.Current;
            }
        }
        else
        {
            int holdState = (int)_holdStateProperty.GetValue(FloatingHoldable, null);
            if (holdState != 0)
            {
                yield break;
            }

            if (message.Equals("drop", StringComparison.InvariantCultureIgnoreCase) ||
                message.Equals("let go", StringComparison.InvariantCultureIgnoreCase) ||
                message.Equals("put down", StringComparison.InvariantCultureIgnoreCase))
            {
                LetGoBombBinder();
            }
            else if (message.Equals("select", StringComparison.InvariantCultureIgnoreCase))
            {
                IEnumerator selectCoroutine = SelectOnPage();
                while (selectCoroutine.MoveNext())
                {
                    yield return selectCoroutine.Current;
                }
            }
            else if (message.StartsWith("select", StringComparison.InvariantCultureIgnoreCase))
            {
                string[] commandParts = message.Split(' ');
                int index = 0;
                IEnumerator selectCoroutine = null;
                if ( (commandParts.Length == 2) &&
                    (int.TryParse(commandParts[1], out index)) )
                {
                    selectCoroutine = SelectOnPage(index);
                }
                else
                {
                    selectCoroutine = SelectOnPage(0, commandParts.Skip(1).ToArray());
                }
                while (selectCoroutine.MoveNext())
                {
                    yield return selectCoroutine.Current;
                }
            }
            else
            {
                string[] sequence = message.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string subCommand in sequence)
                {
                    if (subCommand.Equals("down", StringComparison.InvariantCultureIgnoreCase) ||
                        subCommand.Equals("d", StringComparison.InvariantCultureIgnoreCase))
                    {
                        MoveDownOnPage();
                        yield return new WaitForSeconds(0.2f);
                    }
                    else if (subCommand.Equals("up", StringComparison.InvariantCultureIgnoreCase) ||
                             subCommand.Equals("u", StringComparison.InvariantCultureIgnoreCase))
                    {
                        MoveUpOnPage();
                        yield return new WaitForSeconds(0.2f);
                    }
                }
            }
        }
    }
    #endregion

    #region Helper Methods
    public IEnumerator HoldBombBinder()
    {
        int holdState = (int)_holdStateProperty.GetValue(FloatingHoldable, null);

        if (holdState != 0)
        {
            SelectObject(Selectable);

            float holdTime = (float)_pickupTimeField.GetValue(FloatingHoldable);
            IEnumerator forceRotationCoroutine = ForceHeldRotation(holdTime);
            while (forceRotationCoroutine.MoveNext())
            {
                yield return forceRotationCoroutine.Current;
            }

            yield return null;

            InitialisePage();
        }
    }

    public void LetGoBombBinder()
    {
        int holdState = (int)_holdStateProperty.GetValue(FloatingHoldable, null);
        if (holdState == 0)
        {
            DeselectObject(Selectable);
        }
    }
    
    private void InitialisePage()
    {
        MonoBehaviour currentPage = (MonoBehaviour)Selectable.GetComponentsInChildren(_selectableType, false).Where((x) => x != Selectable).FirstOrDefault();
        _currentSelectable = (MonoBehaviour)_getCurrentChildMethod.Invoke(currentPage, null);

        _handleSelectMethod.Invoke(_currentSelectable, new object[] { true });

        _currentSelectables = (Array)_childrenField.GetValue(currentPage);

        _currentSelectableIndex = 0;
        for (; _currentSelectableIndex < _currentSelectables.Length; ++_currentSelectableIndex)
        {
            object selectable = _currentSelectables.GetValue(_currentSelectableIndex);
            if (selectable != null && _currentSelectable == (MonoBehaviour)selectable)
            {
                return;
            }
        }

        _currentSelectableIndex = int.MinValue;
    }

    private void MoveDownOnPage()
    {
        if (_currentSelectableIndex == int.MinValue || _currentSelectables == null || _currentSelectable == null)
        {
            return;
        }

        int oldSelectableIndex = _currentSelectableIndex;

        for (++_currentSelectableIndex; _currentSelectableIndex < _currentSelectables.Length; ++_currentSelectableIndex)
        {
            MonoBehaviour newSelectable = (MonoBehaviour)_currentSelectables.GetValue(_currentSelectableIndex);
            if (newSelectable != null)
            {
                _handleDeselectMethod.Invoke(_currentSelectable, new object[] { null });
                _currentSelectable = newSelectable;
                _handleSelectMethod.Invoke(_currentSelectable, new object[] { true });
                return;
            }
        }

        _currentSelectableIndex = oldSelectableIndex;
    }

    private void MoveUpOnPage()
    {
        if (_currentSelectableIndex == int.MinValue || _currentSelectables == null || _currentSelectable == null)
        {
            return;
        }

        int oldSelectableIndex = _currentSelectableIndex;

        for (--_currentSelectableIndex; _currentSelectableIndex >= 0; --_currentSelectableIndex)
        {
            MonoBehaviour newSelectable = (MonoBehaviour)_currentSelectables.GetValue(_currentSelectableIndex);
            if (newSelectable != null)
            {
                _handleDeselectMethod.Invoke(_currentSelectable, new object[] { null });
                _currentSelectable = newSelectable;
                _handleSelectMethod.Invoke(_currentSelectable, new object[] { true });
                return;
            }
        }

        _currentSelectableIndex = oldSelectableIndex;
    }

    private IEnumerator SelectOnPage(int index = 0, string[] search = null)
    {
        if ( (index > 0) || (search != null) )
        {
            if ( (_currentSelectables == null) || (index > _currentSelectables.Length) )
            {
                yield break;
            }

            int i = 0;
            MonoBehaviour newSelectable = null;
            for (_currentSelectableIndex = 0; _currentSelectableIndex < _currentSelectables.Length; ++_currentSelectableIndex)
            {
                newSelectable = (MonoBehaviour)_currentSelectables.GetValue(_currentSelectableIndex);
                if (newSelectable != null)
                {
                    // Index mode
                    if (index > 0)
                    {
                        if (++i == index)
                        {
                            break;
                        }
                    }
                    // Search mode
                    else
                    {
                        object tableOfContentsEntryObject = newSelectable.GetComponent(_missionTableOfContentsMissionEntryType);
                        if (tableOfContentsEntryObject == null)
                        {
                            // Previous/Next buttons!
                            newSelectable = null;
                            break;
                        }
                        
                        object entryTextField = _missionEntryTextField.GetValue(tableOfContentsEntryObject);
                        Type entryTextType = entryTextField.GetType();
                        PropertyInfo entryTextProperty = entryTextType.GetProperty("text");
                        string entryText = entryTextProperty.GetValue(entryTextField, null).ToString().ToLowerInvariant();

                        object subsectionTextField = _missionSubsectionTextField.GetValue(tableOfContentsEntryObject);
                        Type subsectionTextType = subsectionTextField.GetType();
                        PropertyInfo subsectionTextProperty = subsectionTextType.GetProperty("text");
                        string subsectionText = subsectionTextProperty.GetValue(subsectionTextField, null).ToString().ToLowerInvariant();

                        if (subsectionText.Equals(search[0]))
                        {
                            // The first search term matches the mission ID ("2.1" etc)
                            break;
                        }

                        foreach (string s in search)
                        {
                            // All search terms must be found in the mission name
                            if (!entryText.Contains(s.ToLowerInvariant()))
                            {
                                newSelectable = null;
                                break;
                            }
                        }

                        if (newSelectable != null)
                        {
                            break;
                        }
                    }
                }
            }

            if (newSelectable != null)
            {
                _handleDeselectMethod.Invoke(_currentSelectable, new object[] { null });
                _currentSelectable = newSelectable;
                _handleSelectMethod.Invoke(_currentSelectable, new object[] { true });
            }
            else
            {
                yield break;
            }
        }

        if (_currentSelectable != null)
        {
            //Some protection to prevent going into a tutorial; don't have complete support for that!
            object tableOfContentsEntryObject = _currentSelectable.GetComponent(_missionTableOfContentsMissionEntryType);
            Debug.Log(tableOfContentsEntryObject);
            if (tableOfContentsEntryObject != null)
            {
                object missionID = _missionIDField.GetValue(tableOfContentsEntryObject);
                object missionManager = _missionManagerInstanceProperty.GetValue(null, null);
                object mission = _getMissionMethod.Invoke(missionManager, new object[] { missionID });
                bool isTutorial = (bool)_isTutorialProperty.GetValue(mission, null);
                if (isTutorial)
                {
                    yield break;
                }
            }

            SelectObject(_currentSelectable);
            yield return null;
            InitialisePage();
        }
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

    private IEnumerator ForceHeldRotation(float duration)
    {
        Transform baseTransform = (Transform)_getBaseHeldObjectTransformMethod.Invoke(SelectableManager, null);

        float initialTime = Time.time;
        while (Time.time - initialTime < duration)
        {
            Quaternion currentRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

            _setZSpinMethod.Invoke(SelectableManager, new object[] { 0.0f });
            _setControlsRotationMethod.Invoke(SelectableManager, new object[] { baseTransform.rotation * currentRotation });
            _handleFaceSelectionMethod.Invoke(SelectableManager, null);
            yield return null;
        }

        _setZSpinMethod.Invoke(SelectableManager, new object[] { 0.0f });
        _setControlsRotationMethod.Invoke(SelectableManager, new object[] { baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, 0.0f) });
        _handleFaceSelectionMethod.Invoke(SelectableManager, null);
    }
    #endregion

    #region Readonly Fields
    public readonly MonoBehaviour BombBinder = null;
    public readonly MonoBehaviour Selectable = null;
    public readonly MonoBehaviour FloatingHoldable = null;
    private readonly MonoBehaviour SelectableManager = null;
    #endregion

    #region Private Static Fields
    private static Type _floatingHoldableType = null;
    private static FieldInfo _pickupTimeField = null;
    private static PropertyInfo _holdStateProperty = null;

    private static Type _selectableType = null;
    private static MethodInfo _handleSelectMethod = null;
    private static MethodInfo _handleDeselectMethod = null;
    private static MethodInfo _onInteractEndedMethod = null;
    private static MethodInfo _getCurrentChildMethod = null;
    private static FieldInfo _childrenField = null;

    private static Type _selectableManagerType = null;
    private static MethodInfo _selectMethod = null;
    private static MethodInfo _handleInteractMethod = null;
    private static MethodInfo _handleCancelMethod = null;
    private static MethodInfo _setZSpinMethod = null;
    private static MethodInfo _setControlsRotationMethod = null;
    private static MethodInfo _getBaseHeldObjectTransformMethod = null;
    private static MethodInfo _handleFaceSelectionMethod = null;

    private static Type _inputManagerType = null;
    private static PropertyInfo _instanceProperty = null;
    private static PropertyInfo _selectableManagerProperty = null;

    private static Type _missionTableOfContentsMissionEntryType = null;
    private static FieldInfo _missionIDField = null;
    private static FieldInfo _missionEntryTextField = null;
    private static FieldInfo _missionSubsectionTextField = null;

    private static Type _missionManagerType = null;
    private static PropertyInfo _missionManagerInstanceProperty = null;
    private static MethodInfo _getMissionMethod = null;

    private static Type _missionType = null;
    private static PropertyInfo _isTutorialProperty = null;

    private static MonoBehaviour _inputManager = null;
    #endregion

    #region Private Fields
    private MonoBehaviour _currentSelectable = null;
    private int _currentSelectableIndex = int.MinValue;
    private Array _currentSelectables = null;
    #endregion
}

