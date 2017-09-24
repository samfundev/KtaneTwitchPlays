using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class BombMessageResponder : MessageResponder
{
    public TwitchBombHandle twitchBombHandlePrefab = null;
    public TwitchComponentHandle twitchComponentHandlePrefab = null;
    public ModuleCameras moduleCamerasPrefab = null;
    public Leaderboard leaderboard = null;
    public TwitchPlaysService parentService = null;

    private List<BombCommander> _bombCommanders = new List<BombCommander>();
    private List<TwitchBombHandle> _bombHandles = new List<TwitchBombHandle>();
    private List<TwitchComponentHandle> _componentHandles = new List<TwitchComponentHandle>();
    private int _currentBomb = -1;

    public static ModuleCameras moduleCameras = null;

    private float specialNameProbability = 0.25f;
    private string[] singleNames = new string[]
    {
        "Bomblebee",
        "Big Bomb",
        "Big Bomb Man",
        "Explodicus",
        "Little Boy",
        "Fat Man",
        "Bombadillo",
        "The Dud",
        "Molotov",
        "Sergeant Cluster",
        "La Bomba",
        "Bombchu",
        "Bomboleo"
    };
    private string[,] doubleNames = new string[,]
    {
        { null, "The Bomb 2: Bomb Harder" },
        { null, "The Bomb 2: The Second Bombing" },
        { "Bomb", "Bomber" },
        { null, "The Bomb Reloaded" },
        { "Bombic", "& Knuckles" },
        { null, "The River Kwai" },
        { "Bomboleo", "Bombolea" }
    };

    #region Unity Lifecycle
    private void OnEnable()
    {
        if (!TwitchPlaysService.DebugMode)
        {
            InputInterceptor.DisableInput();
        }

        leaderboard.ClearSolo();
        TwitchPlaysService.logUploader.Clear();

        StartCoroutine(CheckForBomb());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        leaderboard.BombsAttempted++;
        string bombMessage = null;

        bool HasDetonated = false;
        var timeStarting = float.MaxValue;
        var timeRemaining = float.MaxValue;
        var timeRemainingFormatted = "";

        TwitchPlaysService.logUploader.Post();

        foreach (var commander in _bombCommanders)
        {
            HasDetonated |= (bool) CommonReflectedTypeInfo.HasDetonatedProperty.GetValue(commander.Bomb, null);
            if (timeRemaining > commander.CurrentTimer)
            {
                timeStarting = commander._bombStartingTimer;
                timeRemaining = commander.CurrentTimer;
            }

            if (!string.IsNullOrEmpty(timeRemainingFormatted))
            {
                timeRemainingFormatted += ", " + commander.GetFullFormattedTime;
            }
            else
            {
                timeRemainingFormatted = commander.GetFullFormattedTime;
            }
        }

        if (HasDetonated)
        {
            bombMessage = string.Format("KAPOW KAPOW The bomb has exploded, with {0} remaining! KAPOW KAPOW", timeRemainingFormatted);
            leaderboard.BombsExploded+=_bombCommanders.Count;
            leaderboard.Success = false;
        }
        else
        {
            bombMessage = string.Format("PraiseIt PraiseIt The bomb has been defused, with {0} remaining! PraiseIt PraiseIt", timeRemainingFormatted);
            leaderboard.BombsCleared+=_bombCommanders.Count;
            leaderboard.Success = true;
            if (leaderboard.CurrentSolvers.Count == 1)
            {
                float previousRecord = 0.0f;
                float elapsedTime = timeStarting - timeRemaining;
                string userName = "";
                foreach (string uName in leaderboard.CurrentSolvers.Keys)
                {
                    userName = uName;
                    break;
                }
                if (leaderboard.CurrentSolvers[userName] == (Leaderboard.RequiredSoloSolves * _bombCommanders.Count))
                {
                    leaderboard.AddSoloClear(userName, elapsedTime, out previousRecord);
                    TimeSpan elapsedTimeSpan = TimeSpan.FromSeconds(elapsedTime);
                    bombMessage = string.Format("PraiseIt PraiseIt {0} completed a solo defusal in {1}:{2:00}!", leaderboard.SoloSolver.UserName, (int)elapsedTimeSpan.TotalMinutes, elapsedTimeSpan.Seconds);
                    if (elapsedTime < previousRecord)
                    {
                        TimeSpan previousTimeSpan = TimeSpan.FromSeconds(previousRecord);
                        bombMessage += string.Format(" It's a new record! (Previous record: {0}:{1:00})", (int)previousTimeSpan.TotalMinutes, previousTimeSpan.Seconds);
                    }
                    bombMessage += " PraiseIt PraiseIt";
                }
                else
                {
                    leaderboard.ClearSolo();
                }
            }
        }

        parentService.StartCoroutine(SendDelayedMessage(1.0f, bombMessage, SendAnalysisLink));

        if (moduleCameras != null)
        {
            moduleCameras.gameObject.SetActive(false);
        }

        foreach (var handle in _bombHandles)
        {
            if (handle != null)
            {
                Destroy(handle.gameObject, 2.0f);
            }
        }
        _bombHandles.Clear();
        _bombCommanders.Clear();

        if (_componentHandles != null)
        {
            foreach (TwitchComponentHandle handle in _componentHandles)
            {
                Destroy(handle.gameObject, 2.0f);
            }
        }
        _componentHandles.Clear();

        MusicPlayer.StopAllMusic();
    }
    #endregion

    #region Protected/Private Methods
    private IEnumerator CheckForBomb()
    {
        TwitchComponentHandle.ResetId();

        UnityEngine.Object[] bombs;
        do
        {
            yield return null;
            bombs = FindObjectsOfType(CommonReflectedTypeInfo.BombType);
            if (bombs.Length > 0)
            {
                yield return new WaitForSeconds(0.1f);
                bombs = FindObjectsOfType(CommonReflectedTypeInfo.BombType);
            }

            System.Random rand = new System.Random();

            if (bombs.Length == 1)
            {
                _currentBomb = -1;
                SetBomb((MonoBehaviour) bombs[0], -1);

                if (rand.NextDouble() < specialNameProbability)
                {
                    _bombHandles[0].nameText.text = singleNames[rand.Next(0, singleNames.Length - 1)];
                }
                _coroutineQueue.AddToQueue(_bombHandles[0].OnMessageReceived(_bombHandles[0].nameText.text, "red", "!bomb hold"), -1);
            }
            else
            {
                _currentBomb = 0;
                int id = 0;
                for (var i = bombs.Length - 1; i >= 0; i--)
                {
                    SetBomb((MonoBehaviour) bombs[i], id++);
                }

                if (rand.NextDouble() < specialNameProbability)
                {
                    int nameIndex = rand.Next(0, doubleNames.Length - 1);
                    string nameText = null;
                    for (int i = 0; i < 2; i++)
                    {
                        nameText = doubleNames[nameIndex, i];
                        if (nameText != null)
                        {
                            _bombHandles[i].nameText.text = nameText;
                        }
                    }
                }
                else
                {
                    _bombHandles[1].nameText.text = "The Other Bomb";
                }
                _coroutineQueue.AddToQueue(_bombHandles[0].OnMessageReceived(_bombHandles[0].nameText.text, "red", "!bomb hold"), 0);
            }
        } while (bombs == null || bombs.Length == 0);

        moduleCameras = Instantiate<ModuleCameras>(moduleCamerasPrefab);
    }

    private void SetBomb(MonoBehaviour bomb, int id)
    {
        _bombCommanders.Add(new BombCommander(bomb));
        CreateBombHandleForBomb(bomb, id);
        CreateComponentHandlesForBomb(bomb);

        if (id == -1)
        {
            _ircConnection.SendMessage("The next bomb is now live! Start sending your commands! MrDestructoid");
        }
        else if (id == 0)
        {
            _ircConnection.SendMessage("The next set of bombs are now live! Start sending your commands! MrDestructoid");
        }
    }

    protected override void OnMessageReceived(string userNickName, string userColorCode, string text)
    {
        if (text.Equals("!stop", StringComparison.InvariantCultureIgnoreCase))
        {
            _currentBomb = _coroutineQueue.CurrentBombID;
            return;
        }

        if (text.Equals("!modules", StringComparison.InvariantCultureIgnoreCase))
        {
            moduleCameras.AttachToModules(_componentHandles);
            return;
        }

        if (_currentBomb > -1)
        {
            //Check for !bomb messages, and pass them off to the currently held bomb.
            Match match = Regex.Match(text, "^!bomb (.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string internalCommand = match.Groups[1].Value;
                text = string.Format("!bomb{0} {1}", _currentBomb + 1, internalCommand);
            }
        }

        foreach (var handle in _bombHandles)
        {
            if (handle != null)
            {
                IEnumerator onMessageReceived = handle.OnMessageReceived(userNickName, userColorCode, text);
                if (onMessageReceived == null) continue;

                if (_currentBomb != handle.bombID)
                {
                    _coroutineQueue.AddToQueue(_bombCommanders[_currentBomb].LetGoBomb(), handle.bombID);
                    _currentBomb = handle.bombID;
                }
                _coroutineQueue.AddToQueue(onMessageReceived, handle.bombID);
            }
        }

        foreach (TwitchComponentHandle componentHandle in _componentHandles)
        {
            IEnumerator onMessageReceived = componentHandle.OnMessageReceived(userNickName, userColorCode, text);
            if (onMessageReceived != null)
            {
                if (_currentBomb != componentHandle.bombID)
                {
                    _coroutineQueue.AddToQueue(_bombCommanders[_currentBomb].LetGoBomb(),componentHandle.bombID);
                    _currentBomb = componentHandle.bombID;
                }
                _coroutineQueue.AddToQueue(onMessageReceived,componentHandle.bombID);
            }
        }
    }

    private void CreateBombHandleForBomb(MonoBehaviour bomb, int id)
    {
        TwitchBombHandle _bombHandle = Instantiate<TwitchBombHandle>(twitchBombHandlePrefab);
        _bombHandle.bombID = id;
        _bombHandle.ircConnection = _ircConnection;
        _bombHandle.bombCommander = _bombCommanders[_bombCommanders.Count-1];
        _bombHandle.coroutineQueue = _coroutineQueue;
        _bombHandle.coroutineCanceller = _coroutineCanceller;
        _bombHandles.Add(_bombHandle);
    }

    private bool CreateComponentHandlesForBomb(MonoBehaviour bomb)
    {
        bool foundComponents = false;

        IList bombComponents = (IList)CommonReflectedTypeInfo.BombComponentsField.GetValue(bomb);

        if (bombComponents.Count > 12)
        {
            _bombCommanders[_bombCommanders.Count - 1]._multiDecker = true;
        }

        foreach (MonoBehaviour bombComponent in bombComponents)
        {
            object componentType = CommonReflectedTypeInfo.ComponentTypeField.GetValue(bombComponent);
            int componentTypeInt = (int)Convert.ChangeType(componentType, typeof(int));
            ComponentTypeEnum componentTypeEnum = (ComponentTypeEnum)componentTypeInt;

            switch (componentTypeEnum)
            {
                case ComponentTypeEnum.Empty:
                    continue;

                case ComponentTypeEnum.Timer:
                    _bombCommanders[_bombCommanders.Count - 1]._timerComponent = bombComponent;
                    continue;

                default:
                    foundComponents = true;
                    break;
            }

            TwitchComponentHandle handle = (TwitchComponentHandle)Instantiate(twitchComponentHandlePrefab, bombComponent.transform, false);
            handle.ircConnection = _ircConnection;
            handle.bombCommander = _bombCommanders[_bombCommanders.Count - 1];
            handle.bombComponent = bombComponent;
            handle.componentType = componentTypeEnum;
            handle.coroutineQueue = _coroutineQueue;
            handle.coroutineCanceller = _coroutineCanceller;
            handle.leaderboard = leaderboard;
            handle.bombID = _currentBomb == -1 ? -1 : _bombCommanders.Count - 1;

            Vector3 idealOffset = handle.transform.TransformDirection(GetIdealPositionForHandle(handle, bombComponents, out handle.direction));
            handle.transform.SetParent(bombComponent.transform.parent, true);
            handle.basePosition = handle.transform.localPosition;
            handle.idealHandlePositionOffset = bombComponent.transform.parent.InverseTransformDirection(idealOffset);

            handle.bombCommander._bombSolvableModules++;

            _componentHandles.Add(handle);
        }

        return foundComponents;
    }

    private Vector3 GetIdealPositionForHandle(TwitchComponentHandle thisHandle, IList bombComponents, out TwitchComponentHandle.Direction direction)
    {
        Rect handleBasicRect = new Rect(-0.155f, -0.1f, 0.31f, 0.2f);
        Rect bombComponentBasicRect = new Rect(-0.1f, -0.1f, 0.2f, 0.2f);

        float baseUp = (handleBasicRect.height + bombComponentBasicRect.height) * 0.55f;
        float baseRight = (handleBasicRect.width + bombComponentBasicRect.width) * 0.55f;

        Vector2 extentUp = new Vector2(0.0f, baseUp * 0.1f);
        Vector2 extentRight = new Vector2(baseRight * 0.2f, 0.0f);

        Vector2 extentResult = Vector2.zero;

        while (true)
        {
            Rect handleRect = handleBasicRect;
            handleRect.position += extentRight;
            if (!HasOverlap(thisHandle, handleRect, bombComponentBasicRect, bombComponents))
            {
                extentResult = extentRight;
                direction = TwitchComponentHandle.Direction.Left;
                break;
            }

            handleRect = handleBasicRect;
            handleRect.position -= extentRight;
            if (!HasOverlap(thisHandle, handleRect, bombComponentBasicRect, bombComponents))
            {
                extentResult = -extentRight;
                direction = TwitchComponentHandle.Direction.Right;
                break;
            }

            handleRect = handleBasicRect;
            handleRect.position += extentUp;
            if (!HasOverlap(thisHandle, handleRect, bombComponentBasicRect, bombComponents))
            {
                extentResult = extentUp;
                direction = TwitchComponentHandle.Direction.Down;
                break;
            }

            handleRect = handleBasicRect;
            handleRect.position -= extentUp;
            if (!HasOverlap(thisHandle, handleRect, bombComponentBasicRect, bombComponents))
            {
                extentResult = -extentUp;
                direction = TwitchComponentHandle.Direction.Up;
                break;
            }

            extentUp.y += baseUp * 0.1f;
            extentRight.x += baseRight * 0.1f;
        }

        return new Vector3(extentResult.x, 0.0f, extentResult.y);
    }

    private bool HasOverlap(TwitchComponentHandle thisHandle, Rect handleRect, Rect bombComponentBasicRect, IList bombComponents)
    {
        foreach (MonoBehaviour bombComponent in bombComponents)
        {
            Vector3 bombComponentCenter = thisHandle.transform.InverseTransformPoint(bombComponent.transform.position);

            Rect bombComponentRect = bombComponentBasicRect;
            bombComponentRect.position += new Vector2(bombComponentCenter.x, bombComponentCenter.z);

            if (bombComponentRect.Overlaps(handleRect))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator SendDelayedMessage(float delay, string message, Action callback = null)
    {
        yield return new WaitForSeconds(delay);
        _ircConnection.SendMessage(message);

        if (callback != null)
        {
            callback();
        }
    }

    private void SendAnalysisLink()
    {
        if (!TwitchPlaysService.logUploader.PostToChat())
        {
            Debug.Log("[BombMessageResponder] Analysis URL not found, instructing LogUploader to post when it's ready");
            TwitchPlaysService.logUploader.postOnComplete = true;
        }
    }
    #endregion
}
