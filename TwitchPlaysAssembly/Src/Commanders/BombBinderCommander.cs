using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.Missions;
using UnityEngine;

public class BombBinderCommander : ICommandResponder
{
    #region Constructors
    public BombBinderCommander(BombBinder bombBinder)
    {
        BombBinder = bombBinder;
        Selectable = BombBinder.GetComponent<Selectable>();
        FloatingHoldable = BombBinder.GetComponent<FloatingHoldable>();
        SelectableManager = KTInputManager.Instance.SelectableManager;
    }
    #endregion

    #region Interface Implementation
    public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier, IRCConnection connection)
    {
        message = message.ToLowerInvariant();
        if (message.EqualsAny("hold","pick up"))
        {
            IEnumerator holdCoroutine = HoldBombBinder();
            while (holdCoroutine.MoveNext())
            {
                yield return holdCoroutine.Current;
            }
        }
        else
        {
            FloatingHoldable.HoldStateEnum holdState = FloatingHoldable.HoldState;
            if (holdState != FloatingHoldable.HoldStateEnum.Held)
            {
                yield break;
            }

            if (message.EqualsAny("drop","let go","put down"))
            {
                LetGoBombBinder();
            }
            else if (message.Equals("select"))
            {
                IEnumerator selectCoroutine = SelectOnPage();
                while (selectCoroutine.MoveNext())
                {
                    yield return selectCoroutine.Current;
                }
            }
            else if (message.StartsWith("select"))
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
                    if (subCommand.EqualsAny("down","d"))
                    {
                        MoveDownOnPage();
                        yield return new WaitForSeconds(0.2f);
                    }
                    else if (subCommand.EqualsAny("up","u"))
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
        FloatingHoldable.HoldStateEnum holdState = FloatingHoldable.HoldState;

        if (holdState != FloatingHoldable.HoldStateEnum.Held)
        {
            SelectObject(Selectable);

            float holdTime = FloatingHoldable.PickupTime;
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
        FloatingHoldable.HoldStateEnum holdState = FloatingHoldable.HoldState;
        if (holdState == FloatingHoldable.HoldStateEnum.Held)
        {
            DeselectObject(Selectable);
        }
    }
    
    private void InitialisePage()
    {
        Selectable currentPage = Selectable.GetComponentsInChildren<Selectable>(false).Where((x) => x != Selectable).FirstOrDefault();
        _currentSelectable = currentPage.GetCurrentChild();

        _currentSelectable.HandleSelect(true);

        _currentSelectables = currentPage.Children;

        _currentSelectableIndex = 0;
        for (; _currentSelectableIndex < _currentSelectables.Length; ++_currentSelectableIndex)
        {
            Selectable selectable = _currentSelectables[_currentSelectableIndex];
            if (selectable != null && _currentSelectable == selectable)
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
            Selectable newSelectable = _currentSelectables[_currentSelectableIndex];
            if (newSelectable != null)
            {
                _currentSelectable.HandleDeselect(null);
                _currentSelectable = newSelectable;
                _currentSelectable.HandleSelect(true);
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
            Selectable newSelectable = _currentSelectables[_currentSelectableIndex];
            if (newSelectable != null)
            {
                _currentSelectable.HandleDeselect(null);
                _currentSelectable = newSelectable;
                _currentSelectable.HandleSelect(true);
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
            Selectable newSelectable = null;
            for (_currentSelectableIndex = 0; _currentSelectableIndex < _currentSelectables.Length; ++_currentSelectableIndex)
            {
                newSelectable = _currentSelectables[_currentSelectableIndex];
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
                        MissionTableOfContentsMissionEntry tableOfContentsEntryObject = newSelectable.GetComponent<MissionTableOfContentsMissionEntry>();
                        if (tableOfContentsEntryObject == null)
                        {
                            // Previous/Next buttons!
                            newSelectable = null;
                            break;
                        }

                        string entryText = tableOfContentsEntryObject.EntryText.text.ToLowerInvariant();
                        string subsectionText = tableOfContentsEntryObject.SubsectionText.text.ToLowerInvariant();

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
                _currentSelectable.HandleDeselect(null);
                _currentSelectable = newSelectable;
                _currentSelectable.HandleSelect(true);
            }
            else
            {
                yield break;
            }
        }

        if (_currentSelectable != null)
        {
            //Some protection to prevent going into a tutorial; don't have complete support for that!
            MissionTableOfContentsMissionEntry tableOfContentsEntryObject = _currentSelectable.GetComponent<MissionTableOfContentsMissionEntry>();
            if (tableOfContentsEntryObject != null)
            {
                string missionID = tableOfContentsEntryObject.MissionID;
                MissionManager missionManager = MissionManager.Instance;
                Mission mission = missionManager.GetMission(missionID);
                bool isTutorial = mission.IsTutorial;
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

    private void SelectObject(Selectable selectable)
    {
        selectable.HandleSelect(true);
        SelectableManager.Select(selectable, true);
        SelectableManager.HandleInteract();
        selectable.OnInteractEnded();
    }

    private void DeselectObject(Selectable selectable)
    {
        SelectableManager.HandleCancel();
    }

    private IEnumerator ForceHeldRotation(float duration)
    {
        Transform baseTransform = SelectableManager.GetBaseHeldObjectTransform();

        float initialTime = Time.time;
        while (Time.time - initialTime < duration)
        {
            Quaternion currentRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

            SelectableManager.SetZSpin(0.0f);
            SelectableManager.SetControlsRotation(baseTransform.rotation * currentRotation);
            SelectableManager.HandleFaceSelection();
            yield return null;
        }

        SelectableManager.SetZSpin(0.0f);
        SelectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, 0.0f));
        SelectableManager.HandleFaceSelection();
    }
    #endregion

    #region Readonly Fields
    public readonly BombBinder BombBinder = null;
    public readonly Selectable Selectable = null;
    public readonly FloatingHoldable FloatingHoldable = null;
    private readonly SelectableManager SelectableManager = null;
    #endregion

    #region Private Static Fields
    #endregion

    #region Private Fields
    private Selectable _currentSelectable = null;
    private int _currentSelectableIndex = int.MinValue;
    private Selectable[] _currentSelectables = null;
    #endregion
}

