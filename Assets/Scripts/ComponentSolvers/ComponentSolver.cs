using System;
using System.Reflection;
using System.Collections;
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
    public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier)
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
            subcoroutine = RespondToCommandCommon(message);
        }

        if (subcoroutine == null || !subcoroutine.MoveNext())
        {
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
					if (!_responded) IRCConnection.SendMessage(string.Format("Sorry @{0}, that command is invalid.", userNickName));

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
			IRCConnection.SendMessage(string.Format("Sorry @{0}, that command is invalid.", userNickName));
		}

        if (needQuaternionReset)
        {
            BombCommander.RotateByLocalQuaternion(Quaternion.identity);
			BombMessageResponder.moduleCameras.Show();
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

	protected void DoInteractionClick(MonoBehaviour interactable)
	{
		DoInteractionStart(interactable);
		DoInteractionEnd(interactable);
	}

	protected void HandleModuleException(Exception e)
	{
		Debug.Log("[TwitchPlays] While solving a module an exception has occurred! Here's the error:");
		Debug.LogException(e);

		IRCConnection.SendMessage("Looks like a module ran into a problem while running a command, automatically solving module. Some other modules may also be solved to prevent problems.");

		_currentUserNickName = null;
		_delegatedSolveUserNickName = null;
		BombCommander.RemoveSolveBasedModules();
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
        string componentType = ComponentHandle.componentType.ToString();
        if ( (componentType.Length > 4) && (componentType.Substring(0, 5).Equals("Needy")) )
        {
            return false;
        }

        if (_delegatedSolveUserNickName != null && _delegatedSolveResponseNotifier != null)
        {
            AwardSolve(_delegatedSolveUserNickName, _delegatedSolveResponseNotifier);
            _delegatedSolveUserNickName = null;
            _delegatedSolveResponseNotifier = null;
        }
        else if (_currentUserNickName != null && _currentResponseNotifier != null)
        {
            AwardSolve(_currentUserNickName, _currentResponseNotifier);
        }

        BombCommander._bombSolvedModules++;
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

    private void AwardSolve(string userNickName, ICommandResponseNotifier responseNotifier)
    {
        IRCConnection.SendMessage(string.Format("VoteYea Module {0} is solved! +1 solve to {1}", Code, userNickName));
        responseNotifier.ProcessResponse(CommandResponse.EndComplete);
    }

    private void AwardStrikes(string userNickName, ICommandResponseNotifier responseNotifier, int strikeCount)
    {
        IRCConnection.SendMessage(string.Format("VoteNay Module {0} got {1} strike{2}! +{3} strike{2} to {4}{5}", Code, strikeCount == 1 ? "a" : strikeCount.ToString(), strikeCount == 1 ? "" : "s", strikeCount, userNickName, string.IsNullOrEmpty(StrikeMessage) ? "" : " caused by " + StrikeMessage));
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
    private IEnumerator RespondToCommandCommon(string inputCommand)
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
                cameraPriority = (inputCommand.Equals("view pin", StringComparison.InvariantCultureIgnoreCase)) ? ModuleCameras.CameraPinned : ModuleCameras.CameraPrioritised;
				_responded = true;
			}
            if ( (BombCommander._multiDecker) || (cameraPriority > ModuleCameras.CameraNotInUse) )
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
    #endregion
    
    public string helpMessage = null;
    public string manualCode = null;
    public bool statusLightLeft = false;
    public bool statusLightBottom = false;
    public float IDRotation = 0;
    public int cameraPriority = ModuleCameras.CameraNotInUse;

    public bool _turnQueued = false;
    private bool _readyToTurn = false;
    private bool _processingTwitchCommand = false;
	private bool _responded = false;

	public TwitchComponentHandle ComponentHandle = null;
}
