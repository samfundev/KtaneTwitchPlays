using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public abstract class ComponentSolver
{
	public delegate IEnumerator RegexResponse(Match match);

	#region Constructors
	protected ComponentSolver(BombCommander bombCommander, BombComponent bombComponent)
	{
		BombCommander = bombCommander;
		BombComponent = bombComponent;
		Selectable = bombComponent.GetComponent<Selectable>();

		if (bombCommander != null)
			HookUpEvents();
	}
	#endregion

	#region Interface Implementation
	public IEnumerator RespondToCommand(string userNickName, string message)
	{
		TryCancel = false;
		_responded = false;
		_zoom = false;
		_processingTwitchCommand = true;
		if (Solved)
		{
			_processingTwitchCommand = false;
			yield break;
		}

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

			if (_zoom)
				message = message.Substring(4).Trim();

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
					if (moved && modInfo.DoesTheRightThing)
					{
						//Handle No-focus API commands. In order to focus the module, the first thing yielded cannot be one of the things handled here, as the solver will yield break if
						//it is one of these API commands returned.
						switch (subcoroutine.Current)
						{
							case string currentString:
								if (SendToTwitchChat(currentString, userNickName) && !currentString.StartsWith("strikemessage", StringComparison.InvariantCultureIgnoreCase))
								{
									yield break;
								}
								break;
						}
						_responded = true;
					}
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
					IRCConnection.Instance.SendMessage("Please submit an issue at https://github.com/samfun123/KtaneTwitchPlays/issues regarding module !{0} ({1}) attempting to solve prematurely.", ComponentHandle.Code, ComponentHandle.HeaderText);
					if (modInfo != null)
					{
						modInfo.DoesTheRightThing = false;
						ModuleData.DataHasChanged = true;
						ModuleData.WriteDataToFile();
					}

					IEnumerator focusDefocus = BombCommander.Focus(Selectable, FocusDistance, FrontFace);
					while (focusDefocus.MoveNext())
					{
						yield return focusDefocus.Current;
					}
					yield return new WaitForSeconds(0.5f);

					focusDefocus = BombCommander.Defocus(Selectable, FrontFace);
					while (focusDefocus.MoveNext())
					{
						yield return focusDefocus.Current;
					}
					yield return new WaitForSeconds(0.5f);
				}
				else if (!_responded)
				{
					ComponentHandle.CommandInvalid(userNickName);
				}

				_currentUserNickName = null;
				_processingTwitchCommand = false;
				yield break;
			}
		}

		IEnumerator focusCoroutine = BombCommander.Focus(Selectable, FocusDistance, FrontFace);
		while (focusCoroutine.MoveNext())
		{
			yield return focusCoroutine.Current;
		}

		yield return new WaitForSeconds(0.5f);

		IEnumerator unzoom = null;
		if (_zoom)
		{
			IEnumerator zoom = BombMessageResponder.moduleCameras?.ZoomCamera(BombComponent, 1);
			unzoom = BombMessageResponder.moduleCameras?.UnzoomCamera(BombComponent, 1);
			if (zoom == null || unzoom == null)
			{
				_zoom = false;
			}
			else
			{
				while (zoom.MoveNext())
					yield return zoom.Current;
			}
		}

		int previousStrikeCount = StrikeCount;
		bool parseError = false;
		bool needQuaternionReset = false;
		bool hideCamera = false;
		bool exceptionThrown = false;
		bool trycancelsequence = false;

		while ((previousStrikeCount == StrikeCount && !Solved) || DisableOnStrike)
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
			if (currentValue is string currentString)
			{
				if (currentString.Equals("strike", StringComparison.InvariantCultureIgnoreCase))
				{
					_delegatedStrikeUserNickName = userNickName;
				}
				else if (currentString.Equals("solve", StringComparison.InvariantCultureIgnoreCase))
				{
					_delegatedSolveUserNickName = userNickName;
				}
				else if (currentString.Equals("unsubmittablepenalty", StringComparison.InvariantCultureIgnoreCase))
				{
					if (TwitchPlaySettings.data.UnsubmittablePenaltyPercent <= 0) continue;

					int penalty = Math.Max((int) (modInfo.moduleScore * TwitchPlaySettings.data.UnsubmittablePenaltyPercent), 1);
					Leaderboard.Instance.AddScore(_currentUserNickName, -penalty);
					IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.UnsubmittableAnswerPenalty, _currentUserNickName, Code, modInfo.moduleDisplayName, penalty, penalty > 1 ? "s" : "");
				}
				else if (currentString.Equals("parseerror", StringComparison.InvariantCultureIgnoreCase))
				{
					parseError = true;
					break;
				}
				else if (currentString.RegexMatch(out Match match, "^trycancel((?: (?:.|\\n)+)?)$"))
				{
					if (CoroutineCanceller.ShouldCancel)
					{
						CoroutineCanceller.ResetCancel();
						if (!string.IsNullOrEmpty(match.Groups[1].Value))
							IRCConnection.Instance.SendMessage($"Sorry @{userNickName}, {match.Groups[1].Value.Trim()}");
						break;
					}
				}
				else if (currentString.RegexMatch(out match, "^trycancelsequence((?: (?:.|\\n)+)?)$"))
				{
					trycancelsequence = true;
					yield return currentValue;
					continue;
				}
				else if (currentString.RegexMatch(out match, "^trywaitcancel ([0-9]+(?:\\.[0-9])?)((?: (?:.|\\n)+)?)$") && float.TryParse(match.Groups[1].Value, out float waitCancelTime))
				{
					yield return new WaitForSecondsWithCancel(waitCancelTime, false);
					if (CoroutineCanceller.ShouldCancel)
					{
						CoroutineCanceller.ResetCancel();
						if (!string.IsNullOrEmpty(match.Groups[2].Value))
							IRCConnection.Instance.SendMessage($"Sorry @{userNickName}, {match.Groups[2].Value.Trim()}");
						break;
					}
				}
				// Commands that allow messages to be sent to the chat.
				else if (SendToTwitchChat(currentString, userNickName))
				{
					//handled
				}
				else if (currentString.StartsWith("add strike", StringComparison.InvariantCultureIgnoreCase))
				{
					OnStrike(null);
				}
				else if (currentString.Equals("multiple strikes", StringComparison.InvariantCultureIgnoreCase))
				{
					DisableOnStrike = true;
				}
				else if (currentString.Equals("end multiple strikes", StringComparison.InvariantCultureIgnoreCase))
				{
					if (previousStrikeCount == StrikeCount)
					{
						DisableOnStrike = false;
						if (Solved) OnPass(null);
					}
					else
					{
						break;
					}
				}
				else if (currentString.StartsWith("autosolve", StringComparison.InvariantCultureIgnoreCase))
				{
					HandleModuleException(new Exception(currentString));
					break;
				}
				else if (currentString.ToLowerInvariant().EqualsAny("detonate", "explode"))
				{
					AwardStrikes(_currentUserNickName, BombCommander.StrikeLimit - BombCommander.StrikeCount);
					BombCommander.twitchBombHandle.CauseExplosionByModuleCommand(string.Empty, modInfo.moduleDisplayName);
					break;
				}
				else if (currentString.RegexMatch(out match, "^(end |toggle )?(?:elevator|hold|waiting) music$"))
				{
					if (match.Groups.Count > 1 && _musicPlayer != null)
					{
						_musicPlayer.StopMusic();
						_musicPlayer = null;
					}
					else if (!currentString.StartsWith("end ", StringComparison.InvariantCultureIgnoreCase) && _musicPlayer == null)
					{
						_musicPlayer = MusicPlayer.StartRandomMusic();
					}
				}
				else if (currentString.ToLowerInvariant().Equals("hide camera"))
				{
					if (!hideCamera)
					{
						BombMessageResponder.moduleCameras?.Hide();
						BombMessageResponder.moduleCameras?.HideHUD();
						IEnumerator hideUI = BombCommander.twitchBombHandle.HideMainUIWindow();
						while (hideUI.MoveNext())
						{
							yield return hideUI.Current;
						}
					}
					hideCamera = true;
				}
				else if (currentString.Equals("cancelled", StringComparison.InvariantCultureIgnoreCase) && CoroutineCanceller.ShouldCancel)
				{
					CoroutineCanceller.ResetCancel();
					TryCancel = false;
					break;
				}
				else
				{
					if (TwitchPlaySettings.data.EnableDebuggingCommands)
						DebugHelper.Log($"Unprocessed string: {currentString}");
				}
			}
			else if (currentValue is KMSelectable selectable1)
			{
				if (HeldSelectables.Contains(selectable1))
				{
					DoInteractionEnd(selectable1);
					HeldSelectables.Remove(selectable1);
				}
				else
				{
					DoInteractionStart(selectable1);
					HeldSelectables.Add(selectable1);
				}
			}
			else if (currentValue is IEnumerable<KMSelectable> selectables)
			{
				foreach (KMSelectable selectable in selectables)
				{
					yield return DoInteractionClick(selectable);
					if ((previousStrikeCount != StrikeCount && !DisableOnStrike) || Solved || (trycancelsequence && CoroutineCanceller.ShouldCancel))
						break;
				}
				if (trycancelsequence && CoroutineCanceller.ShouldCancel)
				{
					CoroutineCanceller.ResetCancel();
					break;
				}
			}
			else if (currentValue is Quaternion localQuaternion)
			{
				BombCommander.RotateByLocalQuaternion(localQuaternion);
				//Whitelist perspective pegs as it only returns Quaternion.Euler(x, 0, 0), which is compatible with the RotateCamaraByQuaternion.
				if (BombComponent.GetComponent<KMBombModule>()?.ModuleType.Equals("spwizPerspectivePegs") ?? false)
					BombCommander.RotateCameraByLocalQuaternion(BombComponent, localQuaternion);
				needQuaternionReset = true;
			}
			else if (currentValue is Quaternion[] localQuaternions)
			{
				if (localQuaternions.Length == 2)
				{
					BombCommander.RotateByLocalQuaternion(localQuaternions[0]);
					BombCommander.RotateCameraByLocalQuaternion(BombComponent, localQuaternions[1]);
					needQuaternionReset = true;
				}
			}
			else if (currentValue is string[] currentStrings)
			{
				if (currentStrings.Length >= 1)
				{
					if (currentStrings[0].ToLowerInvariant().EqualsAny("detonate", "explode"))
					{
						AwardStrikes(_currentUserNickName, BombCommander.StrikeLimit - BombCommander.StrikeCount);
						switch (currentStrings.Length)
						{
							case 2:
								BombCommander.twitchBombHandle.CauseExplosionByModuleCommand(currentStrings[1], modInfo.moduleDisplayName);
								break;
							case 3:
								BombCommander.twitchBombHandle.CauseExplosionByModuleCommand(currentStrings[1], currentStrings[2]);
								break;
							default:
								BombCommander.twitchBombHandle.CauseExplosionByModuleCommand(string.Empty, modInfo.moduleDisplayName);
								break;
						}
						break;
					}
				}
			}
			yield return currentValue;

			if (CoroutineCanceller.ShouldCancel)
				TryCancel = true;

			trycancelsequence = false;
		}

		if (!_responded && !exceptionThrown)
		{
			ComponentHandle.CommandInvalid(userNickName);
		}

		if (needQuaternionReset)
		{
			BombCommander.RotateByLocalQuaternion(Quaternion.identity);
			BombCommander.RotateCameraByLocalQuaternion(BombComponent, Quaternion.identity);
		}

		if (hideCamera)
		{
			BombMessageResponder.moduleCameras?.Show();
			BombMessageResponder.moduleCameras?.ShowHUD();
			IEnumerator showUI = BombCommander.twitchBombHandle.ShowMainUIWindow();
			while (showUI.MoveNext())
			{
				yield return showUI.Current;
			}
		}

		if (_musicPlayer != null)
		{
			_musicPlayer.StopMusic();
			_musicPlayer = null;
		}

		if (DisableOnStrike)
		{
			DisableOnStrike = false;
			BombMessageResponder.moduleCameras?.UpdateStrikes(true);
			if (Solved)
				OnPass(null);
			AwardStrikes(_currentUserNickName, StrikeCount - previousStrikeCount);
		}

		if (!parseError)
		{
			yield return new WaitForSeconds(0.5f);
		}

		if (_zoom)
		{
			while (unzoom?.MoveNext() ?? false)
				yield return unzoom.Current;
		}

		IEnumerator defocusCoroutine = BombCommander.Defocus(Selectable, FrontFace);
		while (defocusCoroutine.MoveNext())
		{
			yield return defocusCoroutine.Current;
		}

		yield return new WaitForSeconds(0.5f);

		_currentUserNickName = null;
		_processingTwitchCommand = false;
	}
	#endregion

	#region Abstract Interface
	protected internal abstract IEnumerator RespondToCommandInternal(string inputCommand);
	#endregion

	#region Protected Helper Methods
	protected bool SendToTwitchChat(string message, string userNickName)
	{
		// Within the messages, allow variables:
		// {0} = user’s nickname
		// {1} = Code (module number)
		if (message.RegexMatch(out Match match, @"^senddelayedmessage ([0-9]+(?:\.[0-9])?) (\S(?:\S|\s)*)$") && float.TryParse(match.Groups[1].Value, out float messageDelayTime))
		{
			ComponentHandle.StartCoroutine(SendDelayedMessage(messageDelayTime, string.Format(match.Groups[3].Value, userNickName, ComponentHandle.Code)));
			return true;
		}

		if (!message.RegexMatch(out match, @"^(sendtochat|sendtochaterror|strikemessage|antitroll) +(\S(?:\S|\s)*)$")) return false;

		var chatMsg = string.Format(match.Groups[2].Value, userNickName, ComponentHandle.Code);

		switch (match.Groups[1].Value)
		{
			case "sendtochat":
				IRCConnection.Instance.SendMessage(chatMsg);
				return true;
			case "antitroll":
				if (TwitchPlaySettings.data.EnableTrollCommands) return false;
				goto case "sendtochaterror";
			case "sendtochaterror":
				ComponentHandle.CommandError(userNickName, chatMsg);
				return true;
			case "strikemessage":
				StrikeMessage = chatMsg;
				return true;
			default:
				return false;
		}
	}

	protected IEnumerator SendDelayedMessage(float delay, string message)
	{
		yield return new WaitForSeconds(delay);
		IRCConnection.Instance.SendMessage(message);
	}

	protected void DoInteractionStart(MonoBehaviour interactable)
	{
		interactable.GetComponent<Selectable>().HandleInteract();
	}

	protected void DoInteractionEnd(MonoBehaviour interactable)
	{
		Selectable selectable = interactable.GetComponent<Selectable>();
		selectable.OnInteractEnded();
		selectable.SetHighlight(false);
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

	protected WaitForSeconds DoInteractionClick(MonoBehaviour interactable, float delay) => DoInteractionClick(interactable, null, delay);

	protected WaitForSeconds DoInteractionClick(MonoBehaviour interactable, string strikeMessage = null, float delay = 0.1f)
	{
		if (strikeMessage != null)
		{
			StrikeMessage = strikeMessage;
		}

		if (interactable != null)
		{
			DoInteractionStart(interactable);
			DoInteractionEnd(interactable);
		}
		return new WaitForSeconds(delay);
	}

	protected void HandleModuleException(Exception e)
	{
		DebugHelper.LogException(e, "While solving a module an exception has occurred! Here's the error:");

		SolveModule("Looks like a module ran into a problem while running a command, automatically solving module.");
	}

	protected void SolveModule(string reason = "A module is being automatically solved.", bool removeSolveBasedModules = true)
	{
		IRCConnection.Instance.SendMessage("{0}{1}", reason, removeSolveBasedModules ? " Some other modules may also be solved to prevent problems." : "");
		SolveSilently();
		if (removeSolveBasedModules)
			TwitchComponentHandle.RemoveSolveBasedModules();
	}
	#endregion

	#region Private Methods
	private void HookUpEvents()
	{
		BombComponent.OnPass += OnPass;
		BombComponent.OnStrike += OnStrike;
		KMGameCommands gameCommands = BombComponent.GetComponentInChildren<KMGameCommands>();
		if (gameCommands == null) return;
		gameCommands.OnCauseStrike += x => { OnStrike(x); };
	}

	private bool _silentlySolve;
	private bool OnPass(object _ignore)
	{
		if (DisableOnStrike) return false;
		//string componentType = ComponentHandle.componentType.ToString();
		//string headerText = (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(BombComponent, null);
		if (modInfo != null)
		{
			int moduleScore = modInfo.moduleScore;
			if (modInfo.moduleScoreIsDynamic)
			{
				switch (modInfo.moduleScore)
				{
					case 0:
						switch (modInfo.moduleID) //handle it on a module by module basis, this is to allow for FE to gain 3 times as many points
						{
							case "HexiEvilFMN":
								moduleScore = (int) (BombCommander.bombSolvableModules * 3 * TwitchPlaySettings.data.DynamicScorePercentage);
								break;

							default: //default for forget me not
								moduleScore = (int) (BombCommander.bombSolvableModules * TwitchPlaySettings.data.DynamicScorePercentage);
								break;
						}
						break;
					default:
						moduleScore = modInfo.moduleScore;
						break;
				}
			}

			if (BombComponent is NeedyComponent)
				return false;

			if (UnsupportedModule)
				ComponentHandle?.IDTextUnsupported?.gameObject.SetActive(false);

			string solverNickname = null;
			if (!_silentlySolve)
			{
				if (_delegatedSolveUserNickName != null)
				{
					solverNickname = _delegatedSolveUserNickName;
					_delegatedSolveUserNickName = null;
				}
				else if (_currentUserNickName != null)
				{
					solverNickname = _currentUserNickName;
				}
				else if (ComponentHandle?.PlayerName != null)
				{
					solverNickname = ComponentHandle.PlayerName;
				}
				else
				{
					solverNickname = IRCConnection.Instance.ChannelName;
				}
				AwardSolve(solverNickname, moduleScore);
			}
			ComponentHandle?.OnPass(solverNickname);
		}

		BombCommander.bombSolvedModules++;
		BombMessageResponder.moduleCameras?.UpdateSolves();

		if (_turnQueued)
		{
			DebugHelper.Log("[ComponentSolver] Activating queued turn for completed module {0}.", Code);
			_readyToTurn = true;
			_turnQueued = false;
		}

		BombMessageResponder.moduleCameras?.DetachFromModule(BombComponent, true);
		CommonReflectedTypeInfo.UpdateTimerDisplayMethod.Invoke(BombCommander.timerComponent, null);

		return false;
	}

	public IEnumerator TurnBombOnSolve()
	{
		while (_turnQueued)
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

	public void OnFakeStrike()
	{
		if (_delegatedStrikeUserNickName != null)
		{
			AwardStrikes(_delegatedStrikeUserNickName, 0);
			_delegatedStrikeUserNickName = null;
		}
		else if (_currentUserNickName != null)
		{
			AwardStrikes(_currentUserNickName, 0);
		}
		else if (ComponentHandle.PlayerName != null)
		{
			AwardStrikes(ComponentHandle.PlayerName, 0);
		}
		else
		{
			AwardStrikes(IRCConnection.Instance.ChannelName, 0);
		}
	}

	private bool DisableOnStrike;
	private bool OnStrike(object _ignore)
	{
		//string headerText = (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(BombComponent, null);
		StrikeCount++;

		if (DisableOnStrike) return false;

		if (_delegatedStrikeUserNickName != null)
		{
			AwardStrikes(_delegatedStrikeUserNickName, 1);
			_delegatedStrikeUserNickName = null;
		}
		else if (_currentUserNickName != null)
		{
			AwardStrikes(_currentUserNickName, 1);
		}
		else if (ComponentHandle.PlayerName != null)
		{
			AwardStrikes(ComponentHandle.PlayerName, 1);
		}
		else
		{
			AwardStrikes(IRCConnection.Instance.ChannelName, 1);
		}

		BombMessageResponder.moduleCameras?.UpdateStrikes(true);

		return false;
	}

	protected void PrepareSilentSolve()
	{
		_delegatedSolveUserNickName = null;
		_currentUserNickName = null;
		_silentlySolve = true;
	}

	public void SolveSilently()
	{
		_delegatedSolveUserNickName = null;
		_currentUserNickName = null;
		_silentlySolve = true;
		if (ComponentHandle != null)
			HandleForcedSolve(ComponentHandle);
		else
			HandleForcedSolve(BombComponent);
		OtherModes.DisableLeaderboard(true);
	}

	protected virtual IEnumerator ForcedSolveIEnumerator()
	{
		yield break;
	}

	protected bool HandleForcedSolve()
	{
		_delegatedSolveUserNickName = null;
		_currentUserNickName = null;
		_silentlySolve = true;
		_responded = true;
		IEnumerator forcedSolve = ForcedSolveIEnumerator();
		if (!forcedSolve.MoveNext()) return false;

		CoroutineQueue.AddForcedSolve(ForcedSolveIEnumerator());
		return true;
	}

	public static void HandleForcedSolve(TwitchComponentHandle handle)
	{
		try
		{
			KMBombModule module = handle?.bombComponent.GetComponent<KMBombModule>();
			if (module != null)
			{
				foreach (TwitchComponentHandle h in BombMessageResponder.Instance.ComponentHandles.Where(x => x.bombCommander == handle.bombCommander))
				{
					h.Solver.AddAbandonedModule(module);
				}
			}

			if (!(handle?.Solver?.AttemptedForcedSolve ?? false) && (handle?.Solver?.HandleForcedSolve() ?? false))
			{
				handle.Solver.AttemptedForcedSolve = true;
				return;
			}
			if (!(handle?.Solver?.AttemptedForcedSolve ?? false) && handle?.Solver?.ForcedSolveMethod != null)
			{
				handle.Solver.AttemptedForcedSolve = true;
				handle.Solver._delegatedSolveUserNickName = null;
				handle.Solver._currentUserNickName = null;
				handle.Solver._silentlySolve = true;
				try
				{
					var result = handle.Solver.ForcedSolveMethod.Invoke(handle.Solver.CommandComponent, null);
					if (result is IEnumerator)
						CoroutineQueue.AddForcedSolve((IEnumerator) result);
				}
				catch (Exception ex)
				{
					DebugHelper.LogException(ex, "An exception occured while using the Forced Solve handler:");
					CommonReflectedTypeInfo.HandlePassMethod.Invoke(handle.Solver.BombComponent, null);
					foreach (MonoBehaviour behavior in handle.bombComponent.GetComponentsInChildren<MonoBehaviour>(true))
					{
						behavior.StopAllCoroutines();
					}
				}
			}
			else if (handle?.Solver != null)
			{
				handle.Solver._delegatedSolveUserNickName = null;
				handle.Solver._currentUserNickName = null;
				handle.Solver._silentlySolve = true;
				CommonReflectedTypeInfo.HandlePassMethod.Invoke(handle.bombComponent, null);
				foreach (MonoBehaviour behavior in handle.bombComponent.GetComponentsInChildren<MonoBehaviour>(true))
				{
					behavior.StopAllCoroutines();
				}
			}
			else if (handle != null)
			{
				CommonReflectedTypeInfo.HandlePassMethod.Invoke(handle.bombComponent, null);
				foreach (MonoBehaviour behavior in handle.bombComponent.GetComponentsInChildren<MonoBehaviour>(true))
				{
					behavior.StopAllCoroutines();
				}
			}
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "An exception occured while silently soving a module:");
		}
	}

	public static void HandleForcedSolve(MonoBehaviour bombComponent)
	{
		try
		{
			TwitchComponentHandle handle = null;
			KMBombModule module = bombComponent.GetComponent<KMBombModule>();
			KMNeedyModule needyModule = bombComponent.GetComponent<KMNeedyModule>();
			if (module != null)
			{
				handle = BombMessageResponder.Instance.ComponentHandles.Where(x => x.bombComponent.GetComponent<KMBombModule>() != null)
					.FirstOrDefault(x => x.bombComponent.GetComponent<KMBombModule>() == module);
			}
			else if (needyModule != null)
			{
				handle = BombMessageResponder.Instance.ComponentHandles.Where(x => x.bombComponent.GetComponent<KMBombModule>() != null)
					.FirstOrDefault(x => x.bombComponent.GetComponent<KMBombModule>() == needyModule);
			}
			if (handle != null)
			{
				HandleForcedSolve(handle);
			}
			else
			{
				if (module != null)
				{
					foreach (TwitchComponentHandle h in BombMessageResponder.Instance.ComponentHandles)
					{
						h.Solver.AddAbandonedModule(module);
					}
				}
				CommonReflectedTypeInfo.HandlePassMethod.Invoke(bombComponent, null);
				foreach (MonoBehaviour behaviour in bombComponent.GetComponentsInChildren<MonoBehaviour>(true))
				{
					behaviour.StopAllCoroutines();
				}
			}
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "An exception occured while silently soving a module:");
		}
	}

	private void AwardSolve(string userNickName, int ComponentValue)
	{
		if (OtherModes.ZenModeOn) ComponentValue = (int) Math.Ceiling(ComponentValue * 0.20f);
		if (userNickName == null)
		{
			TwitchPlaySettings.AddRewardBonus(ComponentValue);
		}
		else
		{
			string headerText = UnsupportedModule ? modInfo.moduleDisplayName : BombComponent.GetModuleDisplayName();
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.AwardSolve, Code, userNickName, ComponentValue, headerText);
			string RecordMessageTone = $"Module ID: {Code} | Player: {userNickName} | Module Name: {headerText} | Value: {ComponentValue}";
			Leaderboard.Instance?.AddSolve(userNickName);
			if (!UserAccess.HasAccess(userNickName, AccessLevel.NoPoints))
			{
				Leaderboard.Instance?.AddScore(userNickName, ComponentValue);
			}
			else
			{
				TwitchPlaySettings.AddRewardBonus(ComponentValue);
			}
			TwitchPlaySettings.AppendToSolveStrikeLog(RecordMessageTone);
			TwitchPlaySettings.AppendToPlayerLog(userNickName);
		}
		if (OtherModes.TimeModeOn)
		{
			float time = OtherModes.GetAdjustedMultiplier() * ComponentValue;
			if (time < TwitchPlaySettings.data.TimeModeMinimumTimeGained)
			{
				BombCommander.timerComponent.TimeRemaining = BombCommander.CurrentTimer + TwitchPlaySettings.data.TimeModeMinimumTimeGained;
				IRCConnection.Instance.SendMessage("Bomb time increased by the minimum {0} seconds!", TwitchPlaySettings.data.TimeModeMinimumTimeGained);
			}
			else
			{
				BombCommander.timerComponent.TimeRemaining = BombCommander.CurrentTimer + time;
				IRCConnection.Instance.SendMessage("Bomb time increased by {0} seconds!", Math.Round(time, 1));
			}
			OtherModes.SetMultiplier(OtherModes.GetMultiplier() + TwitchPlaySettings.data.TimeModeSolveBonus);
		}
	}

	private void AwardStrikes(string userNickName, int strikeCount)
	{
		string headerText = UnsupportedModule ? modInfo.moduleDisplayName : BombComponent.GetModuleDisplayName();
		int strikePenalty = modInfo.strikePenalty * (TwitchPlaySettings.data.EnableRewardMultipleStrikes ? strikeCount : 1);
		if (OtherModes.ZenModeOn) strikePenalty = (int) (strikePenalty * 0.20f);
		IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.AwardStrike, Code, strikeCount == 1 ? "a" : strikeCount.ToString(), strikeCount == 1 ? "" : "s", 0, userNickName, string.IsNullOrEmpty(StrikeMessage) ? "" : " caused by " + StrikeMessage, headerText, strikePenalty);
		if (strikeCount <= 0) return;

		string RecordMessageTone = $"Module ID: {Code} | Player: {userNickName} | Module Name: {headerText} | Strike";
		TwitchPlaySettings.AppendToSolveStrikeLog(RecordMessageTone, TwitchPlaySettings.data.EnableRewardMultipleStrikes ? strikeCount : 1);

		int originalReward = TwitchPlaySettings.GetRewardBonus();
		int currentReward = Convert.ToInt32(originalReward * TwitchPlaySettings.data.AwardDropMultiplierOnStrike);
		TwitchPlaySettings.AddRewardBonus(currentReward - originalReward);
		if (currentReward != originalReward)
			IRCConnection.Instance.SendMessage($"Reward {(currentReward > 0 ? "reduced" : "increased")} to {currentReward} points.");
		if (OtherModes.TimeModeOn)
		{
			float originalMultiplier = OtherModes.GetAdjustedMultiplier();
			bool multiDropped = OtherModes.DropMultiplier();
			float multiplier = OtherModes.GetAdjustedMultiplier();
			string tempMessage;
			if (multiDropped)
			{
				if (Mathf.Abs(originalMultiplier - multiplier) >= 0.1)
					tempMessage = "Multiplier reduced to " + Math.Round(multiplier, 1) + " and time";
				else
					tempMessage = "Time";
			}
			else
			{
				tempMessage = $"Multiplier set at {TwitchPlaySettings.data.TimeModeMinMultiplier}, cannot be further reduced.  Time";
			}
			if (BombCommander.CurrentTimer < (TwitchPlaySettings.data.TimeModeMinimumTimeLost / TwitchPlaySettings.data.TimeModeTimerStrikePenalty))
			{
				BombCommander.timerComponent.TimeRemaining = BombCommander.CurrentTimer - TwitchPlaySettings.data.TimeModeMinimumTimeLost;
				tempMessage = tempMessage + $" reduced by {TwitchPlaySettings.data.TimeModeMinimumTimeLost} seconds.";
			}
			else
			{
				float timeReducer = BombCommander.CurrentTimer * TwitchPlaySettings.data.TimeModeTimerStrikePenalty;
				double easyText = Math.Round(timeReducer, 1);
				BombCommander.timerComponent.TimeRemaining = BombCommander.CurrentTimer - timeReducer;
				tempMessage = tempMessage + $" reduced by {Math.Round(TwitchPlaySettings.data.TimeModeTimerStrikePenalty * 100, 1)}%. ({easyText} seconds)";
			}
			IRCConnection.Instance.SendMessage(tempMessage);
			BombCommander.StrikeCount = 0;
			BombMessageResponder.moduleCameras.UpdateStrikes();
		}
		if (OtherModes.ZenModeOn)
		{
			BombCommander.StrikeLimit += strikeCount;
		}

		Leaderboard.Instance.AddScore(userNickName, strikePenalty);
		Leaderboard.Instance.AddStrike(userNickName, strikeCount);
		StrikeMessage = string.Empty;
	}

	protected void AddAbandonedModule(KMBombModule module)
	{
		if (!(AbandonModule?.Contains(module) ?? true))
			AbandonModule?.Add(module);
	}
	#endregion

	public string Code
	{
		get;
		set;
	}

	public bool UnsupportedModule { get; set; } = false;

	#region Protected Properties

	protected string StrikeMessage
	{
		get;
		set;
	}

	protected bool Solved => BombComponent.IsSolved;

	protected bool Detonated => BombCommander.Bomb.HasDetonated;

	protected int StrikeCount { get; private set; } = 0;

	protected float FocusDistance
	{
		get
		{
			Selectable selectable = BombComponent.GetComponent<Selectable>();
			return selectable.GetFocusDistance();
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

	protected FieldInfo AbandonModuleField { get; set; }
	protected List<KMBombModule> AbandonModule
	{
		get
		{
			if (!(AbandonModuleField?.GetValue(AbandonModuleField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType())) is List<KMBombModule>))
				return null;
			return (List<KMBombModule>) AbandonModuleField.GetValue(AbandonModuleField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType()));
		}
		set
		{
			if (AbandonModuleField?.GetValue(AbandonModuleField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType())) is List<KMBombModule>)
				AbandonModuleField.SetValue(AbandonModuleField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType()), value);
		}
	}

	protected FieldInfo TryCancelField { get; set; }
	protected bool TryCancel
	{
		get
		{
			if (!(TryCancelField?.GetValue(TryCancelField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType())) is bool))
				return false;
			return (bool) TryCancelField.GetValue(TryCancelField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType()));
		}
		set
		{
			if (TryCancelField?.GetValue(TryCancelField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType())) is bool)
				TryCancelField.SetValue(TryCancelField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType()), value);
		}
	}
	protected FieldInfo ZenModeField { get; set; }
	protected bool ZenMode
	{
		get
		{
			if (!(ZenModeField?.GetValue(ZenModeField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType())) is bool))
				return false;
			return (bool) ZenModeField.GetValue(ZenModeField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType()));
		}
		set
		{
			if (ZenModeField?.GetValue(ZenModeField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType())) is bool)
				ZenModeField.SetValue(ZenModeField.IsStatic ? null : BombComponent.GetComponent(CommandComponent.GetType()), value);
		}
	}
	#endregion

	#region Private Methods
	private IEnumerator RespondToCommandCommon(string inputCommand, string userNickName)
	{
		inputCommand = inputCommand.Trim();
		if (inputCommand.Equals("unview", StringComparison.InvariantCultureIgnoreCase))
		{
			cameraPriority = ModuleCameras.CameraNotInUse;
			BombMessageResponder.moduleCameras?.DetachFromModule(BombComponent);
			_responded = true;
		}
		else
		{
			if (inputCommand.StartsWith("view", StringComparison.InvariantCultureIgnoreCase))
			{
				_responded = true;
				bool pinAllowed = inputCommand.Equals("view pin", StringComparison.InvariantCultureIgnoreCase) &&
								  (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true) || modInfo.CameraPinningAlwaysAllowed);

				cameraPriority = (pinAllowed) ? ModuleCameras.CameraPinned : ModuleCameras.CameraPrioritised;
			}
			BombMessageResponder.moduleCameras?.AttachToModule(BombComponent, ComponentHandle, Math.Max(cameraPriority, ModuleCameras.CameraInUse));

			if (inputCommand.RegexMatch(out Match match, "^zoom(?: ([0-9]+(?:\\.[0-9])?))?$"))
			{
				if (match.Groups.Count == 1 || !float.TryParse(match.Groups[1].Value, out float delay))
					delay = 2;
				delay = Math.Max(2, delay);
				_zoom = true;
				yield return null;
				if (delay >= 15)
					yield return "elevator music";
				yield return $"trywaitcancel {delay} Your request to hold up the bomb for {delay} seconds has been cut short.";
			}

			if (inputCommand.StartsWith("zoom ", StringComparison.InvariantCultureIgnoreCase))
			{
				_zoom = true;
			}
		}

		if (inputCommand.Equals("show", StringComparison.InvariantCultureIgnoreCase))
		{
			yield return "show";
			yield return null;
		}
		else if (inputCommand.Equals("solve") && ((UserAccess.HasAccess(userNickName, AccessLevel.Admin, true) && !UnsupportedModule) ||
				(UnsupportedModule && GetType() != typeof(UnsupportedModComponentSolver))))
		{
			SolveModule($"A module ({modInfo.moduleDisplayName}) is being automatically solved.", false);
			_responded = true;
		}
	}
	#endregion

	#region Readonly Fields
	protected readonly BombCommander BombCommander = null;
	protected readonly BombComponent BombComponent = null;
	protected readonly Selectable Selectable = null;
	protected readonly HashSet<KMSelectable> HeldSelectables = new HashSet<KMSelectable>();
	#endregion

	#region Private Fields
	private string _delegatedStrikeUserNickName = null;
	private string _delegatedSolveUserNickName = null;
	private string _currentUserNickName = null;

	private MusicPlayer _musicPlayer = null;
	#endregion

	public ModuleInformation modInfo = null;
	public int cameraPriority = ModuleCameras.CameraNotInUse;

	public bool _turnQueued = false;
	private bool _readyToTurn = false;
	private bool _processingTwitchCommand = false;
	private bool _responded = false;
	private bool _zoom = false;
	public bool AttemptedForcedSolve = false;

	public TwitchComponentHandle ComponentHandle = null;
	protected MethodInfo ProcessMethod = null;
	public MethodInfo ForcedSolveMethod = null;
	public Component CommandComponent = null;

}
