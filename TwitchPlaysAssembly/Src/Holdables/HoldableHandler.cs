using System;
using System.Collections;
using System.Collections.Generic;
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
			bool result = false;
			do
			{
				try
				{
					result = processCommand.MoveNext();
				}
				catch
				{
					DebugHelper.Log("Error Processing command. Invocation will not continue.");
				}
				switch (processCommand.Current)
				{
					case KMSelectable kmSelectable:
						if (!heldSelectables.Contains(kmSelectable))
						{
							heldSelectables.Add(kmSelectable);
							DoInteractionStart(kmSelectable);
						}
						else
						{
							heldSelectables.Remove(kmSelectable);
							DoInteractionEnd(kmSelectable);
						}
						break;

					case KMSelectable[] kmSelectables:
						foreach (KMSelectable selectable in kmSelectables)
						{
							yield return DoInteractionClick(selectable);
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
						break;

					default:
						break;
				}
				yield return processCommand.Current;

			} while (result && !parseError && !cancelled);
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

	protected ICommandResponseNotifier ResponseNotifier;
	protected IRCConnection ircConnection;
	protected string UserNickName;
	private MusicPlayer _musicPlayer;

	public string HelpMessage;
	public CoroutineCanceller Canceller;
	public KMHoldableCommander HoldableCommander;
	public FloatingHoldable Holdable;
	protected HashSet<KMSelectable> heldSelectables = new HashSet<KMSelectable>();



}
