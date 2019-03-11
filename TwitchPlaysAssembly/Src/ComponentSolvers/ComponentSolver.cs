using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public abstract class ComponentSolver
{
	#region Constructors
	protected ComponentSolver(TwitchModule module, bool hookUpEvents = true)
	{
		Module = module;

		if (!hookUpEvents) return;
		module.BombComponent.OnPass += OnPass;
		module.BombComponent.OnStrike += OnStrike;
		var gameCommands = module.BombComponent.GetComponentInChildren<KMGameCommands>();
		if (gameCommands != null)
			gameCommands.OnCauseStrike += x => OnStrike(x);
	}
	#endregion

	private int _beforeStrikeCount;
	public IEnumerator RespondToCommand(string userNickName, string command, bool zoom)
	{
		TryCancel = false;
		_responded = false;
		_zoom = zoom;
		_processingTwitchCommand = true;
		if (Solved && !TwitchPlaySettings.data.AnarchyMode)
		{
			_processingTwitchCommand = false;
			yield break;
		}

		Module.CameraPriority = Module.CameraPriority > CameraPriority.Interacted ? Module.CameraPriority : CameraPriority.Interacted;
		_currentUserNickName = userNickName;
		_beforeStrikeCount = StrikeCount;
		IEnumerator subcoroutine;

		try
		{
			subcoroutine = RespondToCommandInternal(command);
		}
		catch (Exception e)
		{
			HandleModuleException(e);
			yield break;
		}

		bool moved = false;
		bool solved = Solved;
		if (subcoroutine != null)
		{
			try
			{
				moved = subcoroutine.MoveNext();
				if (moved && ModInfo.DoesTheRightThing)
				{
					//Handle No-focus API commands. In order to focus the module, the first thing yielded cannot be one of the things handled here, as the solver will yield break if
					//it is one of these API commands returned.
					switch (subcoroutine.Current)
					{
						case string currentString:
							if (SendToTwitchChat(currentString, userNickName) ==
								SendToTwitchChatResponse.InstantResponse)
								yield break;
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

		if (Solved != solved || _beforeStrikeCount != StrikeCount)
		{
			IRCConnection.SendMessageFormat("Please submit an issue at https://github.com/samfun123/KtaneTwitchPlays/issues regarding module !{0} ({1}) attempting to solve prematurely.", Module.Code, Module.HeaderText);
			if (ModInfo != null)
			{
				ModInfo.DoesTheRightThing = false;
				ModuleData.DataHasChanged = true;
				ModuleData.WriteDataToFile();
			}

			if (!TwitchPlaySettings.data.AnarchyMode)
			{
				IEnumerator focusDefocus = Module.Bomb.Focus(Module.Selectable, FocusDistance, FrontFace);
				while (focusDefocus.MoveNext())
					yield return focusDefocus.Current;

				yield return new WaitForSeconds(0.5f);

				focusDefocus = Module.Bomb.Defocus(Module.Selectable, FrontFace);
				while (focusDefocus.MoveNext())
					yield return focusDefocus.Current;

				yield return new WaitForSeconds(0.5f);
				_currentUserNickName = null;
				_processingTwitchCommand = false;
				yield break;
			}
		}

		if (subcoroutine == null || !moved)
		{
			if (!_responded)
				Module.CommandInvalid(userNickName);

			_currentUserNickName = null;
			_processingTwitchCommand = false;
			yield break;
		}

		IEnumerator focusCoroutine = Module.Bomb.Focus(Module.Selectable, FocusDistance, FrontFace);
		while (focusCoroutine.MoveNext())
			yield return focusCoroutine.Current;

		yield return new WaitForSeconds(0.5f);

		IEnumerator unzoomCoroutine = null;
		if (_zoom)
		{
			IEnumerator zoomCoroutine = TwitchGame.ModuleCameras?.ZoomCamera(Module, 1);
			unzoomCoroutine = TwitchGame.ModuleCameras?.UnzoomCamera(Module, 1);
			if (zoomCoroutine == null || unzoomCoroutine == null)
				_zoom = false;
			else
				while (zoomCoroutine.MoveNext())
					yield return zoomCoroutine.Current;
		}

		bool parseError = false;
		bool needQuaternionReset = false;
		bool hideCamera = false;
		bool exceptionThrown = false;
		bool trycancelsequence = false;

		while ((_beforeStrikeCount == StrikeCount && !Solved || _disableOnStrike || TwitchPlaySettings.data.AnarchyMode) && !Detonated)
		{
			try
			{
				if (!subcoroutine.MoveNext())
					break;

				_responded = true;
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
					_delegatedStrikeUserNickName = userNickName;
				else if (currentString.Equals("solve", StringComparison.InvariantCultureIgnoreCase))
					_delegatedSolveUserNickName = userNickName;
				else if (currentString.Equals("unsubmittablepenalty", StringComparison.InvariantCultureIgnoreCase))
				{
					if (TwitchPlaySettings.data.UnsubmittablePenaltyPercent <= 0) continue;

					int penalty =
						Math.Max((int) (ModInfo.moduleScore * TwitchPlaySettings.data.UnsubmittablePenaltyPercent), 1);
					Leaderboard.Instance.AddScore(_currentUserNickName, -penalty);
					IRCConnection.SendMessageFormat(TwitchPlaySettings.data.UnsubmittableAnswerPenalty,
						_currentUserNickName, Code, ModInfo.moduleDisplayName, penalty, penalty > 1 ? "s" : "");
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
							IRCConnection.SendMessage(
								$"Sorry @{userNickName}, {match.Groups[1].Value.Trim()}");
						break;
					}
				}
				else if (currentString.RegexMatch(out match, "^trycancelsequence((?: (?:.|\\n)+)?)$"))
				{
					trycancelsequence = true;
					yield return currentValue;
					continue;
				}
				else if (currentString.RegexMatch(out match,
							"^trywaitcancel ([0-9]+(?:\\.[0-9])?)((?: (?:.|\\n)+)?)$") &&
						float.TryParse(match.Groups[1].Value, out float waitCancelTime))
				{
					yield return new WaitForSecondsWithCancel(waitCancelTime, false, this);
					if (CoroutineCanceller.ShouldCancel)
					{
						CoroutineCanceller.ResetCancel();
						if (!string.IsNullOrEmpty(match.Groups[2].Value))
							IRCConnection.SendMessage($"Sorry @{userNickName}, {match.Groups[2].Value.Trim()}");
						break;
					}
				}
				// Commands that allow messages to be sent to the chat.
				else if (SendToTwitchChat(currentString, userNickName) != SendToTwitchChatResponse.NotHandled)
				{
					if (currentString.StartsWith("antitroll") && !TwitchPlaySettings.data.EnableTrollCommands &&
						!TwitchPlaySettings.data.AnarchyMode)
						break;
					//handled
				}
				else if (currentString.StartsWith("add strike", StringComparison.InvariantCultureIgnoreCase))
					OnStrike(null);
				else if (currentString.Equals("multiple strikes", StringComparison.InvariantCultureIgnoreCase))
					_disableOnStrike = true;
				else if (currentString.Equals("end multiple strikes", StringComparison.InvariantCultureIgnoreCase))
				{
					if (_beforeStrikeCount == StrikeCount && !TwitchPlaySettings.data.AnarchyMode)
					{
						_disableOnStrike = false;
						if (Solved) OnPass(null);
					}
					else if (!TwitchPlaySettings.data.AnarchyMode)
						break;
				}
				else if (currentString.StartsWith("autosolve", StringComparison.InvariantCultureIgnoreCase))
				{
					HandleModuleException(new Exception(currentString));
					break;
				}
				else if (currentString.RegexMatch(out match, "^(?:detonate|explode)(?: ([0-9.]+))?(?: ((?:.|\\n)+))?$"))
				{
					if (!float.TryParse(match.Groups[1].Value, out float explosionTime))
					{
						if (string.IsNullOrEmpty(match.Groups[1].Value))
							explosionTime = 0.1f;
						else
						{
							DebugHelper.Log($"Badly formatted detonate command string: {currentString}");
							yield return currentValue;
							continue;
						}
					}

					_delayedExplosionPending = true;
					if (_delayedExplosionCoroutine != null)
						Module.StopCoroutine(_delayedExplosionCoroutine);
					_delayedExplosionCoroutine = Module.StartCoroutine(DelayedModuleBombExplosion(explosionTime, userNickName, match.Groups[2].Value));
				}
				else if (currentString.RegexMatch(out match, "^cancel (detonate|explode|detonation|explosion)$"))
				{
					_delayedExplosionPending = false;
					if (_delayedExplosionCoroutine != null)
						Module.StopCoroutine(_delayedExplosionCoroutine);
				}
				else if (currentString.RegexMatch(out match, "^(end |toggle )?(?:elevator|hold|waiting) music$"))
				{
					if (match.Groups.Count > 1 && _musicPlayer != null)
					{
						_musicPlayer.StopMusic();
						_musicPlayer = null;
					}
					else if (!currentString.StartsWith("end ", StringComparison.InvariantCultureIgnoreCase) &&
							_musicPlayer == null)
						_musicPlayer = MusicPlayer.StartRandomMusic();
				}
				else if (currentString.EqualsIgnoreCase("hide camera"))
				{
					if (!hideCamera)
					{
						TwitchGame.ModuleCameras?.Hide();
						TwitchGame.ModuleCameras?.HideHud();
						IEnumerator hideUI = Module.Bomb.HideMainUIWindow();
						while (hideUI.MoveNext())
							yield return hideUI.Current;
					}

					hideCamera = true;
				}
				else if (currentString.Equals("cancelled", StringComparison.InvariantCultureIgnoreCase) &&
						CoroutineCanceller.ShouldCancel)
				{
					CoroutineCanceller.ResetCancel();
					TryCancel = false;
					break;
				}
				else if (currentString.RegexMatch(out match, "^(?:skiptime|settime) ([0-9:.]+)") &&
						match.Groups[1].Value.TryParseTime(out float skipTimeTo))
				{
					if (TwitchGame.Instance.Modules.Where(x => x.BombID == Module.BombID && x.BombComponent.IsSolvable && !x.Solved).All(x => x.Solver.SkipTimeAllowed))
					{
						if (ZenMode && Module.Bomb.Bomb.GetTimer().TimeRemaining < skipTimeTo)
							Module.Bomb.Bomb.GetTimer().TimeRemaining = skipTimeTo;

						if (!ZenMode && Module.Bomb.Bomb.GetTimer().TimeRemaining > skipTimeTo)
							Module.Bomb.Bomb.GetTimer().TimeRemaining = skipTimeTo;
					}
				}
				else if (TwitchPlaySettings.data.EnableDebuggingCommands)
					DebugHelper.Log($"Unprocessed string: {currentString}");
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
					if ((_beforeStrikeCount != StrikeCount && !_disableOnStrike || Solved) && !TwitchPlaySettings.data.AnarchyMode || trycancelsequence && CoroutineCanceller.ShouldCancel || Detonated)
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
				Module.Bomb.RotateByLocalQuaternion(localQuaternion);
				//Whitelist perspective pegs as it only returns Quaternion.Euler(x, 0, 0), which is compatible with the RotateCameraByQuaternion.
				if (Module.BombComponent.GetComponent<KMBombModule>()?.ModuleType.Equals("spwizPerspectivePegs") ?? false)
					Module.Bomb.RotateCameraByLocalQuaternion(Module.BombComponent.gameObject, localQuaternion);
				needQuaternionReset = true;
			}
			else if (currentValue is Quaternion[] localQuaternions)
			{
				if (localQuaternions.Length == 2)
				{
					Module.Bomb.RotateByLocalQuaternion(localQuaternions[0]);
					Module.Bomb.RotateCameraByLocalQuaternion(Module.BombComponent.gameObject, localQuaternions[1]);
					needQuaternionReset = true;
				}
			}
			else if (currentValue is string[] currentStrings)
			{
				if (currentStrings.Length >= 1)
				{
					if (currentStrings[0].ToLowerInvariant().EqualsAny("detonate", "explode"))
					{
						AwardStrikes(_currentUserNickName, Module.Bomb.StrikeLimit - Module.Bomb.StrikeCount);
						switch (currentStrings.Length)
						{
							case 2:
								Module.Bomb.CauseExplosionByModuleCommand(currentStrings[1], ModInfo.moduleDisplayName);
								break;
							case 3:
								Module.Bomb.CauseExplosionByModuleCommand(currentStrings[1], currentStrings[2]);
								break;
							default:
								Module.Bomb.CauseExplosionByModuleCommand(string.Empty, ModInfo.moduleDisplayName);
								break;
						}
						break;
					}
				}
			}
			yield return currentValue;

			if (CoroutineCanceller.ShouldCancel)
			{
				if (TwitchPlaySettings.data.AnarchyMode && Solved)
				{
					CoroutineCanceller.ResetCancel();
					break;
				}
				TryCancel = true;
			}

			trycancelsequence = false;
		}

		if (!_responded && !exceptionThrown)
			Module.CommandInvalid(userNickName);

		if (needQuaternionReset)
		{
			Module.Bomb.RotateByLocalQuaternion(Quaternion.identity);
			Module.Bomb.RotateCameraByLocalQuaternion(Module.BombComponent.gameObject, Quaternion.identity);
		}

		if (hideCamera)
		{
			TwitchGame.ModuleCameras?.Show();
			TwitchGame.ModuleCameras?.ShowHud();
			IEnumerator showUI = Module.Bomb.ShowMainUIWindow();
			while (showUI.MoveNext())
				yield return showUI.Current;
		}

		if (_musicPlayer != null)
		{
			_musicPlayer.StopMusic();
			_musicPlayer = null;
		}

		if (_disableOnStrike)
		{
			_disableOnStrike = false;
			TwitchGame.ModuleCameras?.UpdateStrikes(true);
			if (Solved)
				OnPass(null);
			AwardStrikes(_currentUserNickName, StrikeCount - _beforeStrikeCount);
		}
		else if (TwitchPlaySettings.data.AnarchyMode)
		{
			_disableAnarchyStrike = false;
			if (StrikeCount != _beforeStrikeCount)
				AwardStrikes(_currentUserNickName, StrikeCount - _beforeStrikeCount);
		}

		if (!parseError)
			yield return new WaitForSeconds(0.5f);

		if (_zoom && unzoomCoroutine != null)
			while (unzoomCoroutine.MoveNext())
				yield return unzoomCoroutine.Current;

		IEnumerator defocusCoroutine = Module.Bomb.Defocus(Module.Selectable, FrontFace);
		while (defocusCoroutine.MoveNext())
			yield return defocusCoroutine.Current;

		yield return new WaitForSeconds(0.5f);

		_currentUserNickName = null;
		_processingTwitchCommand = false;
	}

	#region Abstract Interface
	protected internal abstract IEnumerator RespondToCommandInternal(string inputCommand);
	#endregion

	#region Protected Helper Methods
	protected enum SendToTwitchChatResponse
	{
		InstantResponse,
		Handled,
		NotHandled
	}

	protected SendToTwitchChatResponse SendToTwitchChat(string message, string userNickName)
	{
		// Within the messages, allow variables:
		// {0} = user’s nickname
		// {1} = Code (module number)
		if (message.RegexMatch(out Match match, @"^senddelayedmessage ([0-9]+(?:\.[0-9])?) (\S(?:\S|\s)*)$") && float.TryParse(match.Groups[1].Value, out float messageDelayTime))
		{
			Module.StartCoroutine(SendDelayedMessage(messageDelayTime, string.Format(match.Groups[3].Value, userNickName, Module.Code)));
			return SendToTwitchChatResponse.InstantResponse;
		}

		if (!message.RegexMatch(out match, @"^(sendtochat|sendtochaterror|strikemessage|antitroll) +(\S(?:\S|\s)*)$")) return SendToTwitchChatResponse.NotHandled;

		string chatMsg = string.Format(match.Groups[2].Value, userNickName, Module.Code);

		switch (match.Groups[1].Value)
		{
			case "sendtochat":
				IRCConnection.SendMessage(chatMsg);
				return SendToTwitchChatResponse.InstantResponse;
			case "antitroll":
				if (TwitchPlaySettings.data.EnableTrollCommands || TwitchPlaySettings.data.AnarchyMode) return SendToTwitchChatResponse.Handled;
				goto case "sendtochaterror";
			case "sendtochaterror":
				Module.CommandError(userNickName, chatMsg);
				return SendToTwitchChatResponse.InstantResponse;
			case "strikemessage":
				StrikeMessageConflict |= StrikeCount != _beforeStrikeCount && !string.IsNullOrEmpty(StrikeMessage) && !StrikeMessage.Equals(chatMsg);
				StrikeMessage = chatMsg;
				return SendToTwitchChatResponse.Handled;
			default:
				return SendToTwitchChatResponse.NotHandled;
		}
	}

	protected IEnumerator SendDelayedMessage(float delay, string message)
	{
		yield return new WaitForSeconds(delay);
		IRCConnection.SendMessage(message);
	}

	protected IEnumerator DelayedModuleBombExplosion(float delay, string userNickName, string chatMessage)
	{
		yield return new WaitForSeconds(delay);
		if (!_delayedExplosionPending) yield break;

		if (!string.IsNullOrEmpty(chatMessage)) SendToTwitchChat($"sendtochat {chatMessage}", userNickName);
		AwardStrikes(userNickName, Module.Bomb.StrikeLimit - Module.Bomb.StrikeCount);
		Module.Bomb.CauseExplosionByModuleCommand(string.Empty, ModInfo.moduleDisplayName);
	}

	protected void DoInteractionStart(MonoBehaviour interactable) => interactable.GetComponent<Selectable>().HandleInteract();

	protected void DoInteractionEnd(MonoBehaviour interactable)
	{
		Selectable selectable = interactable.GetComponent<Selectable>();
		selectable.OnInteractEnded();
		selectable.SetHighlight(false);
	}

	protected string GetModuleType() => Module.BombComponent.GetComponent<KMBombModule>()?.ModuleType ?? Module.BombComponent.GetComponent<KMNeedyModule>()?.ModuleType;

	// ReSharper disable once UnusedMember.Global
	protected WaitForSeconds DoInteractionClick(MonoBehaviour interactable, float delay) => DoInteractionClick(interactable, null, delay);

	protected WaitForSeconds DoInteractionClick(MonoBehaviour interactable, string strikeMessage = null, float delay = 0.1f)
	{
		if (strikeMessage != null)
		{
			StrikeMessageConflict |= StrikeCount != _beforeStrikeCount && !string.IsNullOrEmpty(StrikeMessage) && !StrikeMessage.Equals(strikeMessage);
			StrikeMessage = strikeMessage;
		}

		if (interactable == null) return new WaitForSeconds(delay);
		DoInteractionStart(interactable);
		DoInteractionEnd(interactable);
		return new WaitForSeconds(delay);
	}

	protected void HandleModuleException(Exception e)
	{
		DebugHelper.LogException(e, "While solving a module an exception has occurred! Here's the error:");
		SolveModule($"Looks like {Module.BombComponent.GetModuleDisplayName()} ran into a problem while running a command, automatically solving module.");
	}

	public void SolveModule(string reason)
	{
		IRCConnection.SendMessageFormat("{0}", reason);
		SolveSilently();
	}
	#endregion

	#region Private Methods
	private bool _silentlySolve;
	private bool OnPass(object ignore)
	{
		if (_disableOnStrike) return false;
		if (ModInfo != null)
		{
			int moduleScore = ModInfo.moduleScore;
			if (ModInfo.moduleScoreIsDynamic)
			{
				switch (ModInfo.moduleID)
				{
					case "HexiEvilFMN": // Forget Everything
						moduleScore = (int) (Module.Bomb.bombSolvableModules * 4 * TwitchPlaySettings.data.DynamicScorePercentage);
						break;

					case "cookieJars": //cookie jars
						moduleScore = (int) Mathf.Clamp(Module.Bomb.bombSolvableModules * 0.5f * TwitchPlaySettings.data.DynamicScorePercentage, 1f, float.PositiveInfinity);
						break;

					case "forgetThis": // Forget This
						moduleScore = (int) (Module.Bomb.bombSolvableModules * 3f * TwitchPlaySettings.data.DynamicScorePercentage);
						break;

					default: // Forget Me Not
						moduleScore = (int) (Module.Bomb.bombSolvableModules * TwitchPlaySettings.data.DynamicScorePercentage);
						break;
				}
			}

			if (Module.BombComponent is NeedyComponent)
				return false;

			if (UnsupportedModule)
				Module?.IDTextUnsupported?.gameObject.SetActive(false);

			string solverNickname = null;
			if (!_silentlySolve)
			{
				if (_delegatedSolveUserNickName != null)
				{
					solverNickname = _delegatedSolveUserNickName;
					_delegatedSolveUserNickName = null;
				}
				else if (_currentUserNickName != null)
					solverNickname = _currentUserNickName;
				else if (Module?.PlayerName != null)
					solverNickname = Module.PlayerName;
				else
					solverNickname = IRCConnection.Instance.ChannelName;

				AwardSolve(solverNickname, moduleScore);
			}
			Module?.OnPass(solverNickname);
		}

		TwitchGame.ModuleCameras?.UpdateSolves();

		if (TurnQueued)
		{
			DebugHelper.Log($"[ComponentSolver] Activating queued turn for completed module {Code}.");
			_readyToTurn = true;
			TurnQueued = false;
		}

		TwitchGame.ModuleCameras?.UnviewModule(Module);
		CommonReflectedTypeInfo.UpdateTimerDisplayMethod.Invoke(Module.Bomb.Bomb.GetTimer(), null);
		return false;
	}

	public IEnumerator TurnBombOnSolve()
	{
		while (TurnQueued)
			yield return new WaitForSeconds(0.1f);

		if (!_readyToTurn)
			yield break;

		while (_processingTwitchCommand)
			yield return new WaitForSeconds(0.1f);

		_readyToTurn = false;
		IEnumerator turnCoroutine = Module.Bomb.TurnBomb();
		while (turnCoroutine.MoveNext())
			yield return turnCoroutine.Current;

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
			AwardStrikes(_currentUserNickName, 0);
		else if (Module.PlayerName != null)
			AwardStrikes(Module.PlayerName, 0);
		else
			AwardStrikes(IRCConnection.Instance.ChannelName, 0);
	}

	private bool _disableOnStrike;
	private bool _disableAnarchyStrike;
	private bool OnStrike(object ignore)
	{
		//string headerText = (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(BombComponent, null);
		StrikeCount++;

		if (_disableOnStrike || _disableAnarchyStrike)
		{
			TwitchGame.ModuleCameras?.UpdateStrikes(true);
			return false;
		}

		if (_delegatedStrikeUserNickName != null)
		{
			AwardStrikes(_delegatedStrikeUserNickName, 1);
			_delegatedStrikeUserNickName = null;
		}
		else if (_currentUserNickName != null)
			AwardStrikes(_currentUserNickName, 1);
		else if (Module.PlayerName != null)
			AwardStrikes(Module.PlayerName, 1);
		else
			//AwardStrikes(IRCConnection.Instance.ChannelName, 1); - Instead of striking the streamer, decrease the reward
			AwardStrikes(1);

		TwitchGame.ModuleCameras?.UpdateStrikes(true);

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
		HandleForcedSolve(Module);
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

	public static void HandleForcedSolve(TwitchModule handle)
	{
		try
		{
			BombComponent bombComponent = handle == null ? null : handle.BombComponent;
			ComponentSolver solver = handle == null ? null : handle.Solver;

			KMBombModule module = bombComponent == null ? null : bombComponent.GetComponent<KMBombModule>();
			if (module != null)
			{
				foreach (TwitchModule h in TwitchGame.Instance.Modules.Where(x => x.Bomb == handle.Bomb))
				{
					h.Solver.AddAbandonedModule(module);
				}
			}

			if (!(solver?.AttemptedForcedSolve ?? false) && (solver?.HandleForcedSolve() ?? false))
			{
				solver.AttemptedForcedSolve = true;
				return;
			}
			if (!(solver?.AttemptedForcedSolve ?? false) && solver?.ForcedSolveMethod != null)
			{
				handle.Solver.AttemptedForcedSolve = true;
				handle.Solver._delegatedSolveUserNickName = null;
				handle.Solver._currentUserNickName = null;
				handle.Solver._silentlySolve = true;
				try
				{
					object result = handle.Solver.ForcedSolveMethod.Invoke(handle.Solver.CommandComponent, null);
					if (result is IEnumerator enumerator)
						CoroutineQueue.AddForcedSolve(enumerator);
				}
				catch (Exception ex)
				{
					DebugHelper.LogException(ex, "An exception occurred while using the Forced Solve handler:");
					CommonReflectedTypeInfo.HandlePassMethod.Invoke(handle.Solver.Module, null);
					foreach (MonoBehaviour behavior in handle.BombComponent.GetComponentsInChildren<MonoBehaviour>(true))
					{
						behavior.StopAllCoroutines();
					}
				}
			}
			else if (solver != null)
			{
				solver._delegatedSolveUserNickName = null;
				solver._currentUserNickName = null;
				solver._silentlySolve = true;
				CommonReflectedTypeInfo.HandlePassMethod.Invoke(handle.BombComponent, null);
				foreach (MonoBehaviour behavior in handle.BombComponent.GetComponentsInChildren<MonoBehaviour>(true))
				{
					behavior.StopAllCoroutines();
				}
			}
			else if (handle != null)
			{
				CommonReflectedTypeInfo.HandlePassMethod.Invoke(handle.BombComponent, null);
				foreach (MonoBehaviour behavior in handle.BombComponent.GetComponentsInChildren<MonoBehaviour>(true))
				{
					behavior.StopAllCoroutines();
				}
			}
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "An exception occurred while silently solving a module:");
		}
	}

	private void AwardSolve(string userNickName, int componentValue)
	{
		if (OtherModes.ZenModeOn) componentValue = (int) Math.Ceiling(componentValue * 0.20f);
		if (userNickName == null)
			TwitchPlaySettings.AddRewardBonus(componentValue);
		else
		{
			string headerText = UnsupportedModule ? ModInfo.moduleDisplayName : Module.BombComponent.GetModuleDisplayName();
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AwardSolve, Code, userNickName, componentValue,
				headerText);
			string recordMessageTone =
				$"Module ID: {Code} | Player: {userNickName} | Module Name: {headerText} | Value: {componentValue}";
			Leaderboard.Instance?.AddSolve(userNickName);
			if (!UserAccess.HasAccess(userNickName, AccessLevel.NoPoints))
				Leaderboard.Instance?.AddScore(userNickName, componentValue);
			else
				TwitchPlaySettings.AddRewardBonus(componentValue);

			TwitchPlaySettings.AppendToSolveStrikeLog(recordMessageTone);
			TwitchPlaySettings.AppendToPlayerLog(userNickName);
		}

		if (!OtherModes.TimeModeOn) return;
		float time = OtherModes.GetAdjustedMultiplier() * componentValue;
		if (time < TwitchPlaySettings.data.TimeModeMinimumTimeGained)
		{
			Module.Bomb.Bomb.GetTimer().TimeRemaining = Module.Bomb.CurrentTimer + TwitchPlaySettings.data.TimeModeMinimumTimeGained;
			IRCConnection.SendMessage($"Bomb time increased by the minimum {TwitchPlaySettings.data.TimeModeMinimumTimeGained} seconds!");
		}
		else
		{
			Module.Bomb.Bomb.GetTimer().TimeRemaining = Module.Bomb.CurrentTimer + time;
			IRCConnection.SendMessage($"Bomb time increased by {Math.Round(time, 1)} seconds!");
		}
		OtherModes.SetMultiplier(OtherModes.GetMultiplier() + TwitchPlaySettings.data.TimeModeSolveBonus);
	}

	private void AwardStrikes(int strikeCount) => AwardStrikes(null, strikeCount);

	private void AwardStrikes(string userNickName, int strikeCount)
	{
		string headerText = UnsupportedModule ? ModInfo.moduleDisplayName : Module.BombComponent.GetModuleDisplayName();
		int strikePenalty = ModInfo.strikePenalty * (TwitchPlaySettings.data.EnableRewardMultipleStrikes ? strikeCount : 1);
		if (OtherModes.ZenModeOn) strikePenalty = (int) (strikePenalty * 0.20f);
		if (!string.IsNullOrEmpty(userNickName))
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AwardStrike, Code, strikeCount == 1 ? "a" : strikeCount.ToString(), strikeCount == 1 ? "" : "s", "0", userNickName, string.IsNullOrEmpty(StrikeMessage) || StrikeMessageConflict ? "" : " caused by " + StrikeMessage, headerText, strikePenalty);
		else
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AwardRewardStrike, Code, strikeCount == 1 ? "a" : strikeCount.ToString(), strikeCount == 1 ? "" : "s", headerText, string.IsNullOrEmpty(StrikeMessage) || StrikeMessageConflict ? "" : " caused by " + StrikeMessage);
		if (strikeCount <= 0) return;

		string recordMessageTone = !string.IsNullOrEmpty(userNickName) ? $"Module ID: {Code} | Player: {userNickName} | Module Name: {headerText} | Strike" : $"Module ID: {Code} | Module Name: {headerText} | Strike";

		TwitchPlaySettings.AppendToSolveStrikeLog(recordMessageTone, TwitchPlaySettings.data.EnableRewardMultipleStrikes ? strikeCount : 1);

		int originalReward = TwitchPlaySettings.GetRewardBonus();
		int currentReward = Convert.ToInt32(originalReward * TwitchPlaySettings.data.AwardDropMultiplierOnStrike);
		TwitchPlaySettings.AddRewardBonus(currentReward - originalReward);
		if (currentReward != originalReward)
			IRCConnection.SendMessage($"Reward {(currentReward > 0 ? "reduced" : "increased")} to {currentReward} points.");
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
				tempMessage =
					$"Multiplier set at {TwitchPlaySettings.data.TimeModeMinMultiplier}, cannot be further reduced.  Time";

			if (Module.Bomb.CurrentTimer < (TwitchPlaySettings.data.TimeModeMinimumTimeLost / TwitchPlaySettings.data.TimeModeTimerStrikePenalty))
			{
				Module.Bomb.Bomb.GetTimer().TimeRemaining = Module.Bomb.CurrentTimer - TwitchPlaySettings.data.TimeModeMinimumTimeLost;
				tempMessage += $" reduced by {TwitchPlaySettings.data.TimeModeMinimumTimeLost} seconds.";
			}
			else
			{
				float timeReducer = Module.Bomb.CurrentTimer * TwitchPlaySettings.data.TimeModeTimerStrikePenalty;
				double easyText = Math.Round(timeReducer, 1);
				Module.Bomb.Bomb.GetTimer().TimeRemaining = Module.Bomb.CurrentTimer - timeReducer;
				tempMessage += $" reduced by {Math.Round(TwitchPlaySettings.data.TimeModeTimerStrikePenalty * 100, 1)}%. ({easyText} seconds)";
			}
			IRCConnection.SendMessage(tempMessage);
			Module.Bomb.StrikeCount = 0;
			TwitchGame.ModuleCameras.UpdateStrikes();
		}

		if (OtherModes.ZenModeOn)
			Module.Bomb.StrikeLimit += strikeCount;

		if (!string.IsNullOrEmpty(userNickName))
		{
			Leaderboard.Instance.AddScore(userNickName, strikePenalty);
			Leaderboard.Instance.AddStrike(userNickName, strikeCount);
		}
		StrikeMessage = string.Empty;
		StrikeMessageConflict = false;
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

	protected bool StrikeMessageConflict { get; set; }

	public bool Solved => Module.Solved;

	protected bool Detonated => Module.Bomb.Bomb.HasDetonated;

	public int StrikeCount { get; private set; }

	protected float FocusDistance => Module.FocusDistance;

	protected bool FrontFace => Module.FrontFace;

	protected FieldInfo HelpMessageField { get; set; }
	private string _helpMessage = null;
	public string HelpMessage
	{
		get
		{
			if (!(HelpMessageField?.GetValue(HelpMessageField.IsStatic ? null : CommandComponent) is string))
				return _helpMessage ?? ModInfo.helpText;
			return ModInfo.helpTextOverride
				? ModInfo.helpText
				: (string) HelpMessageField.GetValue(HelpMessageField.IsStatic ? null : CommandComponent);
		}
		protected set
		{
			if (HelpMessageField?.GetValue(HelpMessageField.IsStatic ? null : CommandComponent) is string)
				HelpMessageField.SetValue(HelpMessageField.IsStatic ? null : CommandComponent, value);
			else _helpMessage = value;
		}
	}

	protected FieldInfo ManualCodeField { get; set; }
	private string _manualCode = null;
	public string ManualCode
	{
		get
		{
			if (!(ManualCodeField?.GetValue(ManualCodeField.IsStatic ? null : CommandComponent) is string))
				return _manualCode ?? ModInfo.manualCode;
			return ModInfo.manualCodeOverride
				? ModInfo.manualCode
				: (string) ManualCodeField.GetValue(ManualCodeField.IsStatic ? null : CommandComponent);
		}
		protected set
		{
			if (ManualCodeField?.GetValue(ManualCodeField.IsStatic ? null : CommandComponent) is string)
				ManualCodeField.SetValue(ManualCodeField.IsStatic ? null : CommandComponent, value);
			else _manualCode = value;
		}
	}

	protected FieldInfo SkipTimeField { get; set; }
	private bool _skipTimeAllowed;
	public bool SkipTimeAllowed
	{
		get
		{
			if (!(SkipTimeField?.GetValue(SkipTimeField.IsStatic ? null : CommandComponent) is bool))
				return _skipTimeAllowed;
			return (bool) SkipTimeField.GetValue(SkipTimeField.IsStatic ? null : CommandComponent);
		}
		protected set
		{
			if (SkipTimeField?.GetValue(SkipTimeField.IsStatic ? null : CommandComponent) is bool)
				SkipTimeField.SetValue(SkipTimeField.IsStatic ? null : CommandComponent, value);
			else _skipTimeAllowed = value;
		}
	}

	protected FieldInfo AbandonModuleField { get; set; }
	protected List<KMBombModule> AbandonModule
	{
		get
		{
			if (!(AbandonModuleField?.GetValue(AbandonModuleField.IsStatic ? null : CommandComponent) is List<KMBombModule>))
				return null;
			return (List<KMBombModule>) AbandonModuleField.GetValue(AbandonModuleField.IsStatic ? null : CommandComponent);
		}
		set
		{
			if (AbandonModuleField?.GetValue(AbandonModuleField.IsStatic ? null : CommandComponent) is List<KMBombModule>)
				AbandonModuleField.SetValue(AbandonModuleField.IsStatic ? null : CommandComponent, value);
		}
	}

	protected FieldInfo TryCancelField { get; set; }
	protected bool TryCancel
	{
		get
		{
			if (!(TryCancelField?.GetValue(TryCancelField.IsStatic ? null : CommandComponent) is bool))
				return false;
			return (bool) TryCancelField.GetValue(TryCancelField.IsStatic ? null : CommandComponent);
		}
		set
		{
			if (TryCancelField?.GetValue(TryCancelField.IsStatic ? null : CommandComponent) is bool)
				TryCancelField.SetValue(TryCancelField.IsStatic ? null : CommandComponent, value);
		}
	}
	protected FieldInfo ZenModeField { get; set; }
	protected bool ZenMode
	{
		get
		{
			if (!(ZenModeField?.GetValue(ZenModeField.IsStatic ? null : CommandComponent) is bool))
				return OtherModes.ZenModeOn;
			return (bool) ZenModeField.GetValue(ZenModeField.IsStatic ? null : CommandComponent);
		}
		set
		{
			if (ZenModeField?.GetValue(ZenModeField.IsStatic ? null : CommandComponent) is bool)
				ZenModeField.SetValue(ZenModeField.IsStatic ? null : CommandComponent, value);
		}
	}
	protected FieldInfo TimeModeField { get; set; }
	protected bool TimeMode
	{
		get
		{
			if (!(TimeModeField?.GetValue(TimeModeField.IsStatic ? null : CommandComponent) is bool))
				return OtherModes.TimeModeOn;
			return (bool) TimeModeField.GetValue(TimeModeField.IsStatic ? null : CommandComponent);
		}
		set
		{
			if (TimeModeField?.GetValue(TimeModeField.IsStatic ? null : CommandComponent) is bool)
				TimeModeField.SetValue(TimeModeField.IsStatic ? null : CommandComponent, value);
		}
	}
	protected FieldInfo TwitchPlaysField { get; set; }
	protected bool TwitchPlays
	{
		get
		{
			if (!(TwitchPlaysField?.GetValue(TwitchPlaysField.IsStatic ? null : CommandComponent) is bool))
				return false;
			return (bool) TwitchPlaysField.GetValue(TwitchPlaysField.IsStatic ? null : CommandComponent);
		}
		set
		{
			if (TwitchPlaysField?.GetValue(TwitchPlaysField.IsStatic ? null : CommandComponent) is bool)
				TwitchPlaysField.SetValue(TwitchPlaysField.IsStatic ? null : CommandComponent, value);
		}
	}
	#endregion

	#region Fields
	protected readonly TwitchModule Module = null;
	protected readonly HashSet<KMSelectable> HeldSelectables = new HashSet<KMSelectable>();

	private string _delegatedStrikeUserNickName;
	private string _delegatedSolveUserNickName;
	private string _currentUserNickName;

	private MusicPlayer _musicPlayer;
	public ModuleInformation ModInfo = null;

	public bool TurnQueued;
	private bool _readyToTurn;
	private bool _processingTwitchCommand;
	private bool _responded;
	public bool _zoom;
	public bool AttemptedForcedSolve;
	private bool _delayedExplosionPending;
	private Coroutine _delayedExplosionCoroutine;

	protected MethodInfo ProcessMethod = null;
	public MethodInfo ForcedSolveMethod = null;
	public Component CommandComponent = null;
	#endregion
}