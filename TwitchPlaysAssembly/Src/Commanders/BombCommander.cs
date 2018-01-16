using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Scripts.Records;
using UnityEngine;

public class BombCommander : ICommandResponder
{
    #region Constructors
    public BombCommander(Bomb bomb)
    {
        Bomb = bomb;
        timerComponent = Bomb.GetTimer();
        widgetManager = Bomb.WidgetManager;
        Selectable = Bomb.GetComponent<Selectable>();
        FloatingHoldable = Bomb.GetComponent<FloatingHoldable>();
        SelectableManager = KTInputManager.Instance.SelectableManager;
        BombTimeStamp = DateTime.Now;
        bombStartingTimer = CurrentTimer;
    }
    #endregion

    #region Interface Implementation
    public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier, IRCConnection connection)
    {
        message = message.ToLowerInvariant();

        if(message.EqualsAny("hold","pick up"))
        {
            responseNotifier.ProcessResponse(CommandResponse.Start);

            IEnumerator holdCoroutine = HoldBomb(_heldFrontFace);
            while (holdCoroutine.MoveNext())
            {
                yield return holdCoroutine.Current;
            }

            responseNotifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (message.EqualsAny("turn", "turn round", "turn around", "rotate", "flip", "spin"))
        {
            responseNotifier.ProcessResponse(CommandResponse.Start);

            IEnumerator holdCoroutine = HoldBomb(!_heldFrontFace);
            while (holdCoroutine.MoveNext())
            {
                yield return holdCoroutine.Current;
            }

            responseNotifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (message.EqualsAny("drop","let go","put down"))
        {
            responseNotifier.ProcessResponse(CommandResponse.Start);

            IEnumerator letGoCoroutine = LetGoBomb();
            while (letGoCoroutine.MoveNext())
            {
                yield return letGoCoroutine.Current;
            }

            responseNotifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (Regex.IsMatch(message, "^(edgework( 45|-45)?)$") || 
                 Regex.IsMatch(message, "^(edgework( 45|-45)? )?(top|top right|right top|right|right bottom|bottom right|bottom|bottom left|left bottom|left|left top|top left|)$"))
        {
            responseNotifier.ProcessResponse(CommandResponse.Start);
            bool _45Degrees = Regex.IsMatch(message, "^(edgework(-45| 45)).*$");
            IEnumerator edgeworkCoroutine = ShowEdgework(message.Replace("edgework", "").Replace(" 45", "").Replace("-45","").Trim(), _45Degrees);
            while (edgeworkCoroutine.MoveNext())
            {
                yield return edgeworkCoroutine.Current;
            }

            responseNotifier.ProcessResponse(CommandResponse.EndNotComplete);
        }
        else if (message.Equals("unview"))
        {
            if (BombMessageResponder.moduleCameras != null)
                BombMessageResponder.moduleCameras.DetachFromModule(timerComponent);
        }
        else if (message.StartsWith("view"))
        {
            int priority = (message.Equals("view pin")) ? ModuleCameras.CameraPinned : ModuleCameras.CameraPrioritised;
            if (BombMessageResponder.moduleCameras != null)
                BombMessageResponder.moduleCameras.AttachToModule(timerComponent, null, priority);
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
        FloatingHoldable.HoldStateEnum holdState = FloatingHoldable.HoldState;
        bool doForceRotate = false;

        if (holdState != FloatingHoldable.HoldStateEnum.Held)
        {
            SelectObject(Selectable);
            doForceRotate = true;
            if (BombMessageResponder.moduleCameras != null)
                BombMessageResponder.moduleCameras.ChangeBomb(this);
        }
        else if (frontFace != _heldFrontFace)
        {
            doForceRotate = true;
        }

        if (doForceRotate)
        {
            float holdTime = FloatingHoldable.PickupTime;
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
        FloatingHoldable.HoldStateEnum holdState = FloatingHoldable.HoldState;
        if (holdState == FloatingHoldable.HoldStateEnum.Held)
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
        if (BombMessageResponder.moduleCameras != null)
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
            yield return new WaitForSeconds(0.5f);
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
            yield return new WaitForSeconds(0.5f);
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
            yield return new WaitForSeconds(0.5f);
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
            yield return new WaitForSeconds(0.5f);
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

        if (BombMessageResponder.moduleCameras != null)
            BombMessageResponder.moduleCameras.Show();
    }

	public IEnumerable<Dictionary<string, T>> QueryWidgets<T>(string queryKey, string queryInfo = null)
	{
	    return widgetManager.GetWidgetQueryResponses(queryKey, queryInfo).Select(str => Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, T>>(str));
	}

	public void FillEdgework(bool silent = false)
	{
		List<string> edgework = new List<string>();
		Dictionary<string, string> portNames = new Dictionary<string, string>()
		{
			{ "RJ45", "RJ" },
			{ "StereoRCA", "RCA" }
		};

		var batteries = QueryWidgets<int>(KMBombInfo.QUERYKEY_GET_BATTERIES);
		edgework.Add(string.Format("{0}B {1}H", batteries.Sum(x => x["numbatteries"]), batteries.Count()));

		edgework.Add(QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_INDICATOR).OrderBy(x => x["label"]).Select(x => (x["on"] == "True" ? "*" : "") + x["label"]).Join());

		edgework.Add(QueryWidgets<List<string>>(KMBombInfo.QUERYKEY_GET_PORTS).Select(x => x["presentPorts"].Select(port => portNames.ContainsKey(port) ? portNames[port] : port).OrderBy(y => y).Join(", ")).Select(x => x == "" ? "Empty" : x).Select(x => "[" + x + "]").Join(" "));
		
		edgework.Add(QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER).First()["serial"]);
		
		string edgeworkString = edgework.Where(str => str != "").Join(" // ");
		if (twitchBombHandle.edgeworkText.text == edgeworkString) return;

		twitchBombHandle.edgeworkText.text = edgeworkString;

        if(!silent)
		    twitchBombHandle.ircConnection.SendMessage(TwitchPlaySettings.data.BombEdgework, edgeworkString);
	}
	
    public IEnumerator Focus(Selectable selectable, float focusDistance, bool frontFace)
    {
        IEnumerator holdCoroutine = HoldBomb(frontFace);
        while (holdCoroutine.MoveNext())
        {
            yield return holdCoroutine.Current;
        }

        float focusTime = FloatingHoldable.FocusTime;
        FloatingHoldable.Focus(selectable.transform, focusDistance, false, false, focusTime);
        selectable.HandleSelect(false);
        selectable.HandleInteract();
    }

    public IEnumerator Defocus(Selectable selectable, bool frontFace)
    {
        FloatingHoldable.Defocus(false, false);
        selectable.HandleCancel();
        selectable.HandleDeselect();
        yield break;
    }

    public void RotateByLocalQuaternion(Quaternion localQuaternion)
    {
        Transform baseTransform = SelectableManager.GetBaseHeldObjectTransform();

        float currentZSpin = _heldFrontFace ? 0.0f : 180.0f;

        SelectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, currentZSpin) * localQuaternion);
        SelectableManager.HandleFaceSelection();
    }

    public void CauseStrikesToExplosion(string reason)
    {
        for (int strikesToMake = StrikeLimit - StrikeCount; strikesToMake > 0; --strikesToMake)
        {
            CauseStrike(reason);
        }
    }

    public void CauseStrike(string reason)
    {
        StrikeSource strikeSource = new StrikeSource();
        strikeSource.ComponentType = Assets.Scripts.Missions.ComponentTypeEnum.Mod;
        strikeSource.InteractionType = Assets.Scripts.Records.InteractionTypeEnum.Other;
        strikeSource.Time = CurrentTimerElapsed;
        strikeSource.ComponentName = reason;

        RecordManager recordManager = RecordManager.Instance;
        recordManager.RecordStrike(strikeSource);

        Bomb.OnStrike(null);
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

    private IEnumerator ForceHeldRotation(bool frontFace, float duration)
    {
        Transform baseTransform = SelectableManager.GetBaseHeldObjectTransform();

        float oldZSpin = _heldFrontFace ? 0.0f : 180.0f;
        float targetZSpin = frontFace ? 0.0f : 180.0f;

        float initialTime = Time.time;
        while (Time.time - initialTime < duration)
        {
            float lerp = (Time.time - initialTime) / duration;
            float currentZSpin = Mathf.SmoothStep(oldZSpin, targetZSpin, lerp);

            Quaternion currentRotation = Quaternion.Euler(0.0f, 0.0f, currentZSpin);

            SelectableManager.SetZSpin(currentZSpin);
            SelectableManager.SetControlsRotation(baseTransform.rotation * currentRotation);
            SelectableManager.HandleFaceSelection();
            yield return null;
        }

        SelectableManager.SetZSpin(targetZSpin);
        SelectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, targetZSpin));
        SelectableManager.HandleFaceSelection();

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

    private void HandleStrikeChanges()
    {
        int strikeLimit = StrikeLimit;
        int strikeCount = Math.Min(StrikeCount, StrikeLimit);

        RecordManager RecordManager = RecordManager.Instance;
        GameRecord GameRecord = RecordManager.GetCurrentRecord();
        StrikeSource[] Strikes = GameRecord.Strikes;
        if (Strikes.Length != strikeLimit)
        {
            StrikeSource[] newStrikes = new StrikeSource[Math.Max(strikeLimit, 1)];
            Array.Copy(Strikes, newStrikes, Math.Min(Strikes.Length, newStrikes.Length));
            GameRecord.Strikes = newStrikes;
        }

        if (strikeCount == strikeLimit)
        {
            if (strikeLimit < 1)
            {
                Bomb.NumStrikesToLose = 1;
                strikeLimit = 1;
            }
            Bomb.NumStrikes = strikeLimit - 1;
            CommonReflectedTypeInfo.GameRecordCurrentStrikeIndexField.SetValue(GameRecord, strikeLimit - 1);
            CauseStrike("Strike count / limit changed.");
        }
        else
        {
            Debug.Log(string.Format("[Bomb] Strike from TwitchPlays! {0} / {1} strikes", StrikeCount, StrikeLimit));
            CommonReflectedTypeInfo.GameRecordCurrentStrikeIndexField.SetValue(GameRecord, strikeCount);
            //MasterAudio.PlaySound3DAtTransformAndForget("strike", base.transform, 1f, null, 0f, null);
            float[] rates = { 1, 1.25f, 1.5f, 1.75f, 2 };
            timerComponent.SetRateModifier(rates[Math.Min(strikeCount, 4)]);
            Bomb.StrikeIndicator.StrikeCount = strikeCount;
        }
    }

    public bool IsSolved => Bomb.IsSolved();

    public float CurrentTimerElapsed => timerComponent.TimeElapsed;

    public float CurrentTimer
    {
        get => timerComponent.TimeRemaining;
        set => timerComponent.TimeRemaining = (value < 0) ? 0 : value;
    }

    public string CurrentTimerFormatted => timerComponent.GetFormattedTime(CurrentTimer, true);

    public string StartingTimerFormatted => timerComponent.GetFormattedTime(bombStartingTimer, true);

    public string GetFullFormattedTime => Math.Max(CurrentTimer, 0).FormatTime();

    public string GetFullStartingTime => Math.Max(bombStartingTimer, 0).FormatTime();

    public int StrikeCount
    {
        get => Bomb.NumStrikes;
        set
        {
            if (value < 0) value = 0;   //Simon says is unsolvable with less than zero strikes.
            Bomb.NumStrikes = value;
            HandleStrikeChanges();
        }
    }

    public int StrikeLimit
    {
        get => Bomb.NumStrikesToLose;
        set { Bomb.NumStrikesToLose = value; HandleStrikeChanges(); }
    }

    public int NumberModules => bombSolvableModules;

    private static string[] solveBased = new string[] { "MemoryV2", "SouvenirModule", "TurnTheKeyAdvanced" };
	private bool removedSolveBasedModules = false;
	public void RemoveSolveBasedModules()
	{
		if (removedSolveBasedModules) return;
		removedSolveBasedModules = true;

		foreach (KMBombModule module in MonoBehaviour.FindObjectsOfType<KMBombModule>())
		{
			if (solveBased.Contains(module.ModuleType))
			{
				module.HandlePass();
			}
		}
	}
	#endregion

	#region Readonly Fields
	public readonly Bomb Bomb = null;
    public readonly Selectable Selectable = null;
    public readonly FloatingHoldable FloatingHoldable = null;
    public readonly DateTime BombTimeStamp;

    private readonly SelectableManager SelectableManager = null;
    #endregion

    public TwitchBombHandle twitchBombHandle = null;
    public TimerComponent timerComponent = null;
	public WidgetManager widgetManager = null;
	public int bombSolvableModules;
    public int bombSolvedModules;
    public float bombStartingTimer;
    public bool multiDecker = false;

    private bool _heldFrontFace = true;
}
