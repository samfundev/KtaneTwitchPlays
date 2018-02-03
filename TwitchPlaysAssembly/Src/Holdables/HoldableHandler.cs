using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract class HoldableHandler : ICommandResponder
{
	public HoldableHandler(KMHoldableCommander commander, FloatingHoldable holdable, IRCConnection connection, CoroutineCanceller canceller)
	{
		HoldableCommander = commander;
		Holdable = holdable;
		ircConnection = connection;
		Canceller = canceller;
	}

	public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier, IRCConnection connection)
	{
		ResponseNotifier = responseNotifier;

		FloatingHoldable.HoldStateEnum holdState = Holdable.HoldState;

		if (holdState == FloatingHoldable.HoldStateEnum.Held)
		{}
		else
		{
			IEnumerator holdCoroutine = HoldableCommander.Hold();
			while (holdCoroutine.MoveNext())
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
			processIEnumerators.Push(processCommand);
			do
			{
				try
				{
					bool result = false;
					while(!result && processIEnumerators.Count > 0)
					{
						if (processIEnumerators.Count > 0)
							processCommand = processIEnumerators.Pop();
						result = processCommand.MoveNext();
						if (result)
							processIEnumerators.Push(processCommand);
					}
					if (!result) break;
				}
				catch (Exception ex)
				{
					DebugHelper.LogException(ex, "Error Processing command due to an exception. Invocation will not continue.:");
				}
				switch (processCommand.Current)
				{
					case IEnumerator iEnumerator:
						if(iEnumerator != null)
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

					case string currentString:
						if (currentString.Equals("parseerror", StringComparison.InvariantCultureIgnoreCase))
						{
							ircConnection.SendMessage($"Sorry @{userNickName}, there was an error parsing the command for !{HoldableCommander.ID}");
							parseError = true;
						}
						else if (currentString.Equals("trycancel", StringComparison.InvariantCultureIgnoreCase) &&
						         Canceller.ShouldCancel)
						{
							Canceller.ResetCancel();
							cancelled = true;
						}
						else if (currentString.StartsWith("sendtochat ", StringComparison.InvariantCultureIgnoreCase) &&
						         currentString.Substring(11).Trim() != string.Empty)
						{
							SendToChat(currentString, userNickName);
						}
						else if (currentString.StartsWith("sendtochaterror ", StringComparison.InvariantCultureIgnoreCase) &&
						         currentString.Substring(16).Trim() != string.Empty)
						{
							SendToChat(currentString, userNickName);
							parseError = true;
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
							Canceller.ResetCancel();
							cancelled = true;
						}
						break;
					case object[] objects:
						if (objects == null) break;
						switch (objects.Length)
						{
							case 3 when objects[0] is string objstr && objects[1] is Action actionTrue:
								switch (objstr)
								{
									case "streamer" when UserAccess.HasAccess(userNickName, AccessLevel.Streamer, true):
									case "streamer-only" when UserAccess.HasAccess(userNickName, AccessLevel.Streamer):
									case "superuser" when UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true):
									case "superuser-only" when UserAccess.HasAccess(userNickName, AccessLevel.SuperUser):
									case "admin" when UserAccess.HasAccess(userNickName, AccessLevel.Admin , true):
									case "admin-only" when UserAccess.HasAccess(userNickName, AccessLevel.Admin):
									case "mod" when UserAccess.HasAccess(userNickName, AccessLevel.Mod, true):
									case "mod-only" when UserAccess.HasAccess(userNickName, AccessLevel.Mod):
									case "defuser" when UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true):
									case "defuser-only" when UserAccess.HasAccess(userNickName, AccessLevel.Defuser):
										actionTrue.Invoke();
										break;
									default:
										if (objects[2] is Action actionFalse)
											actionFalse.Invoke();
										if (objects[2] is string objStr2)
										{
											if (objStr2.StartsWith("sendtochat ", StringComparison.InvariantCultureIgnoreCase))
												SendToChat(objStr2, userNickName);
											if (objStr2.StartsWith("sendtochaterror ", StringComparison.InvariantCultureIgnoreCase))
											{
												SendToChat(objStr2, userNickName);
												parseError = true;
											}
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

				if (Canceller.ShouldCancel && !cancelling && CommandComponent != null)
				{
					CancelBool?.SetValue(CommandComponent, true);
					cancelling = CancelBool != null;
				}
			} while (processIEnumerators.Count > 0 && !parseError && !cancelled);
			processIEnumerators.Clear();
			if (_musicPlayer != null)
			{
				_musicPlayer.StopMusic();
				_musicPlayer = null;
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

	private void SendToChat(string message, string userNickname)
	{
		bool error = message.StartsWith("sendtochaterror ", StringComparison.InvariantCultureIgnoreCase);
		message = message.Replace("sendtochat ", "").Replace("sendtochaterror ", "");
		if (error)
		{
			//ircConnection.SendMessage($"Sorry @{userNickName}, !{HoldableCommander.ID} responed with the following error: {currentString.Substring(16)}");
			message = "Sorry @{1}, !{0} responded with the following error: " + message;
		}
		ircConnection.SendMessage(message, HoldableCommander.ID, userNickname);
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

	protected WaitForSeconds DoInteractionClick(MonoBehaviour interactable, float delay = 0.1f)
	{
		DoInteractionStart(interactable);
		DoInteractionEnd(interactable);
		return new WaitForSeconds(delay);
	}

	public void ShowHelp()
	{
		if (!string.IsNullOrEmpty(HelpMessage))
			ircConnection.SendMessage(HelpMessage, HoldableCommander.ID);
	}

	protected Component CommandComponent;
	protected MethodInfo HandlerMethod;
	protected FieldInfo CancelBool;
	protected ICommandResponseNotifier ResponseNotifier;
	protected IRCConnection ircConnection;
	protected string UserNickName;
	private MusicPlayer _musicPlayer;

	public string HelpMessage;
	public CoroutineCanceller Canceller;
	public KMHoldableCommander HoldableCommander;
	public FloatingHoldable Holdable;
	protected HashSet<KMSelectable> heldSelectables = new HashSet<KMSelectable>();
	protected Stack<IEnumerator> processIEnumerators = new Stack<IEnumerator>();



}
