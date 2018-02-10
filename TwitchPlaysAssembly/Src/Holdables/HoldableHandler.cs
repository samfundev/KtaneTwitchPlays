using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract class HoldableHandler
{
	public HoldableHandler(KMHoldableCommander commander, FloatingHoldable holdable)
	{
		HoldableCommander = commander;
		Holdable = holdable;

		if (!BombMessageResponder.BombActive) return;
		KMGameCommands gameCommands = holdable.GetComponent<KMGameCommands>();
		if (gameCommands == null) return;
		gameCommands.OnCauseStrike += OnStrike;
	}

	private void OnStrike(string message)
	{
		StrikeCount++;
		BombMessageResponder.moduleCameras?.UpdateStrikes();
		if (DisableOnStrike) return;
		if (_delegatedStrikeUserNickName != null)
		{
			AwardStrikes(_delegatedStrikeUserNickName, 1);
			_delegatedStrikeUserNickName = null;
		}
		else if (_currentUserNickName != null)
		{
			AwardStrikes(_currentUserNickName, 1);
		}
		Strike = true;
	}

	private void AwardStrikes(string userNickName, int strikeCount)
	{
		BombCommander BombCommander = BombMessageResponder.Instance.BombCommanders[0];
		int strikePenalty = -5;
		strikePenalty = TwitchPlaySettings.data.EnableRewardMultipleStrikes ? (strikeCount * strikePenalty) : (Math.Min(strikePenalty, strikeCount * strikePenalty));

		IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.AwardHoldableStrike, 
			HoldableCommander.ID, 
			strikeCount == 1 ? "a" : strikeCount.ToString(), 
			strikeCount == 1 ? "" : "s",
			strikePenalty, 
			userNickName, 
			string.IsNullOrEmpty(StrikeMessage) ? "" : " caused by " + StrikeMessage);
		if (strikeCount <= 0) return;

		string RecordMessageTone = $"Holdable ID: {HoldableCommander.ID} | Player: {userNickName} | Strike";
		TwitchPlaySettings.AppendToSolveStrikeLog(RecordMessageTone, TwitchPlaySettings.data.EnableRewardMultipleStrikes ? strikeCount : 1);

		int originalReward = TwitchPlaySettings.GetRewardBonus();
		int currentReward = Convert.ToInt32(originalReward * TwitchPlaySettings.data.AwardDropMultiplierOnStrike);
		TwitchPlaySettings.SetRewardBonus(currentReward);
		if(currentReward != originalReward)
			IRCConnection.Instance.SendMessage($"Reward {(currentReward > 0 ? "reduced" : "increased")} to {currentReward} points.");
		if (OtherModes.timedModeOn)
		{
			bool multiDropped = OtherModes.dropMultiplier();
			float multiplier = OtherModes.getMultiplier();
			string tempMessage;
			if (multiDropped)
			{
				tempMessage = "Multiplier reduced to " + Math.Round(multiplier, 1) + " and time";
			}
			else
			{
				tempMessage = $"Mutliplier set at {TwitchPlaySettings.data.TimeModeMinMultiplier}, cannot be further reduced.  Time";
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
			BombMessageResponder.moduleCameras?.UpdateStrikes();
		}

		Leaderboard.Instance?.AddScore(userNickName, strikePenalty);
		Leaderboard.Instance?.AddStrike(userNickName, strikeCount);
		StrikeMessage = string.Empty;
	}

	private IEnumerator FirstItem(object item)
	{
		yield return item;
	}
	
	public IEnumerator RespondToCommand(string userNickName, string message)
	{
		DisableOnStrike = false;
		Strike = false;
		StrikeCount = 0;
		_currentUserNickName = userNickName;

		FloatingHoldable.HoldStateEnum holdState = Holdable.HoldState;

		if (holdState == FloatingHoldable.HoldStateEnum.Held)
		{}
		else
		{
			IEnumerator holdCoroutine = HoldableCommander.Hold();
			while (holdCoroutine.MoveNext() && !Strike)
			{
				yield return holdCoroutine.Current;
			}
		}

		IEnumerator processCommand = RespondToCommandCommon(message);
		if (processCommand == null || !processCommand.MoveNext())
		{	
			DebugHelper.Log("Running RespondToCommandInternal()");
			processCommand = RespondToCommandInternal(message);
			bool cancelled = false;
			bool parseError = false;
			bool cancelling = false;

			if (processCommand == null || !processCommand.MoveNext())
			{
				if(!Strike)
				{
					SendToChat(null, userNickName, ref parseError);
				}
			}
			else
			{
				processIEnumerators.Push(processCommand);
				processIEnumerators.Push(FirstItem(processCommand.Current));
			}

			do
			{
				try
				{
					bool result = false;
					while (!result && !Strike)
					{
						if (processIEnumerators.Count > 0)
						{
							processCommand = processIEnumerators.Pop();
							result = processCommand.MoveNext();
							if (result)
								processIEnumerators.Push(processCommand);
						}
						else
						{
							break;
						}
					}
					if (Strike)
						DebugHelper.Log("A strike was caused by the command. Invocation will not continue.");
					if (!result || Strike) break;

				}
				catch (Exception ex)
				{
					DebugHelper.LogException(ex, "Error Processing command due to an exception. Invocation will not continue.:");
					break;
				}
				if (processCommand.Current != null)
				{
					DebugHelper.Log(processCommand.Current.GetType().FullName);
				}

				switch (processCommand.Current)
				{
					case IEnumerator iEnumerator:
						if (iEnumerator != null)
							processIEnumerators.Push(iEnumerator);
						continue;
					case KMSelectable kmSelectable when kmSelectable != null:
						if (heldSelectables.Contains(kmSelectable))
						{
							DoInteractionEnd(kmSelectable);
							heldSelectables.Remove(kmSelectable);
						}
						else
						{
							DoInteractionStart(kmSelectable);
							heldSelectables.Add(kmSelectable);
						}
						break;

					case KMSelectable[] kmSelectables:
						foreach (KMSelectable selectable in kmSelectables)
						{
							if (selectable != null)
							{
								yield return DoInteractionClick(selectable);
								if (Strike) break;
							}
							else
							{
								yield return new WaitForSeconds(0.1f);
							}
						}
						break;

					case Quaternion quaternion:
						HoldableCommander.RotateByLocalQuaternion(quaternion);
						break;

					case string currentString when !string.IsNullOrEmpty(currentString):
						DebugHelper.Log(currentString);
						if (currentString.Equals("trycancel", StringComparison.InvariantCultureIgnoreCase) &&
						    CoroutineCanceller.ShouldCancel)
						{
							CoroutineCanceller.ResetCancel();
							cancelled = true;
						}
						else if (currentString.ToLowerInvariant().EqualsAny("elevator music", "hold music", "waiting music"))
						{
							if (_musicPlayer == null)
							{
								_musicPlayer = MusicPlayer.StartRandomMusic();
							}
						}
						else if (currentString.ToLowerInvariant().Equals("cancelled") && cancelling)
						{
							CancelBool?.SetValue(CommandComponent, false);
							CoroutineCanceller.ResetCancel();
							cancelled = true;
						}
						else if (currentString.StartsWith("strikemessage ", StringComparison.InvariantCultureIgnoreCase) &&
						         currentString.Substring(14).Trim() != string.Empty)
						{
							StrikeMessage = currentString.Substring(14);
						}
						else if (currentString.Equals("strike", StringComparison.InvariantCultureIgnoreCase))
						{
							_delegatedStrikeUserNickName = _currentUserNickName;
						}
						else if (currentString.Equals("multiple strikes", StringComparison.InvariantCultureIgnoreCase))
						{
							DisableOnStrike = true;
						}
						else if (currentString.ToLowerInvariant().EqualsAny("detonate", "explode") && BombMessageResponder.BombActive)
						{
							BombCommander BombCommander = BombMessageResponder.Instance.BombCommanders[0];
							AwardStrikes(_currentUserNickName, BombCommander.StrikeLimit - BombCommander.StrikeCount);
							BombCommander.twitchBombHandle.CauseExplosionByModuleCommand(string.Empty, HoldableCommander.ID);
							Strike = true;
						}
						else
						{
							SendToChat(currentString, userNickName, ref parseError);
						}
						break;
					case string[] currentStrings:
						if (currentStrings.Length >= 1)
						{
							if (currentStrings[0].ToLowerInvariant().EqualsAny("detonate", "explode") && BombMessageResponder.BombActive)
							{
								BombCommander BombCommander = BombMessageResponder.Instance.BombCommanders[0];
								AwardStrikes(_currentUserNickName, BombCommander.StrikeLimit - BombCommander.StrikeCount);
								switch (currentStrings.Length)
								{
									case 2:
										BombCommander.twitchBombHandle.CauseExplosionByModuleCommand(currentStrings[1], HoldableCommander.ID);
										break;
									case 3:
										BombCommander.twitchBombHandle.CauseExplosionByModuleCommand(currentStrings[1], currentStrings[2]);
										break;
									default:
										BombCommander.twitchBombHandle.CauseExplosionByModuleCommand(string.Empty, HoldableCommander.ID);
										break;
								}
								break;
							}
						}
						break;
					case object[] objects:
						DebugHelper.Log("processing objects[]");
						if (objects == null) break;
						switch (objects.Length)
						{
							case 3 when objects[0] is string objstr && objects[1] is Action actionTrue:
								DebugHelper.Log("objects[] is a request for permission to perform the next action");
								switch (objstr)
								{
									case "streamer" when UserAccess.HasAccess(userNickName, AccessLevel.Streamer, true):
									case "streamer-only" when UserAccess.HasAccess(userNickName, AccessLevel.Streamer):
									case "superuser" when UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true):
									case "superuser-only" when UserAccess.HasAccess(userNickName, AccessLevel.SuperUser):
									case "admin" when UserAccess.HasAccess(userNickName, AccessLevel.Admin, true):
									case "admin-only" when UserAccess.HasAccess(userNickName, AccessLevel.Admin):
									case "mod" when UserAccess.HasAccess(userNickName, AccessLevel.Mod, true):
									case "mod-only" when UserAccess.HasAccess(userNickName, AccessLevel.Mod):
									case "defuser" when UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true):
									case "defuser-only" when UserAccess.HasAccess(userNickName, AccessLevel.Defuser):
										DebugHelper.Log("Permission was granted");
										actionTrue.Invoke();
										break;
									default:
										DebugHelper.Log("Permission was denied");
										switch (objects[2])
										{
											case Action actionFalse:
												DebugHelper.Log("Invoking the permission denied action");
												actionFalse.Invoke();
												break;
											case string objStr2 when !string.IsNullOrEmpty(objStr2):
												DebugHelper.Log("Sending to chat the permission denied string");
												SendToChat(objStr2, userNickName, ref parseError);
												break;
											case IEnumerator iEnumerator when iEnumerator != null:
												DebugHelper.Log("Resuming execution through the permission denied Enumerator");
												processIEnumerators.Push(iEnumerator);
												yield return null;
												continue;
										}
										break;
								}
								break;
						}
						break;

					default:
						break;
				}
				
				yield return processCommand.Current;

				if (CoroutineCanceller.ShouldCancel && !cancelling && CommandComponent != null)
				{
					CancelBool?.SetValue(CommandComponent, true);
					cancelling = CancelBool != null;
				}
				
			} while (processIEnumerators.Count > 0 && !parseError && !cancelled && !Strike);
			processIEnumerators.Clear();
			if (_musicPlayer != null)
			{
				_musicPlayer.StopMusic();
				_musicPlayer = null;
			}

			if (DisableOnStrike)
			{
				AwardStrikes(userNickName, StrikeCount);
			}
			DebugHelper.Log("RespondToCommandInternal() Complete");
		}
		else
		{	// running RespondToCommandCommon()	
			DebugHelper.Log("Running RespondToCommandCommon()");
			do
			{
				yield return processCommand.Current;
			} while (processCommand.MoveNext());
			DebugHelper.Log("RespondToCommandCommon() complete");
		}

		yield break;
	}

	private void SendToChat(string message, string userNickname, ref bool parseerror)
	{
		if (string.IsNullOrEmpty(message))
		{
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.HoldableInvalidCommand, HoldableCommander.ID, userNickname);
			return;
		}
		if (message.StartsWith("sendtochat ", StringComparison.InvariantCultureIgnoreCase) && message.Substring(11).Trim() != string.Empty)
		{
			IRCConnection.Instance.SendMessage(message.Substring(11), HoldableCommander.ID, userNickname);
			return;
		}
		if (message.StartsWith("sendtochaterror ", StringComparison.InvariantCultureIgnoreCase) && message.Substring(16).Trim() != string.Empty)
		{
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.HoldableCommandError, HoldableCommander.ID, userNickname, message.Substring(16));
			parseerror = true;
			return;
		}
		if (!message.Equals("parseerror", StringComparison.InvariantCultureIgnoreCase)) return;
		IRCConnection.Instance.SendMessage($"Sorry @{userNickname}, there was an error parsing the command for !{HoldableCommander.ID}");
		parseerror = true;
	}

	protected abstract IEnumerator RespondToCommandInternal(string command);

	protected IEnumerator RespondToCommandCommon(string command)
	{
		yield break;
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

	protected WaitForSeconds DoInteractionClick(MonoBehaviour interactable, float delay) => DoInteractionClick(interactable, null, delay);

	protected WaitForSeconds DoInteractionClick(MonoBehaviour interactable, string strikeMessage = null, float delay = 0.1f)
	{
		if (strikeMessage != null)
		{
			StrikeMessage = strikeMessage;
		}

		DoInteractionStart(interactable);
		DoInteractionEnd(interactable);
		return new WaitForSeconds(delay);
	}

	public void ShowHelp()
	{
		if (!string.IsNullOrEmpty(HelpMessage))
			IRCConnection.Instance.SendMessage(HelpMessage, HoldableCommander.ID);
	}


	private string _delegatedStrikeUserNickName = null;
	private string _currentUserNickName = null;

	

	private bool DisableOnStrike;
	protected bool Strike { get; private set; }
	protected int StrikeCount { get; private set; }
	protected string StrikeMessage { get; set; }

	protected Component CommandComponent;
	protected MethodInfo HandlerMethod;
	protected FieldInfo CancelBool;
	protected string UserNickName;
	private MusicPlayer _musicPlayer;

	public string HelpMessage;
	public KMHoldableCommander HoldableCommander;
	public FloatingHoldable Holdable;
	protected HashSet<KMSelectable> heldSelectables = new HashSet<KMSelectable>();
	protected Stack<IEnumerator> processIEnumerators = new Stack<IEnumerator>();
}
