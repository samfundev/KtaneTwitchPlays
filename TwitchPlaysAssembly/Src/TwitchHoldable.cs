using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TwitchHoldable
{
	public TwitchHoldable(FloatingHoldable holdable, Type commandType = null, bool allowModded = false)
	{
		Holdable = holdable;
		CommandType = commandType;
		_defaultId = holdable.name.ToLowerInvariant().Replace("(clone)", "");

		if (TwitchGame.BombActive)
		{
			var gameCommands = holdable.GetComponent<KMGameCommands>();
			if (gameCommands != null)
				gameCommands.OnCauseStrike += OnStrike;
		}

		if (allowModded)
		{
			// Find a suitable ProcessTwitchCommand method
			foreach (var component in holdable.GetComponentsInChildren<Component>(true))
			{
				var type = component.GetType();
				var candidateMethod = type.GetMethod("ProcessTwitchCommand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (candidateMethod == null)
					continue;
				if (ValidateMethodCommandMethod(type, candidateMethod))
				{
					HandlerMethod = candidateMethod;
					CommandComponent = component;

					HelpMessageField = type.GetField("TwitchHelpMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

					// Find a suitable TwitchShouldCancelCommand boolean field
					FieldInfo f;
					if ((f = type.GetField("TwitchShouldCancelCommand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) != null && f.FieldType == typeof(bool))
						CancelBool = f;
					break;
				}
			}

			if (HandlerMethod == null)
			{
				// No method found.
				LogAllComponentTypes(holdable);
			}
		}
	}

	private void OnStrike(string message)
	{
		StrikeCount++;
		if (TwitchGame.ModuleCameras != null)
			TwitchGame.ModuleCameras.UpdateStrikes();
		if (_disableOnStrike) return;
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
		TwitchBomb bomb = TwitchGame.Instance.Bombs[0];
		int strikePenalty = -5;
		strikePenalty = TwitchPlaySettings.data.EnableRewardMultipleStrikes ? (strikeCount * strikePenalty) : (Math.Min(strikePenalty, strikeCount * strikePenalty));

		IRCConnection.SendMessage(TwitchPlaySettings.data.AwardHoldableStrike,
			ID,
			strikeCount == 1 ? "a" : strikeCount.ToString(),
			strikeCount == 1 ? "" : "s",
			strikePenalty,
			userNickName,
			string.IsNullOrEmpty(StrikeMessage) ? "" : " caused by " + StrikeMessage);
		if (strikeCount <= 0) return;

		string recordMessageTone = $"Holdable ID: {ID} | Player: {userNickName} | Strike";
		TwitchPlaySettings.AppendToSolveStrikeLog(recordMessageTone, TwitchPlaySettings.data.EnableRewardMultipleStrikes ? strikeCount : 1);

		int originalReward = TwitchPlaySettings.GetRewardBonus();
		int currentReward = Convert.ToInt32(originalReward * TwitchPlaySettings.data.AwardDropMultiplierOnStrike);
		TwitchPlaySettings.AddRewardBonus(currentReward - originalReward);
		if (currentReward != originalReward)
			IRCConnection.SendMessage($"Reward {(currentReward > 0 ? "reduced" : "increased")} to {currentReward} points.");
		if (OtherModes.TimeModeOn)
		{
			bool multiDropped = OtherModes.DropMultiplier();
			float multiplier = OtherModes.GetMultiplier();
			string tempMessage;
			if (multiDropped)
			{
				tempMessage = "Multiplier reduced to " + Math.Round(multiplier, 1) + " and time";
			}
			else
			{
				tempMessage = $"Multiplier set at {TwitchPlaySettings.data.TimeModeMinMultiplier}, cannot be further reduced.  Time";
			}
			if (bomb.CurrentTimer < (TwitchPlaySettings.data.TimeModeMinimumTimeLost / TwitchPlaySettings.data.TimeModeTimerStrikePenalty))
			{
				bomb.Bomb.GetTimer().TimeRemaining = bomb.CurrentTimer - TwitchPlaySettings.data.TimeModeMinimumTimeLost;
				tempMessage = tempMessage + $" reduced by {TwitchPlaySettings.data.TimeModeMinimumTimeLost} seconds.";
			}
			else
			{
				float timeReducer = bomb.CurrentTimer * TwitchPlaySettings.data.TimeModeTimerStrikePenalty;
				double easyText = Math.Round(timeReducer, 1);
				bomb.Bomb.GetTimer().TimeRemaining = bomb.CurrentTimer - timeReducer;
				tempMessage = tempMessage + $" reduced by {Math.Round(TwitchPlaySettings.data.TimeModeTimerStrikePenalty * 100, 1)}%. ({easyText} seconds)";
			}
			IRCConnection.SendMessage(tempMessage);
			bomb.StrikeCount = 0;
			if (TwitchGame.ModuleCameras != null)
				TwitchGame.ModuleCameras.UpdateStrikes();
		}

		Leaderboard.Instance?.AddScore(userNickName, strikePenalty);
		Leaderboard.Instance?.AddStrike(userNickName, strikeCount);
		StrikeMessage = string.Empty;
	}

	private static IEnumerator FirstItem(object item)
	{
		yield return item;
	}

	private static IEnumerator BlankIEnumerator()
	{
		yield break;
	}

	public IEnumerator RespondToCommand(string userNickName, bool isWhisper) => RespondToCommand(userNickName, "", isWhisper, BlankIEnumerator());

	public IEnumerator RespondToCommand(string userNickName, string cmdStr, bool isWhisper, IEnumerator processCommand = null)
	{
		if (HandlerMethod == null && processCommand == null)
		{
			IRCConnection.SendMessage(@"Sorry @{0}, this holdable is not supported by Twitch Plays.", userNickName, !isWhisper, userNickName);
			yield break;
		}

		_disableOnStrike = false;
		Strike = false;
		StrikeCount = 0;
		_currentUserNickName = userNickName;

		FloatingHoldable.HoldStateEnum holdState = Holdable.HoldState;

		if (holdState != FloatingHoldable.HoldStateEnum.Held)
		{
			IEnumerator holdCoroutine = Hold();
			while (holdCoroutine.MoveNext() && !Strike)
				yield return holdCoroutine.Current;
		}

		DebugHelper.Log("Running RespondToCommandInternal()");
		if(HandlerMethod != null && processCommand == null)
			processCommand = MakeCoroutine(HandlerMethod.Invoke(CommandComponent, new object[] {cmdStr}));

		bool cancelled = false;
		bool parseError = false;
		bool cancelling = false;

		if (processCommand == null || !processCommand.MoveNext())
		{
			if (!Strike)
				SendToChat(null, userNickName, isWhisper, ref parseError);
		}
		else
		{
			ProcessIEnumerators.Push(processCommand);
			ProcessIEnumerators.Push(FirstItem(processCommand.Current));
		}

		do
		{
			try
			{
				bool result = false;
				while (!result && !Strike)
				{
					if (ProcessIEnumerators.Count > 0)
					{
						processCommand = ProcessIEnumerators.Pop();
						result = processCommand.MoveNext();
						if (result)
							ProcessIEnumerators.Push(processCommand);
					}
					else
						break;
				}
				if (Strike)
					DebugHelper.Log("A strike was caused by the command. Invocation will not continue.");
				if (!result || Strike) break;
			}
			catch (Exception ex)
			{
				DebugHelper.LogException(ex, "Error processing command due to an exception. Invocation will not continue.:");
				break;
			}

			switch (processCommand.Current)
			{
				case IEnumerator iEnumerator:
					if (iEnumerator != null)
						ProcessIEnumerators.Push(iEnumerator);
					continue;
				case KMSelectable kmSelectable when kmSelectable != null:
					if (HeldSelectables.Contains(kmSelectable))
					{
						DoInteractionEnd(kmSelectable);
						HeldSelectables.Remove(kmSelectable);
					}
					else
					{
						DoInteractionStart(kmSelectable);
						HeldSelectables.Add(kmSelectable);
					}
					break;

				case KMSelectable[] kmSelectables:
					foreach (KMSelectable selectable in kmSelectables)
						if (selectable != null)
						{
							yield return DoInteractionClick(selectable);
							if (Strike) break;
						}
						else
						{
							yield return new WaitForSeconds(0.1f);
						}

					break;

				case Quaternion quaternion:
					RotateByLocalQuaternion(quaternion);
					break;

				case string currentString when !string.IsNullOrEmpty(currentString):
					if (currentString.Equals("trycancel", StringComparison.InvariantCultureIgnoreCase) &&
						CoroutineCanceller.ShouldCancel)
					{
						CoroutineCanceller.ResetCancel();
						cancelled = true;
					}
					else if (currentString.ToLowerInvariant().EqualsAny("elevator music", "hold music", "waiting music"))
					{
						if (_musicPlayer == null)
							_musicPlayer = MusicPlayer.StartRandomMusic();
					}
					else if (currentString.ToLowerInvariant().Equals("cancelled") && cancelling)
					{
						CancelBool?.SetValue(CommandComponent, false);
						CoroutineCanceller.ResetCancel();
						cancelled = true;
					}
					else if (currentString.StartsWith("strikemessage ",
								 StringComparison.InvariantCultureIgnoreCase) &&
							 currentString.Substring(14).Trim() != string.Empty)
						StrikeMessage = currentString.Substring(14);
					else if (currentString.Equals("strike", StringComparison.InvariantCultureIgnoreCase))
						_delegatedStrikeUserNickName = _currentUserNickName;
					else if (currentString.Equals("multiple strikes", StringComparison.InvariantCultureIgnoreCase))
						_disableOnStrike = true;
					else if (currentString.ToLowerInvariant().EqualsAny("detonate", "explode") && TwitchGame.BombActive)
					{
						var bomb = TwitchGame.Instance.Bombs[0];
						AwardStrikes(_currentUserNickName, bomb.StrikeLimit - bomb.StrikeCount);
						bomb.CauseExplosionByModuleCommand(string.Empty,
							ID);
						Strike = true;
					}
					else if (currentString.ToLowerInvariant().Equals("show front"))
						ProcessIEnumerators.Push(Hold());
					else if (currentString.ToLowerInvariant().Equals("show back"))
						ProcessIEnumerators.Push(Hold(false));
					else
						SendToChat(currentString, userNickName, isWhisper, ref parseError);

					break;
				case string[] currentStrings:
					if (currentStrings.Length >= 1)
						if (currentStrings[0].ToLowerInvariant().EqualsAny("detonate", "explode") &&
							TwitchGame.BombActive)
						{
							TwitchBomb bombs = TwitchGame.Instance.Bombs[0];
							AwardStrikes(_currentUserNickName, bombs.StrikeLimit - bombs.StrikeCount);
							switch (currentStrings.Length)
							{
								case 2:
									bombs.CauseExplosionByModuleCommand(currentStrings[1], ID);
									break;
								case 3:
									bombs.CauseExplosionByModuleCommand(currentStrings[1], currentStrings[2]);
									break;
								default:
									bombs.CauseExplosionByModuleCommand(string.Empty, ID);
									break;
							}
						}

					break;
				case Dictionary<string, bool> permissions:
					foreach (KeyValuePair<string, bool> pair in permissions)
					{
						if (TwitchPlaySettings.data.ModPermissions.ContainsKey(pair.Key)) continue;
						TwitchPlaySettings.data.ModPermissions.Add(pair.Key, pair.Value);
						TwitchPlaySettings.WriteDataToFile();
					}
					break;
				case KMMission mission:
					TwitchPlaysService.Instance.RunMission(mission);
					break;
				case object[] objects:
					if (objects == null) break;
					// ReSharper disable once SwitchStatementMissingSomeCases
					switch (objects.Length)
					{
						case 3 when objects[0] is string objstr:
							if (IsAskingPermission(objstr, userNickName, out bool permissionGranted))
							{
								if (permissionGranted)
									switch (objects[1])
									{
										case Action actionTrue:
											actionTrue.Invoke();
											break;
										case IEnumerator iEnumerator when iEnumerator != null:
											ProcessIEnumerators.Push(iEnumerator);
											yield return null;
											continue;
									}
								else
									switch (objects[2])
									{
										case Action actionFalse:
											actionFalse.Invoke();
											break;
										case string objStr2 when !string.IsNullOrEmpty(objStr2):
											SendToChat(objStr2, userNickName, isWhisper, ref parseError);
											break;
										case IEnumerator iEnumerator when iEnumerator != null:
											ProcessIEnumerators.Push(iEnumerator);
											yield return null;
											continue;
									}
							}
							break;
					}
					break;
			}

			yield return processCommand.Current;

			if (CoroutineCanceller.ShouldCancel && !cancelling && CommandComponent != null)
			{
				CancelBool?.SetValue(CommandComponent, true);
				cancelling = CancelBool != null;
			}

		}
		while (ProcessIEnumerators.Count > 0 && !parseError && !cancelled && !Strike);

		ProcessIEnumerators.Clear();
		if (_musicPlayer != null)
		{
			_musicPlayer.StopMusic();
			_musicPlayer = null;
		}

		if (_disableOnStrike)
			AwardStrikes(userNickName, StrikeCount);
		DebugHelper.Log("RespondToCommandInternal() Complete");
	}

	private IEnumerator MakeCoroutine(object obj)
	{
		if (obj is IEnumerator ienum)
			return ienum;
		if (obj is IEnumerable<KMSelectable> kms)
			return MakeSimpleCoroutine(kms);

		if (obj != null)
			DebugHelper.Log("[TwitchHoldable] ProcessTwitchCommand() returned an object that is neither IEnumerator nor IEnumerable<KMSelectable>.");
		return null;
	}

	private IEnumerator MakeSimpleCoroutine(IEnumerable<KMSelectable> kms)
	{
		yield return null;
		yield return kms;
	}

	private static bool IsAskingPermission(string permission, string userNickName, out bool permissionGranted)
	{
		permissionGranted = false;
		switch (permission)
		{
			case "streamer":
				permissionGranted = UserAccess.HasAccess(userNickName, AccessLevel.Streamer, true);
				return true;
			case "streamer-only":
				permissionGranted = UserAccess.HasAccess(userNickName, AccessLevel.Streamer);
				return true;
			case "superuser":
				permissionGranted = UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true);
				return true;
			case "superuser-only":
				permissionGranted = UserAccess.HasAccess(userNickName, AccessLevel.SuperUser);
				return true;
			case "admin":
				permissionGranted = UserAccess.HasAccess(userNickName, AccessLevel.Admin, true);
				return true;
			case "admin-only":
				permissionGranted = UserAccess.HasAccess(userNickName, AccessLevel.Admin);
				return true;
			case "mod":
				permissionGranted = UserAccess.HasAccess(userNickName, AccessLevel.Mod, true);
				return true;
			case "mod-only":
				permissionGranted = UserAccess.HasAccess(userNickName, AccessLevel.Mod);
				return true;
			case "defuser":
				permissionGranted = UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true);
				return true;
			case "defuser-only":
				permissionGranted = UserAccess.HasAccess(userNickName, AccessLevel.Defuser);
				return true;

			default:
				return TwitchPlaySettings.data.ModPermissions.TryGetValue(permission, out permissionGranted);
		}
	}

	internal bool PrintHelp(string userNickname, bool isWhisper)
	{
		if (CommandType == typeof(AlarmClockCommands))
		{
			HelpMessage = "Snooze the alarm clock with “!{0} snooze”.";
			HelpMessage += (TwitchPlaySettings.data.AllowSnoozeOnly && !TwitchPlaySettings.data.AnarchyMode)
				? " (Current settings forbid turning the alarm clock back on.)"
				: " Alarm clock may also be turned back on with “!{0} snooze”.";
		}
		else if (CommandType == typeof(IRCConnectionManagerCommands))
		{
			HelpMessage = "sendtochat Disconnect the IRC from Twitch Plays with “!{0} disconnect”. For obvious reasons, only the streamer may do this.";
		}

		if (string.IsNullOrEmpty(HelpMessage)) return false;
		IRCConnection.SendMessage(string.Format(HelpMessage, ID, userNickname), userNickname, isWhisper);
		return true;
	}

	internal void SendToChat(string message, string userNickname, bool isWhisper, ref bool parseerror)
	{
		if (string.IsNullOrEmpty(message))
		{
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.HoldableInvalidCommand, ID, userNickname), userNickname, !isWhisper);
			return;
		}
		if (message.StartsWith("sendtochat ", StringComparison.InvariantCultureIgnoreCase) && message.Substring(11).Trim() != string.Empty)
		{
			IRCConnection.SendMessage(string.Format(message.Substring(11), ID, userNickname), userNickname, !isWhisper);
			return;
		}
		if (message.StartsWith("sendtochaterror ", StringComparison.InvariantCultureIgnoreCase) && message.Substring(16).Trim() != string.Empty)
		{
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.HoldableCommandError, ID, userNickname, message.Substring(16)), userNickname, !isWhisper);
			parseerror = true;
			return;
		}
		if (!message.Equals("parseerror", StringComparison.InvariantCultureIgnoreCase)) return;
		IRCConnection.SendMessage($"Sorry @{userNickname}, there was an error parsing the command for !{ID}", userNickname, !isWhisper);
		parseerror = true;
	}

	protected void DoInteractionStart(MonoBehaviour interactable) => interactable.GetComponent<Selectable>().HandleInteract();

	protected void DoInteractionEnd(MonoBehaviour interactable)
	{
		Selectable selectable = interactable.GetComponent<Selectable>();
		selectable.OnInteractEnded();
		selectable.SetHighlight(false);
	}

	// ReSharper disable once UnusedMember.Global
	protected WaitForSeconds DoInteractionClick(MonoBehaviour interactable, float delay) => DoInteractionClick(interactable, null, delay);

	protected WaitForSeconds DoInteractionClick(MonoBehaviour interactable, string strikeMessage = null, float delay = 0.1f)
	{
		if (strikeMessage != null)
			StrikeMessage = strikeMessage;

		DoInteractionStart(interactable);
		DoInteractionEnd(interactable);
		return new WaitForSeconds(delay);
	}

	public IEnumerator Hold(bool frontFace = true)
	{
		bool doForceRotate = false;

		if (Holdable.HoldState != FloatingHoldable.HoldStateEnum.Held)
		{
			Holdable.GetComponent<Selectable>().Trigger();
			doForceRotate = true;
		}
		else if (frontFace != _heldFrontFace)
		{
			doForceRotate = true;
		}

		_heldFrontFace = frontFace;

		if (doForceRotate)
		{
			var forceRotationCoroutine = ForceHeldRotation(frontFace, Holdable.PickupTime);
			while (forceRotationCoroutine.MoveNext())
				yield return forceRotationCoroutine.Current;
		}
	}

	public IEnumerator Drop()
	{
		if (Holdable.HoldState != FloatingHoldable.HoldStateEnum.Held)
			yield break;
		yield return null;

		var turnCoroutine = Hold();
		while (turnCoroutine.MoveNext())
			yield return turnCoroutine.Current;

		while (Holdable.HoldState == FloatingHoldable.HoldStateEnum.Held)
		{
			KTInputManager.Instance.SelectableManager.HandleCancel();
			yield return new WaitForSeconds(0.1f);
		}
	}

	public IEnumerator Turn() => Hold(!_heldFrontFace);

	private static IEnumerator ForceHeldRotation(bool frontFace, float duration)
	{
		var selectableManager = KTInputManager.Instance.SelectableManager;
		var baseTransform = selectableManager.GetBaseHeldObjectTransform();

		float oldZSpin = selectableManager.GetZSpin();
		float targetZSpin = frontFace ? 0.0f : 180.0f;

		float initialTime = Time.time;
		while (Time.time - initialTime < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			float currentZSpin = Mathf.SmoothStep(oldZSpin, targetZSpin, lerp);

			var currentRotation = Quaternion.Euler(0.0f, 0.0f, currentZSpin);
			var HeldObjectTiltEulerAngles = selectableManager.GetHeldObjectTiltEulerAngles();
			HeldObjectTiltEulerAngles.x = Mathf.Clamp(HeldObjectTiltEulerAngles.x, -95f, 95f);
			HeldObjectTiltEulerAngles.z -= selectableManager.GetZSpin() - currentZSpin;

			selectableManager.SetZSpin(currentZSpin);
			selectableManager.SetControlsRotation(baseTransform.rotation * currentRotation);
			selectableManager.SetHeldObjectTiltEulerAngles(HeldObjectTiltEulerAngles);
			selectableManager.HandleFaceSelection();
			yield return null;
		}

		var HeldObjectTileEulerAnglesFinal = selectableManager.GetHeldObjectTiltEulerAngles();
		HeldObjectTileEulerAnglesFinal.x = Mathf.Clamp(HeldObjectTileEulerAnglesFinal.x, -95f, 95f);
		HeldObjectTileEulerAnglesFinal.z -= selectableManager.GetZSpin() - targetZSpin;

		selectableManager.SetZSpin(targetZSpin);
		selectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, targetZSpin));
		selectableManager.SetHeldObjectTiltEulerAngles(HeldObjectTileEulerAnglesFinal);
		selectableManager.HandleFaceSelection();
	}

	public void RotateByLocalQuaternion(Quaternion localQuaternion)
	{
		var selectableManager = KTInputManager.Instance.SelectableManager;
		var baseTransform = selectableManager.GetBaseHeldObjectTransform();

		float currentZSpin = _heldFrontFace ? 0.0f : 180.0f;

		selectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, currentZSpin) * localQuaternion);
		selectableManager.HandleFaceSelection();
	}

	private static readonly List<string> FullNamesLogged = new List<string>();
	private static void LogAllComponentTypes(MonoBehaviour holdable)
	{
		//If and when there is a potential conflict between multiple assemblies, this will help to find these conflicts so that
		//ReflectionHelper.FindType(fullName, assemblyName) can be used instead.

		Component[] allComponents = holdable.GetComponentsInChildren<Component>(true);
		foreach (Component component in allComponents)
		{
			string fullName = component.GetType().FullName;
			if (FullNamesLogged.Contains(fullName)) continue;
			FullNamesLogged.Add(fullName);

			Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetSafeTypes()).Where(t => t.FullName?.Equals(fullName) ?? false).ToArray();
			if (types.Length < 2)
				continue;

			Debug.LogFormat("[TwitchHoldable] Found {0} types with fullName = \"{1}\"", types.Length, fullName);
			foreach (Type type in types)
			{
				Debug.LogFormat("[TwitchHoldable] \ttype.FullName=\"{0}\" type.Assembly.GetName().Name=\"{1}\"", type.FullName, type.Assembly.GetName().Name);
			}
		}
	}

	private static bool ValidateMethodCommandMethod(Type type, MethodInfo candidateMethod)
	{
		ParameterInfo[] parameters = candidateMethod.GetParameters();
		if (parameters.Length == 0)
		{
			Debug.LogFormat("[TwitchHoldable] Found a potential candidate ProcessTwitchCommand method in {0}, but the parameter list does not match the expected parameter list (too few parameters).", type.FullName);
			return false;
		}

		if (parameters.Length > 1)
		{
			Debug.LogFormat("[TwitchHoldable] Found a potential candidate ProcessTwitchCommand method in {0}, but the parameter list does not match the expected parameter list (too many parameters).", type.FullName);
			return false;
		}

		if (parameters[0].ParameterType != typeof(string))
		{
			Debug.LogFormat("[TwitchHoldable] Found a potential candidate ProcessTwitchCommand method in {0}, but the parameter list does not match the expected parameter list (expected a single string parameter, got a single {1} parameter).", type.FullName, parameters[0].ParameterType.FullName);
			return false;
		}

		if (typeof(IEnumerable<KMSelectable>).IsAssignableFrom(candidateMethod.ReturnType))
		{
			Debug.LogFormat("[TwitchHoldable] Found a valid candidate ProcessTwitchCommand method in {0} (using easy/simple API).", type.FullName);
			return true;
		}

		if (candidateMethod.ReturnType == typeof(IEnumerator))
		{
			Debug.LogFormat("[TwitchHoldable] Found a valid candidate ProcessTwitchCommand method in {0} (using advanced/coroutine API).", type.FullName);
			return true;
		}

		return false;
	}

	private readonly string _defaultId;
	public virtual string ID => _defaultId;

	private bool _heldFrontFace;
	private string _delegatedStrikeUserNickName;
	private string _currentUserNickName;

	private bool _disableOnStrike;
	protected bool Strike { get; private set; }
	protected int StrikeCount { get; private set; }
	protected string StrikeMessage { get; set; }
	protected FieldInfo HelpMessageField { get; set; }
	private string _helpMessage = null;
	public string HelpMessage
	{
		get
		{
			if (HelpMessageField?.GetValue(HelpMessageField.IsStatic ? null : CommandComponent) is string helpString)
				_helpMessage = helpString;
			return _helpMessage;
		}
		protected set
		{
			if (HelpMessageField?.GetValue(HelpMessageField.IsStatic ? null : CommandComponent) is string)
				HelpMessageField.SetValue(HelpMessageField.IsStatic ? null : CommandComponent, value);
			_helpMessage = value;
		}
	}

	protected Component CommandComponent;
	protected MethodInfo HandlerMethod;
	protected FieldInfo CancelBool;
	protected string UserNickName;
	private MusicPlayer _musicPlayer;

	public FloatingHoldable Holdable;
	public Type CommandType;
	protected HashSet<KMSelectable> HeldSelectables = new HashSet<KMSelectable>();
	protected Stack<IEnumerator> ProcessIEnumerators = new Stack<IEnumerator>();
}
