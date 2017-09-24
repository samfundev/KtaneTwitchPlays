using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public abstract class ComponentSolver : ICommandResponder
{
    public delegate IEnumerator RegexResponse(Match match);

    #region Constructors
    static ComponentSolver()
    {
        _selectableType = ReflectionHelper.FindType("Selectable");
        _interactMethod = _selectableType.GetMethod("HandleInteract", BindingFlags.Public | BindingFlags.Instance);
        _interactEndedMethod = _selectableType.GetMethod("OnInteractEnded", BindingFlags.Public | BindingFlags.Instance);
        _setHighlightMethod = _selectableType.GetMethod("SetHighlight", BindingFlags.Public | BindingFlags.Instance);
        _getFocusDistanceMethod = _selectableType.GetMethod("GetFocusDistance", BindingFlags.Public | BindingFlags.Instance);

        Type thisType = typeof(ComponentSolver);
        _onPassInternalMethod = thisType.GetMethod("OnPass", BindingFlags.NonPublic | BindingFlags.Instance);
        _onStrikeInternalMethod = thisType.GetMethod("OnStrike", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public ComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller)
    {
        BombCommander = bombCommander;
        BombComponent = bombComponent;
        Selectable = (MonoBehaviour)bombComponent.GetComponent(_selectableType);
        IRCConnection = ircConnection;
        Canceller = canceller;
    
        HookUpEvents();
    }
    #endregion

    #region Interface Implementation
    public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier, IRCConnection connection)
    {
		_responded = false;
        _processingTwitchCommand = true;
        if (Solved)
        {
            responseNotifier.ProcessResponse(CommandResponse.NoResponse);
            _processingTwitchCommand = false;
            yield break;
        }

        _currentResponseNotifier = responseNotifier;
        _currentUserNickName = userNickName;

        int beforeStrikeCount = StrikeCount;

		IEnumerator subcoroutine = null;
        if (message.StartsWith("send to module ", StringComparison.InvariantCultureIgnoreCase))
        {
            message = message.Substring(15);
        }
        else
        {
            subcoroutine = RespondToCommandCommon(message, userNickName);
        }

        if (subcoroutine == null || !subcoroutine.MoveNext())
        {
            if (_responded)
            {
                yield break;
            }

            try
			{
				subcoroutine = RespondToCommandInternal(message);
			}
			catch (Exception e)
			{
				HandleModuleException(e);
				yield break;
			}

			bool moved = false;
			if (subcoroutine != null)
			{
				try
				{
					moved = subcoroutine.MoveNext();
				}
				catch (Exception e)
				{
					HandleModuleException(e);
					yield break;
				}
			}

            if (subcoroutine == null || !moved || Solved || beforeStrikeCount != StrikeCount)
            {
                if (Solved || beforeStrikeCount != StrikeCount)
                {
                    IEnumerator focusDefocus = BombCommander.Focus(Selectable, FocusDistance, FrontFace);
                    while (focusDefocus.MoveNext())
                    {
                        yield return focusDefocus.Current;
                    }
                    yield return new WaitForSeconds(0.5f);

                    responseNotifier.ProcessResponse(Solved ? CommandResponse.EndComplete : CommandResponse.EndError);

                    focusDefocus = BombCommander.Defocus(Selectable, FrontFace);
                    while (focusDefocus.MoveNext())
                    {
                        yield return focusDefocus.Current;
                    }
                    yield return new WaitForSeconds(0.5f);
                }
                else
				{
					IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.InvalidCommand, userNickName));

					responseNotifier.ProcessResponse(CommandResponse.NoResponse);
				}

                _currentResponseNotifier = null;
                _currentUserNickName = null;
                _processingTwitchCommand = false;
                yield break;
            }
        }

        responseNotifier.ProcessResponse(CommandResponse.Start);

        IEnumerator focusCoroutine = BombCommander.Focus(Selectable, FocusDistance, FrontFace);
        while (focusCoroutine.MoveNext())
        {
            yield return focusCoroutine.Current;
        }

        yield return new WaitForSeconds(0.5f);

        int previousStrikeCount = StrikeCount;
        bool parseError = false;
        bool needQuaternionReset = false;
		bool exceptionThrown = false;
		
        while (previousStrikeCount == StrikeCount && !Solved)
        {
			try
			{
				if (!subcoroutine.MoveNext())
				{
					break;
				}
				else
				{
					_responded = true;
				}
			}
			catch (Exception e)
			{
				exceptionThrown = true;
				HandleModuleException(e);
				break;
			}

            object currentValue = subcoroutine.Current;
            if (currentValue is string)
            {
                string currentString = (string)currentValue;
                if (currentString.Equals("strike", StringComparison.InvariantCultureIgnoreCase))
                {
                    _delegatedStrikeUserNickName = userNickName;
                    _delegatedStrikeResponseNotifier = responseNotifier;
                }
                else if (currentString.Equals("solve", StringComparison.InvariantCultureIgnoreCase))
                {
                    _delegatedSolveUserNickName = userNickName;
                    _delegatedSolveResponseNotifier = responseNotifier;
                }
                else if (currentString.StartsWith("strikemessage ", StringComparison.InvariantCultureIgnoreCase) && 
                    currentString.Substring(14).Trim() != string.Empty)
                {
                    StrikeMessage = currentString.Substring(14);
                }
                else if (currentString.Equals("parseerror", StringComparison.InvariantCultureIgnoreCase))
                {
                    parseError = true;
                    break;
                }
                else if (currentString.Equals("trycancel", StringComparison.InvariantCultureIgnoreCase) && 
                    Canceller.ShouldCancel)
                {
                    Canceller.ResetCancel();
                    break;
                }
                else if (currentString.StartsWith("sendtochat ", StringComparison.InvariantCultureIgnoreCase) && 
                    currentString.Substring(11).Trim() != string.Empty)
                {
                    IRCConnection.SendMessage(currentString.Substring(11));
                }
                else if (currentString.StartsWith("add strike", StringComparison.InvariantCultureIgnoreCase))
                {
                    OnStrike(null);
                }
                else if (currentString.Equals("multiple strikes", StringComparison.InvariantCultureIgnoreCase))
                {
                    DisableOnStrike = true;
                }
                else if (currentString.StartsWith("award strikes ", StringComparison.CurrentCultureIgnoreCase))
                {
                    int awardStrikeCount;
                    if (int.TryParse(currentString.Substring(14), out awardStrikeCount))
                    {
                        _strikeCount += awardStrikeCount;
                        AwardStrikes(_currentUserNickName, _currentResponseNotifier, awardStrikeCount);
                        DisableOnStrike = false;
                    }
                }
                else if (currentString.StartsWith("autosolve", StringComparison.InvariantCultureIgnoreCase))
                {
                    HandleModuleException(new Exception(currentString));
                    break;
                }
                else if (currentString.Equals("elevator music", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (_musicPlayer == null)
                    {
                        _musicPlayer = MusicPlayer.StartRandomMusic();
                    }
                }
            }
            else if (currentValue is Quaternion)
            {
				if (!needQuaternionReset)
				{
					BombMessageResponder.moduleCameras.Hide();
				}

                Quaternion localQuaternion = (Quaternion)currentValue;
                BombCommander.RotateByLocalQuaternion(localQuaternion);
                needQuaternionReset = true;
            }
            yield return currentValue;
        }

		if (!_responded && !exceptionThrown)
		{
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.InvalidCommand, userNickName));
		}

        if (needQuaternionReset)
        {
            BombCommander.RotateByLocalQuaternion(Quaternion.identity);
			BombMessageResponder.moduleCameras.Show();
		}

        if (_musicPlayer != null)
        {
            _musicPlayer.StopMusic();
            _musicPlayer = null;
        }

        if (parseError)
        {
            responseNotifier.ProcessResponse(CommandResponse.NoResponse);
        }
        else
        {
            if (!Solved && (previousStrikeCount == StrikeCount))
            {
                responseNotifier.ProcessResponse(CommandResponse.EndNotComplete);
            }

            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator defocusCoroutine = BombCommander.Defocus(Selectable, FrontFace);
        while (defocusCoroutine.MoveNext())
        {
            yield return defocusCoroutine.Current;
        }

        yield return new WaitForSeconds(0.5f);

        _currentResponseNotifier = null;
        _currentUserNickName = null;
        _processingTwitchCommand = false;
    }
    #endregion

    #region Abstract Interface
    protected abstract IEnumerator RespondToCommandInternal(string inputCommand);
    #endregion

    #region Protected Helper Methods
    protected void DoInteractionStart(MonoBehaviour interactable)
    {
        MonoBehaviour selectable = (MonoBehaviour)interactable.GetComponent(_selectableType);
        _interactMethod.Invoke(selectable, null);
    }

    protected void DoInteractionEnd(MonoBehaviour interactable)
    {
        MonoBehaviour selectable = (MonoBehaviour)interactable.GetComponent(_selectableType);
        _interactEndedMethod.Invoke(selectable, null);
        _setHighlightMethod.Invoke(selectable, new object[] { false });
    }

    protected string GetModuleType()
    {
        KMBombModule bombModule = BombComponent.GetComponent<KMBombModule>();
        if (bombModule != null)
            return bombModule.ModuleType;
        KMNeedyModule needyModule = BombComponent.GetComponent<KMNeedyModule>();
        if (needyModule != null)
            return needyModule.ModuleType;
        return null;
    }

	protected WaitForSeconds DoInteractionClick(MonoBehaviour interactable, string strikeMessage=null)
	{
	    if (strikeMessage != null)
	    {
	        StrikeMessage = strikeMessage;
	    }
        DoInteractionStart(interactable);
		DoInteractionEnd(interactable);
	    return new WaitForSeconds(0.1f);
	}

	protected void HandleModuleException(Exception e)
	{
		Debug.Log("[TwitchPlays] While solving a module an exception has occurred! Here's the error:");
		Debug.LogException(e);

		IRCConnection.SendMessage("Looks like a module ran into a problem while running a command, automatically solving module. Some other modules may also be solved to prevent problems.");

		_currentUserNickName = null;
		_delegatedSolveUserNickName = null;

	    TwitchComponentHandle.RemoveSolveBasedModules();
		CommonReflectedTypeInfo.HandlePassMethod.Invoke(BombComponent, null);
	}
	#endregion

	#region Private Methods
	private void HookUpEvents()
    {
        Delegate gameOnPassDelegate = (Delegate)CommonReflectedTypeInfo.OnPassField.GetValue(BombComponent);
        Delegate internalOnPassDelegate = Delegate.CreateDelegate(CommonReflectedTypeInfo.PassEventType, this, _onPassInternalMethod);
        CommonReflectedTypeInfo.OnPassField.SetValue(BombComponent, Delegate.Combine(internalOnPassDelegate, gameOnPassDelegate));

        Delegate gameOnStrikeDelegate = (Delegate)CommonReflectedTypeInfo.OnStrikeField.GetValue(BombComponent);
        Delegate internalOnStrikeDelegate = Delegate.CreateDelegate(CommonReflectedTypeInfo.StrikeEventType, this, _onStrikeInternalMethod);
        CommonReflectedTypeInfo.OnStrikeField.SetValue(BombComponent, Delegate.Combine(internalOnStrikeDelegate, gameOnStrikeDelegate));
    }

    private bool OnPass(object _ignore)
    {
        //string componentType = ComponentHandle.componentType.ToString();
        //string headerText = (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(BombComponent, null);

        int moduleScore = modInfo.moduleScore;
        if (modInfo.moduleScoreIsDynamic)
        {
            switch (modInfo.moduleScore)
            {
                case 0:
                    moduleScore = (BombCommander.bombSolvableModules) / 2;
                    break;
                default:
                    moduleScore = 5;
                    break;
            }
        }

        switch (modInfo.moduleID)
        {
            case "NeedyVentComponentSolver":
            case "NeedyKnobComponentSolver":
            case "NeedyDischargeComponentSolver":
                return false;
            default:
                if (BombComponent.GetComponent<KMNeedyModule>() != null)
                {
                    return false;
                }
                break;
        }

        if (_delegatedSolveUserNickName != null && _delegatedSolveResponseNotifier != null)
        {

            AwardSolve(_delegatedSolveUserNickName, _delegatedSolveResponseNotifier, moduleScore);
            _delegatedSolveUserNickName = null;
            _delegatedSolveResponseNotifier = null;
        }
        else if (_currentUserNickName != null && _currentResponseNotifier != null)
        {
            AwardSolve(_currentUserNickName, _currentResponseNotifier, moduleScore);
        }

        BombCommander.bombSolvedModules++;
        BombMessageResponder.moduleCameras.UpdateSolves();

        if (_turnQueued)
        {
            Debug.LogFormat("[ComponentSolver] Activating queued turn for completed module {0}.", Code);
            _readyToTurn = true;
            _turnQueued = false;
        }

        ComponentHandle.OnPass();

        BombMessageResponder.moduleCameras.DetachFromModule(BombComponent, true);

        return false;
    }

    public IEnumerator TurnBombOnSolve()
    {
        while(_turnQueued)
            yield return new WaitForSeconds(0.1f);

        if (!_readyToTurn)
            yield break;

        while (_processingTwitchCommand)
            yield return new WaitForSeconds(0.1f);

        _readyToTurn = false;
        IEnumerator turnCoroutine = BombCommander.TurnBomb();
        while (turnCoroutine.MoveNext())
        {
            yield return turnCoroutine.Current;
        }

        yield return new WaitForSeconds(0.5f);
    }

    private bool DisableOnStrike;
    private bool OnStrike(object _ignore)
    {
        //string headerText = (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(BombComponent, null);
        if (DisableOnStrike) return false;

        _strikeCount++;


        if (_delegatedStrikeUserNickName != null && _delegatedStrikeResponseNotifier != null)
        {
            AwardStrikes(_delegatedStrikeUserNickName, _delegatedStrikeResponseNotifier, 1);
            _delegatedStrikeUserNickName = null;
            _delegatedStrikeResponseNotifier = null;
        }
        else if (_currentUserNickName != null && _currentResponseNotifier != null)
        {
            AwardStrikes(_currentUserNickName, _currentResponseNotifier, 1);
        }

        BombMessageResponder.moduleCameras.UpdateStrikes(true);

        return false;
    }

    public bool OnStrikes(object _ignore)
    {
        _strikeCount++;
        BombMessageResponder.moduleCameras.UpdateStrikes(true);
        return false;

    }

    private void AwardSolve(string userNickName, ICommandResponseNotifier responseNotifier, int ComponentValue)
    {
        string headerText = (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(BombComponent, null);
        IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.AwardSolve, Code, userNickName, ComponentValue, headerText));
        string RecordMessageTone = "Module ID: " + Code + " | Player: " + userNickName + " | Module Name: " + headerText + " | Value: " + ComponentValue;
        responseNotifier.ProcessResponse(CommandResponse.EndComplete, ComponentValue);
        TwitchPlaySettings.AppendToSolveStrikeLog(RecordMessageTone);
        TwitchPlaySettings.AppendToPlayerLog(userNickName);
    }

    private void AwardStrikes(string userNickName, ICommandResponseNotifier responseNotifier, int strikeCount)
    {
        string headerText = (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(BombComponent, null);
        int strikePenalty = -6 * (TwitchPlaySettings.data.EnableRewardMultipleStrikes ? strikeCount : 1);
        IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.AwardStrike, Code, strikeCount == 1 ? "a" : strikeCount.ToString(), strikeCount == 1 ? "" : "s", 0, userNickName, string.IsNullOrEmpty(StrikeMessage) ? "" : " caused by " + StrikeMessage, headerText, strikePenalty));
        string RecordMessageTone = "Module ID: " + Code + " | Player: " + userNickName + " | Module Name: " + headerText + " | Strike";
        for (int i = 0; i < strikeCount; i++)
        {
            TwitchPlaySettings.AppendToSolveStrikeLog(RecordMessageTone);
            if (!TwitchPlaySettings.data.EnableRewardMultipleStrikes)
            {
                break;
            }
        }
        responseNotifier.ProcessResponse(CommandResponse.EndError, strikeCount);
        StrikeMessage = string.Empty;
    }
    #endregion

    public string Code
    {
        get;
        set;
    }
    
    #region Protected Properties

    protected string StrikeMessage
    {
        get;
        set;
    }

    protected bool Solved
    {
        get
        {
            return (bool)CommonReflectedTypeInfo.IsSolvedField.GetValue(BombComponent);
        }
    }

    protected bool Detonated
    {
        get
        {
            return (bool)CommonReflectedTypeInfo.HasDetonatedProperty.GetValue(BombCommander.Bomb, null);
        }
    }

    private int _strikeCount = 0;
    protected int StrikeCount
	{
		get
		{
            return _strikeCount;
		}
	}

	protected float FocusDistance
    {
        get
        {
            MonoBehaviour selectable = (MonoBehaviour)BombComponent.GetComponent(_selectableType);
            return (float)_getFocusDistanceMethod.Invoke(selectable, null);
        }
    }

    protected bool FrontFace
    {
        get
        {
            Vector3 componentUp = BombComponent.transform.up;
            Vector3 bombUp = BombCommander.Bomb.transform.up;
            float angleBetween = Vector3.Angle(componentUp, bombUp);
            return angleBetween < 90.0f;
        }
    }

    protected FieldInfo TryCancelField { get; set; }
    protected Type TryCancelComponentSolverType { get; set; }

    protected bool TryCancel
    {
        get
        {
            if (TryCancelField == null || TryCancelComponentSolverType == null ||
                !(TryCancelField.GetValue(TryCancelComponentSolverType) is bool))
                return false;
            return (bool)TryCancelField.GetValue(BombComponent.GetComponent(TryCancelComponentSolverType));
        }
        set
        {
            if (TryCancelField != null && TryCancelComponentSolverType != null &&
                (TryCancelField.GetValue(BombComponent.GetComponent(TryCancelComponentSolverType)) is bool))
                TryCancelField.SetValue(BombComponent.GetComponent(TryCancelComponentSolverType), value);
        }
    }
    #endregion

    #region Private Methods
    private IEnumerator RespondToCommandCommon(string inputCommand, string userNickName)
    {
        if (inputCommand.Equals("unview", StringComparison.InvariantCultureIgnoreCase))
        {
            cameraPriority = ModuleCameras.CameraNotInUse;
            BombMessageResponder.moduleCameras.DetachFromModule(BombComponent);
			_responded = true;
        }
        else
        {
            if (inputCommand.StartsWith("view", StringComparison.InvariantCultureIgnoreCase))
            {
                _responded = true;
                bool pinAllowed = inputCommand.Equals("view pin", StringComparison.InvariantCultureIgnoreCase) &&
                                  (UserAccess.HasAccess(userNickName, AccessLevel.Mod) || modInfo.CameraPinningAlwaysAllowed);

                cameraPriority = (pinAllowed) ? ModuleCameras.CameraPinned : ModuleCameras.CameraPrioritised;
            }
            if ( (BombCommander.multiDecker) || (cameraPriority > ModuleCameras.CameraNotInUse) )
            {
                BombMessageResponder.moduleCameras.AttachToModule(BombComponent, ComponentHandle, Math.Max(cameraPriority, ModuleCameras.CameraInUse));
            }
        }

        if (inputCommand.Equals("show", StringComparison.InvariantCultureIgnoreCase))
		{
			yield return "show";
            yield return null;
        }
    }
    #endregion

    #region Readonly Fields
    protected readonly BombCommander BombCommander = null;
    protected readonly MonoBehaviour BombComponent = null;
    protected readonly MonoBehaviour Selectable = null;
    protected readonly IRCConnection IRCConnection = null;
    public readonly CoroutineCanceller Canceller = null;
    #endregion

    #region Private Static Fields
    private static Type _selectableType = null;
    private static MethodInfo _interactMethod = null;
    private static MethodInfo _interactEndedMethod = null;
    private static MethodInfo _setHighlightMethod = null;
    private static MethodInfo _getFocusDistanceMethod = null;

    private static MethodInfo _onPassInternalMethod = null;
    private static MethodInfo _onStrikeInternalMethod = null;
    #endregion

    #region Private Fields
    private ICommandResponseNotifier _delegatedStrikeResponseNotifier = null;
    private string _delegatedStrikeUserNickName = null;

    private ICommandResponseNotifier _delegatedSolveResponseNotifier = null;
    private string _delegatedSolveUserNickName = null;

    private ICommandResponseNotifier _currentResponseNotifier = null;
    private string _currentUserNickName = null;

    private MusicPlayer _musicPlayer = null;
    #endregion


    public ModuleInformation modInfo = null;
    public int cameraPriority = ModuleCameras.CameraNotInUse;

    public bool _turnQueued = false;
    private bool _readyToTurn = false;
    private bool _processingTwitchCommand = false;
	private bool _responded = false;

	public TwitchComponentHandle ComponentHandle = null;
}
