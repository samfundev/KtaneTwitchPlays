using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TwitchPlays.ScoreMethods;
using UnityEngine;

public abstract class ComponentSolver
{
	#region Constructors
	protected ComponentSolver(TwitchModule module, bool hookUpEvents = true)
	{
		Module = module;

		if (!hookUpEvents) return;
		module.BombComponent.OnPass += OnPass;

		var previousHandler = module.BombComponent.OnStrike;
		module.BombComponent.OnStrike = _ =>
		{
			OnStrike(_);
			previousHandler(_);

			return false;
		};

		var gameCommands = module.BombComponent.GetComponentInChildren<KMGameCommands>();
		if (gameCommands != null)
			gameCommands.OnCauseStrike += x => OnStrike(x);
	}
	#endregion

	private int _beforeStrikeCount;
	public IEnumerator RespondToCommand(string userNickName, string command)
	{
		_disableAnarchyStrike = TwitchPlaySettings.data.AnarchyMode;

		TryCancel = false;
		_responded = false;
		_processingTwitchCommand = true;
		if (Solved && !TwitchPlaySettings.data.AnarchyMode)
		{
			_processingTwitchCommand = false;
			yield break;
		}

		StrikeMessage = string.Empty;
		StrikeMessageConflict = false;

		Module.CameraPriority |= CameraPriority.Interacted;
		_currentUserNickName = userNickName;
		_beforeStrikeCount = StrikeCount;
		IEnumerator subcoroutine;

		try
		{
			subcoroutine = (ChainableCommands ? ChainCommand(command) : RespondToCommandInternal(command)).Flatten();
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
				if (moved && (!ModInfo.CompatibilityMode || ModInfo.builtIntoTwitchPlays))
				{
					//Handle No-focus API commands. In order to focus the module, the first thing yielded cannot be one of the things handled here, as the solver will yield break if
					//it is one of these API commands returned.
					switch (subcoroutine.Current)
					{
						case string currentString:
							if (SendToTwitchChat(currentString, userNickName) <= SendToTwitchChatResponse.HandledHaltIfUnfocused)
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

		bool select = !Module.BombComponent.GetModuleID().EqualsAny("lookLookAway");

		if (Solved != solved || _beforeStrikeCount != StrikeCount)
		{
			IRCConnection.SendMessageFormat("Warning: Module !{0} ({1}) attempted to {2} in response to a command before Twitch Plays could focus on it. Compatibility mode has been enabled for this module.",
				Module.Code, Module.HeaderText, (Solved != solved) ? "solve" : "strike");

			if (ModInfo != null)
			{
				ModInfo.CompatibilityMode = true;
				ModuleData.DataHasChanged = true;
				ModuleData.WriteDataToFile();
			}

			if (!TwitchPlaySettings.data.AnarchyMode)
			{
				IEnumerator focusDefocus = Module.Bomb.Focus(Module.Selectable, FocusDistance, FrontFace, select);
				while (focusDefocus.MoveNext())
					yield return focusDefocus.Current;

				yield return new WaitForSeconds(0.5f);

				focusDefocus = Module.Bomb.Defocus(Module.Selectable, FrontFace, select);
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

		AppreciateArtComponentSolver.ShowAppreciation(Module);

		IEnumerator focusCoroutine = Module.Bomb.Focus(Module.Selectable, FocusDistance, FrontFace, select);
		while (focusCoroutine.MoveNext())
			yield return focusCoroutine.Current;

		yield return new WaitForSeconds(0.5f);

		bool parseError = false;
		bool needQuaternionReset = false;
		bool hideCamera = false;
		bool exceptionThrown = false;
		bool trycancelsequence = false;
		SendToTwitchChatResponse chatResponse;

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

					int penalty = (int) (Mathf.Max(Module.GetPoints<BaseScore>() * TwitchPlaySettings.data.UnsubmittablePenaltyPercent, 1) * OtherModes.ScoreMultiplier);
					if (penalty == 0)
						continue;

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
							"^trywaitcancel ([0-9]+(?:\\.[0-9]+)?)((?: (?:.|\\n)+)?)$") &&
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
				else if ((chatResponse = SendToTwitchChat(currentString, userNickName)) != SendToTwitchChatResponse.NotHandled)
				{
					if (chatResponse == SendToTwitchChatResponse.HandledMustHalt)
						break; // Antitroll, requested stop with "sendtochat!h", etc.
					// otherwise handled, continue
				}
				else if (currentString.StartsWith("add strike", StringComparison.InvariantCultureIgnoreCase))
					OnStrike(null);
				else if (currentString.Equals("multiple strikes", StringComparison.InvariantCultureIgnoreCase))
					_disableOnStrike = true;
				else if (currentString.Equals("end multiple strikes", StringComparison.InvariantCultureIgnoreCase) && _disableOnStrike)
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
				else if (currentString.StartsWith("nrsolve", StringComparison.InvariantCultureIgnoreCase))
				{
					HandleNRSolve(currentString);
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
						TwitchGame.ModuleCameras?.SetHudVisibility(false);
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
				else if (currentString.RegexMatch(out match, "^(?:skiptime|settime) ([0-9:.]+)$") &&
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
				else if (currentString.RegexMatch(out match, @"^awardpoints(onsolve)? (-?\d+)$") && int.TryParse(match.Groups[2].Value, out int pointsAwarded))
				{
					if (OtherModes.ScoreMultiplier == 0)
						continue;

					var ppaScore = Module.GetPoints<PerAction>();
					pointsAwarded = (ppaScore != 0 ? ppaScore : pointsAwarded * OtherModes.ScoreMultiplier).RoundToInt();

					if (match.Groups[1].Success)
					{
						_delegatedAwardUserNickName = userNickName;
						_pointsToAward = pointsAwarded;
					}
					else
						AwardPoints(_currentUserNickName, pointsAwarded);
				}
				else if (TwitchPlaySettings.data.EnableDebuggingCommands)
					DebugHelper.Log($"Unprocessed string: {currentString}");
			}
			else if (currentValue is KMSelectable selectable1)
			{
				try
				{
					if (HeldSelectables.Contains(selectable1))
					{
						HeldSelectables.Remove(selectable1);
						DoInteractionEnd(selectable1);
					}
					else
					{
						HeldSelectables.Add(selectable1);
						DoInteractionStart(selectable1);
					}
				}
				catch (Exception exception)
				{
					exceptionThrown = true;
					HandleModuleException(exception);
					break;
				}
			}
			else if (currentValue is IEnumerable<KMSelectable> selectables)
			{
				foreach (KMSelectable selectable in selectables)
				{
					WaitForSeconds result = null;
					try
					{
						result = DoInteractionClick(selectable);
					}
					catch (Exception exception)
					{
						exceptionThrown = true;
						HandleModuleException(exception);
						break;
					}

					yield return result;

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
					TwitchBomb.RotateCameraByLocalQuaternion(Module.BombComponent.gameObject, localQuaternion);
				needQuaternionReset = true;
			}
			else if (currentValue is Quaternion[] localQuaternions)
			{
				if (localQuaternions.Length == 2)
				{
					Module.Bomb.RotateByLocalQuaternion(localQuaternions[0]);
					TwitchBomb.RotateCameraByLocalQuaternion(Module.BombComponent.gameObject, localQuaternions[1]);
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
			TwitchBomb.RotateCameraByLocalQuaternion(Module.BombComponent.gameObject, Quaternion.identity);
		}

		if (hideCamera)
		{
			TwitchGame.ModuleCameras?.Show();
			TwitchGame.ModuleCameras?.SetHudVisibility(true);
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

		AppreciateArtComponentSolver.HideAppreciation(Module);

		IEnumerator defocusCoroutine = Module.Bomb.Defocus(Module.Selectable, FrontFace, select);
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
	static string EscapeFormatting(string text)
	{
		int index = -1;
		int count = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '{')
			{
				count++;

				if (count == 1)
					index = i;
			}
			else if (text[i] == '}')
			{
				count--;

				if (count == -1)
					index = i;
			}
		}

		if (index != -1 && count % 2 == 1)
		{
			text = text.Substring(0, index) + (count > 0 ? "{" : "}") + text.Substring(index);
		}

		return text;
	}

	protected enum SendToTwitchChatResponse
	{
		HandledMustHalt,
		HandledHaltIfUnfocused,
		HandledContinue,
		NotHandled
	}

	protected SendToTwitchChatResponse SendToTwitchChat(string message, string userNickName)
	{
		// Default behavior is to halt if the module is unfocused, as it has always been
		SendToTwitchChatResponse instantResponseReturn = SendToTwitchChatResponse.HandledHaltIfUnfocused;
		bool skipFormatting = false;

		// Catch the following flags (which can be combined):
		// !f = Skip formatting
		// !h = Execution must halt
		if (message.RegexMatch(out Match match2, @"^(\w+)!([fh]+) (.+)$"))
		{
			string flagsList = match2.Groups[2].ToString().ToLowerInvariant();
			if (flagsList.Contains('f'))
				skipFormatting = true;
			if (flagsList.Contains('h'))
				instantResponseReturn = SendToTwitchChatResponse.HandledMustHalt;

			message = match2.Groups[1].Value + " " + match2.Groups[3].Value;
		}

		if (!skipFormatting)
			message = EscapeFormatting(message);

		// Within the messages, allow variables:
		// {0} = user’s nickname
		// {1} = Code (module number)
		if (message.RegexMatch(out Match match, @"^senddelayedmessage ([0-9]+(?:\.[0-9]+)?) (\S(?:\S|\s)*)$") && float.TryParse(match.Groups[1].Value, out float messageDelayTime))
		{
			Module.StartCoroutine(SendDelayedMessage(messageDelayTime, skipFormatting ? match.Groups[2].Value : string.Format(match.Groups[2].Value, userNickName, Module.Code)));
			return instantResponseReturn;
		}

		if (!message.RegexMatch(out match, @"^(sendtochat|sendtochaterror|strikemessage|antitroll) +(\S(?:\S|\s)*)$")) return SendToTwitchChatResponse.NotHandled;

		string chatMsg = skipFormatting ? match.Groups[2].Value : string.Format(match.Groups[2].Value, userNickName, Module.Code);

		switch (match.Groups[1].Value)
		{
			case "sendtochat":
				IRCConnection.SendMessage(chatMsg);
				return instantResponseReturn;
			case "antitroll":
				if (TwitchPlaySettings.data.EnableTrollCommands || TwitchPlaySettings.data.AnarchyMode)
					return SendToTwitchChatResponse.HandledContinue;

				// Absolutely ensure that we don't continue executing troll commands.
				Module.CommandError(userNickName, chatMsg);
				return SendToTwitchChatResponse.HandledMustHalt;
			case "sendtochaterror":
				Module.CommandError(userNickName, chatMsg);
				return instantResponseReturn;
			case "strikemessage":
				StrikeMessageConflict |= StrikeCount != _beforeStrikeCount && !string.IsNullOrEmpty(StrikeMessage) && !StrikeMessage.Equals(chatMsg);
				StrikeMessage = chatMsg;
				return SendToTwitchChatResponse.HandledContinue;
			default:
				return SendToTwitchChatResponse.NotHandled;
		}
	}

	protected static IEnumerator SendDelayedMessage(float delay, string message)
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

	protected IEnumerator ChainCommand(string command)
	{
		string[] chainedCommands = command.SplitFull(';', ',')
			.Where(chained => chained.Trim().Length != 0)
			.ToArray();
		if (chainedCommands.Length > 1)
		{
			var commandRoutines = chainedCommands.Select(RespondToCommandInternal).ToArray();
			var invalidCommand = Array.Find(commandRoutines, routine => !routine.MoveNext());
			if (invalidCommand != null)
			{
				yield return "sendtochaterror!f The command \"" + chainedCommands[Array.IndexOf(commandRoutines, invalidCommand)] + "\" is invalid.";
				yield break;
			}

			yield return null;
			foreach (IEnumerator routine in commandRoutines)
				yield return routine;
		}
		else
		{
			var enumerator = RespondToCommandInternal(command);
			while (enumerator.MoveNext())
				yield return enumerator.Current;
		}
	}

	protected static void DoInteractionStart(MonoBehaviour interactable) => interactable.GetComponent<Selectable>().HandleInteract();

	protected static void DoInteractionEnd(MonoBehaviour interactable)
	{
		Selectable selectable = interactable.GetComponent<Selectable>();
		selectable.OnInteractEnded();
		selectable.SetHighlight(false);
	}

	protected static void DoInteractionHighlight(MonoBehaviour interactable) => interactable.GetComponent<Selectable>().SetHighlight(true);

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

	protected IEnumerator SelectIndex(int current, int target, int length, MonoBehaviour increase, MonoBehaviour decrease)
	{
		var difference = target - current;
		if (Math.Abs(difference) > length / 2)
		{
			difference = Math.Abs(difference) - length;

			if (target < current)
				difference = -difference;
		}

		for (int i = 0; i < Math.Abs(difference); i++)
			yield return DoInteractionClick(difference > 0 ? increase : decrease);
	}

	protected void HandleModuleException(Exception e)
	{
		if (Module.BombComponent.GetModuleDisplayName() == "Manometers")
		{
			var j = 0;
			try
			{
				var component = Module.BombComponent.GetComponent(ReflectionHelper.FindType("manometers"));
				var _pressureList = component.GetValue<int[,]>("pressureList");
				DebugHelper.Log(_pressureList.GetLength(0) + " " + _pressureList.GetLength(1));
				for (int i = 0; i < _pressureList.GetLength(0); i++)
				{
					DebugHelper.Log($"Checking value {i} of the list...");
					j++;
					var value1 = _pressureList[i, 0];
					j++;
					var value2 = _pressureList[i, 1];
					j++;
					var value3 = _pressureList[i, 2];
					j = 0;
					DebugHelper.Log($"Values are: {value1}, {value2}, and {value3}");
				}
				DebugHelper.Log("List checks out, problem is elsewhere.");
			}
			catch (Exception ex)
			{
				if (j != 0)
					DebugHelper.Log($"Index at {j - 1} was out of range.");
				DebugHelper.LogException(ex, "While attempting to process an issue with Manometers, an exception has occurred. Here's the error:");
			}
			//Stop the audio from playing, but separate it out from the previous try/catch to identify errors better
			try
			{
				var component = Module.BombComponent.GetComponent(ReflectionHelper.FindType("manometers"));
				var audio = component.GetValue<KMAudio.KMAudioRef>("timerSound");
				if (audio != null)
					audio.StopSound();
			}
			catch
			{
				DebugHelper.Log("Audio for manometers could not be stopped.");
			}
		}
		DebugHelper.LogException(e, $"While solving a module ({Module.BombComponent.GetModuleDisplayName()}) an exception has occurred! Here's the error:");
		SolveModule($"Looks like {Module.BombComponent.GetModuleDisplayName()} ran into a problem while running a command, automatically solving module.");
	}

	protected void HandleNRSolve(string reason)
	{
		reason = reason.Remove(0, 7).Trim();
		string _reason = Module.BombComponent.GetModuleDisplayName() + " has requested to be solved without reward. " + (string.IsNullOrEmpty(reason) ? "No reason was given." : (reason + (!reason.EndsWith(".") ? "." : ""))) + " Solving module.";
		DebugHelper.Log(_reason);
		SolveModule(_reason);
	}

	public void SolveModule(string reason)
	{
		IRCConnection.SendMessage(reason);
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
			if (Module.BombComponent is NeedyComponent)
				return false;

			int moduleScore = (int) Module.ScoreMethods.Sum(method => method.CalculateScore(Module.PlayerName));

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

				if (_delegatedAwardUserNickName != null)
					AwardPoints(_delegatedAwardUserNickName, _pointsToAward);
				AwardSolve(solverNickname, moduleScore);
				AwardRewardBonus();
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

	public void EnableAnarchyStrike() => _disableAnarchyStrike = false;

	private bool _disableOnStrike;
	private bool _disableAnarchyStrike;
	private bool OnStrike(object _)
	{
		//string headerText = (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(BombComponent, null);
		StrikeCount++;

		// This strike handler runs before a strike is assigned, so we need to set the number of strikes to -1 which will then get incremented to 0.
		// Ensure strikes are always set to 0 in VS mode, even for strikes not assigned to a team. (Needies, etc.)
		if (OtherModes.TimeModeOn || OtherModes.VSModeOn)
			Module.Bomb.Bomb.NumStrikes = -1;

		if (OtherModes.Unexplodable)
			Module.Bomb.StrikeLimit++;

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

	public void ForceAwardSolveToNickName(string nickname) => _delegatedSolveUserNickName = nickname;

	protected void PrepareSilentSolve()
	{
		_delegatedSolveUserNickName = null;
		_currentUserNickName = null;
		_silentlySolve = true;
	}

	public void SolveSilently()
	{
		PrepareSilentSolve();
		HandleForcedSolve(Module);
	}

	protected virtual IEnumerator ForcedSolveIEnumerator()
	{
		yield break;
	}

	protected bool HandleForcedSolve()
	{
		PrepareSilentSolve();
		_responded = true;
		IEnumerator forcedSolve = ForcedSolveIEnumerator();
		if (!forcedSolve.MoveNext()) return false;

		CoroutineQueue.AddForcedSolve(EnsureSolve(ForcedSolveIEnumerator(), Module.BombComponent));
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

			if (solver.AttemptedForcedSolve)
			{
				IRCConnection.SendMessage("Forcing the module into a solved state.");
				CommonReflectedTypeInfo.HandlePassMethod.Invoke(bombComponent, null);
				return;
			}

			solver.AttemptedForcedSolve = true;

			if (solver?.HandleForcedSolve() ?? false)
			{
				// The force solve is being handled by a TP solver.
			}
			else if (solver?.ForcedSolveMethod != null)
			{
				// The force solve is being handled by the module's solver.
				solver.AttemptedForcedSolve = true;
				solver._delegatedSolveUserNickName = null;
				solver._currentUserNickName = null;
				solver._silentlySolve = true;
				try
				{
					object result = solver.ForcedSolveMethod.Invoke(solver.CommandComponent, null);
					if (result is IEnumerator enumerator)
						CoroutineQueue.AddForcedSolve(EnsureSolve(enumerator, bombComponent));
				}
				catch (Exception ex)
				{
					DebugHelper.LogException(ex, $"An exception occurred while using the Forced Solve handler ({bombComponent.GetModuleDisplayName()}):");
					CommonReflectedTypeInfo.HandlePassMethod.Invoke(bombComponent, null);
					foreach (MonoBehaviour behavior in bombComponent.GetComponentsInChildren<MonoBehaviour>(true))
					{
						behavior?.StopAllCoroutines();
					}
				}
			}
			else if (handle != null)
			{
				// There is no force solver, just force a pass.
				if (solver != null)
				{
					solver._delegatedSolveUserNickName = null;
					solver._currentUserNickName = null;
					solver._silentlySolve = true;
				}

				CommonReflectedTypeInfo.HandlePassMethod.Invoke(bombComponent, null);
				foreach (MonoBehaviour behavior in bombComponent.GetComponentsInChildren<MonoBehaviour>(true))
				{
					behavior?.StopAllCoroutines();
				}
			}
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, $"An exception occurred while silently solving a module ({handle.BombComponent.GetModuleDisplayName()}):");
		}
	}

	static IEnumerator EnsureSolve(IEnumerator enumerator, BombComponent bombComponent)
	{
		yield return enumerator;
		CommonReflectedTypeInfo.HandlePassMethod.Invoke(bombComponent, null);
	}

	private void AwardSolve(string userNickName, int componentValue)
	{
		List<string> messageParts = new List<string>();

		componentValue = (componentValue * OtherModes.ScoreMultiplier).RoundToInt();
		if (userNickName == null)
			TwitchPlaySettings.AddRewardBonus(componentValue);
		else
		{
			string headerText = UnsupportedModule ? ModInfo.moduleDisplayName : Module.BombComponent.GetModuleDisplayName();
			CalculateVSHP(userNickName, componentValue, out OtherModes.Team? teamDamaged, out int HPDamage);
			if (OtherModes.VSModeOn)
			{
				if (!UnsupportedModule)
				{
					if (componentValue != 0)
						messageParts.Add(string.Format(TwitchPlaySettings.data.AwardVsSolve, Code, userNickName,
							componentValue, headerText, HPDamage,
							teamDamaged == OtherModes.Team.Evil ? "the evil team" : "the good team"));
					else
						messageParts.Add(string.Format(TwitchPlaySettings.data.AwardVsSolveNoPoints, Code, userNickName,
							headerText, HPDamage,
							teamDamaged == OtherModes.Team.Evil ? "the evil team" : "the good team"));
				}
				else if (componentValue != 0)
					messageParts.Add(string.Format(TwitchPlaySettings.data.AwardSolve, Code, userNickName,
						componentValue, headerText));
				else
					messageParts.Add(string.Format(TwitchPlaySettings.data.AwardSolveNoPoints, Code, userNickName, headerText));
			}
			else if (componentValue != 0)
				messageParts.Add(string.Format(TwitchPlaySettings.data.AwardSolve, Code, userNickName, componentValue, headerText));
			else
				messageParts.Add(string.Format(TwitchPlaySettings.data.AwardSolveNoPoints, Code, userNickName, headerText));
			string recordMessageTone = $"Module ID: {Code} | Player: {userNickName} | Module Name: {headerText} | Value: {componentValue}";
			if (!OtherModes.TrainingModeOn) Leaderboard.Instance?.AddSolve(userNickName);
			if (!UserAccess.HasAccess(userNickName, AccessLevel.NoPoints))
				Leaderboard.Instance?.AddScore(userNickName, componentValue);
			else
				TwitchPlaySettings.AddRewardBonus(componentValue);

			if (OtherModes.VSModeOn)
				VSUpdate(teamDamaged, HPDamage);

			TwitchPlaySettings.AppendToSolveStrikeLog(recordMessageTone);
			TwitchPlaySettings.AppendToPlayerLog(userNickName);
		}

		if (OtherModes.TimeModeOn && Module.Bomb.BombSolvedModules < Module.Bomb.BombSolvableModules)
		{
			float time = OtherModes.GetAdjustedMultiplier() * componentValue;
			if (time < TwitchPlaySettings.data.TimeModeMinimumTimeGained)
			{
				Module.Bomb.Bomb.GetTimer().TimeRemaining = Module.Bomb.CurrentTimer + TwitchPlaySettings.data.TimeModeMinimumTimeGained;
				messageParts.Add($"Bomb time increased by the minimum {TwitchPlaySettings.data.TimeModeMinimumTimeGained} seconds!");
			}
			else
			{
				Module.Bomb.Bomb.GetTimer().TimeRemaining = Module.Bomb.CurrentTimer + time;
				messageParts.Add($"Bomb time increased by {Math.Round(time, 1)} seconds!");
			}
			OtherModes.SetMultiplier(OtherModes.GetMultiplier() + TwitchPlaySettings.data.TimeModeSolveBonus);
		}

		IRCConnection.SendMessage(messageParts.Join());
	}

	private void AwardStrikes(int strikeCount) => AwardStrikes(null, strikeCount);

	private void AwardStrikes(string userNickName, int strikeCount)
	{
		List<string> messageParts = new List<string>();
		string headerText = UnsupportedModule ? ModInfo.moduleDisplayName : Module.BombComponent.GetModuleDisplayName();
		int strikePenalty = -TwitchPlaySettings.data.StrikePenalty * (TwitchPlaySettings.data.EnableRewardMultipleStrikes ? strikeCount : 1);
		strikePenalty = (strikePenalty * OtherModes.ScoreMultiplier).RoundToInt();
		bool VSAffect = OtherModes.VSModeOn && !string.IsNullOrEmpty(userNickName);
		OtherModes.Team? teamDamaged = null;
		int HPDamage = 0;
		if (VSAffect)
		{
			CalculateVSHP(userNickName, strikePenalty, out teamDamaged, out HPDamage);
			messageParts.Add(string.Format(TwitchPlaySettings.data.AwardVsStrike, Code,
				strikeCount == 1 ? "a" : strikeCount.ToString(), strikeCount == 1 ? "" : "s", "0", teamDamaged == OtherModes.Team.Good ? "the good team" : "the evil team",
				string.IsNullOrEmpty(StrikeMessage) || StrikeMessageConflict ? "" : " caused by " + StrikeMessage, headerText, HPDamage, strikePenalty, userNickName));
		}
		else
		{
			if (!string.IsNullOrEmpty(userNickName))
				messageParts.Add(string.Format(TwitchPlaySettings.data.AwardStrike, Code,
					strikeCount == 1 ? "a" : strikeCount.ToString(), strikeCount == 1 ? "" : "s", "0", userNickName,
					string.IsNullOrEmpty(StrikeMessage) || StrikeMessageConflict ? "" : " caused by " + StrikeMessage,
					headerText, strikePenalty));
			else
				messageParts.Add(string.Format(TwitchPlaySettings.data.AwardRewardStrike, Code,
					strikeCount == 1 ? "a" : strikeCount.ToString(), strikeCount == 1 ? "" : "s", headerText,
					string.IsNullOrEmpty(StrikeMessage) || StrikeMessageConflict ? "" : " caused by " + StrikeMessage));
		}

		if (strikeCount <= 0) return;

		string recordMessageTone = !string.IsNullOrEmpty(userNickName) ? $"Module ID: {Code} | Player: {userNickName} | Module Name: {headerText} | Strike" : $"Module ID: {Code} | Module Name: {headerText} | Strike";

		TwitchPlaySettings.AppendToSolveStrikeLog(recordMessageTone, TwitchPlaySettings.data.EnableRewardMultipleStrikes ? strikeCount : 1);
		int originalReward = TwitchPlaySettings.GetRewardBonus();
		int currentReward = Convert.ToInt32(originalReward * (1 - (1 - TwitchPlaySettings.data.AwardDropMultiplierOnStrike) * OtherModes.ScoreMultiplier));
		TwitchPlaySettings.AddRewardBonus(currentReward - originalReward);
		if (currentReward != originalReward)
			messageParts.Add($"Reward {(currentReward > 0 ? "reduced" : "increased")} to {currentReward} points.");

		if (VSAffect)
			VSUpdate(teamDamaged, HPDamage);

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
			messageParts.Add(tempMessage);
		}

		if (!string.IsNullOrEmpty(userNickName) && !OtherModes.TrainingModeOn)
		{
			Leaderboard.Instance.AddScore(userNickName, strikePenalty);
			Leaderboard.Instance.AddStrike(userNickName, strikeCount);
		}

		IRCConnection.SendMessage(messageParts.Join());
	}

	private void AwardPoints(string userNickName, int pointsAwarded)
	{
		if (pointsAwarded == 0 || UserAccess.HasAccess(userNickName, AccessLevel.NoPoints) || string.IsNullOrEmpty(userNickName))
			return;
		List<string> messageParts = new List<string>();
		Leaderboard.Instance?.AddScore(userNickName, pointsAwarded);
		if (!OtherModes.VSModeOn)
			messageParts.Add(string.Format(TwitchPlaySettings.data.AwardPPA, userNickName,
				pointsAwarded > 0 ? "awarded" : "deducted", Math.Abs(pointsAwarded), Math.Abs(pointsAwarded) > 1 ? "s" : "",
				Code, ModInfo.moduleDisplayName, pointsAwarded > 0 ? TwitchPlaySettings.data.PosPPAEmote : TwitchPlaySettings.data.NegPPAEmote));
		else
		{
			CalculateVSHP(userNickName, pointsAwarded, out OtherModes.Team? teamDamaged, out int HPDamage);

			messageParts.Add(string.Format(TwitchPlaySettings.data.AwardVSPPA, userNickName,
				pointsAwarded > 0 ? "awarded" : "deducted", Math.Abs(pointsAwarded), Math.Abs(pointsAwarded) > 1 ? "s" : "",
				Code, ModInfo.moduleDisplayName, HPDamage, teamDamaged == OtherModes.Team.Evil ? "the evil team" : "the good team",
				pointsAwarded > 0 ? TwitchPlaySettings.data.PosPPAEmote : TwitchPlaySettings.data.NegPPAEmote));

			VSUpdate(teamDamaged, HPDamage);
		}

		if (TwitchPlaySettings.data.TimeModeTimeForActions && OtherModes.TimeModeOn)
		{
			float time = OtherModes.GetAdjustedMultiplier() * pointsAwarded;
			Module.Bomb.Bomb.GetTimer().TimeRemaining = Module.Bomb.CurrentTimer + time;
			messageParts.Add($"Bomb time increased by {Math.Round(time, 1)} seconds!");
		}

		IRCConnection.SendMessage(messageParts.Join());
	}

	private void CalculateVSHP(string userNickName, int pointsAwarded, out OtherModes.Team? teamDamaged, out int HPDamage)
	{
		HPDamage = Mathf.FloorToInt(Math.Abs(pointsAwarded) * TwitchPlaySettings.data.VSHPMultiplier) == 0 ? 1 : Mathf.FloorToInt(Math.Abs(pointsAwarded) * TwitchPlaySettings.data.VSHPMultiplier);
		var entry = Leaderboard.Instance.GetEntry(userNickName);
		teamDamaged = pointsAwarded > 0 ? (entry.Team == OtherModes.Team.Good ? OtherModes.Team.Evil : OtherModes.Team.Good) : entry.Team;

		switch (teamDamaged)
		{
			case OtherModes.Team.Evil:
				if (OtherModes.GetEvilHealth() < 2 && pointsAwarded < 0)
					HPDamage = 0;
				else if (OtherModes.GetEvilHealth() <= HPDamage && OtherModes.GetEvilHealth() > 1)
					HPDamage = OtherModes.GetEvilHealth() - 1;
				break;
			case OtherModes.Team.Good:
				if (OtherModes.GetGoodHealth() < 2 && pointsAwarded < 0)
					HPDamage = 0;
				if (OtherModes.GetGoodHealth() <= HPDamage && OtherModes.GetGoodHealth() > 1)
					HPDamage = OtherModes.GetGoodHealth() - 1;
				break;
		}
	}

	private void VSUpdate(OtherModes.Team? teamDamaged, int HPDamage)
	{
		if (HPDamage == 0)
			return;
		switch (teamDamaged)
		{
			case OtherModes.Team.Good:
				if (OtherModes.GetGoodHealth() - HPDamage < 1)
				{
					OtherModes.goodHealth = 0;
					TwitchPlaysService.Instance.CoroutineQueue.CancelFutureSubcoroutines();
					TwitchGame.Instance.Bombs[0].CauseVersusExplosion();
					return;
				}
				else
					OtherModes.SubtractGoodHealth(HPDamage);
				break;
			case OtherModes.Team.Evil:
				if (OtherModes.GetEvilHealth() - HPDamage < 1)
				{
					OtherModes.evilHealth = 0;
					GameCommands.SolveBomb();
					return;
				}
				else
					OtherModes.SubtractEvilHealth(HPDamage);
				break;
		}
		TwitchGame.ModuleCameras.UpdateConfidence();
	}

	internal void AwardRewardBonus()
	{
		if (Module.RewardBonusMethods == null)
			return;

		float floatBonus = Module.RewardBonusMethods.Sum(method =>
		{
			var players = method.Players;
			return players.Count == 0 ? method.CalculateScore(null) : players.Sum(method.CalculateScore);
		});

		int rewardBonus = (floatBonus * OtherModes.ScoreMultiplier).RoundToInt();
		if (rewardBonus != 0)
		{
			TwitchPlaySettings.AddRewardBonus(rewardBonus);
			IRCConnection.SendMessage($"Reward increased by {rewardBonus} for defusing module !{Code} ({ModInfo.moduleDisplayName}).");
		}
	}

	protected void ReleaseHeldButtons()
	{
		var copy = HeldSelectables.ToArray();
		HeldSelectables.Clear();

		foreach (var selectable in copy)
		{
			DoInteractionEnd(selectable);
		}
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

	public string LanguageCode;

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

	public string ManualCode
	{
		get
		{
			if (ModInfo.manualCodeOverride) return ModInfo.manualCode;

			var bombComponent = Module.BombComponent;
			return (Repository.GetManual(bombComponent.GetModuleID()) ?? bombComponent.GetModuleDisplayName()) + TranslatedModuleHelper.GetManualCodeAddOn(LanguageCode);
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
				return OtherModes.Unexplodable;
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
	private string _delegatedAwardUserNickName;
	private string _currentUserNickName;
	private int _pointsToAward;

	private MusicPlayer _musicPlayer;
	public ModuleInformation ModInfo = null;
	public bool ChainableCommands = false;

	public bool TurnQueued;
	private bool _readyToTurn;
	private bool _processingTwitchCommand;
	private bool _responded;
	public bool AttemptedForcedSolve;
	private bool _delayedExplosionPending;
	private Coroutine _delayedExplosionCoroutine;

	protected MethodInfo ProcessMethod = null;
	public MethodInfo ForcedSolveMethod = null;
	public Component CommandComponent = null;
	#endregion
}
