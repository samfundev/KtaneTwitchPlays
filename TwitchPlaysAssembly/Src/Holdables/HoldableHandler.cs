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
							kmSelectable.OnInteract();
						}
						else
						{
							heldSelectables.Remove(kmSelectable);
							kmSelectable.OnInteractEnded();
						}
						break;

					case KMSelectable[] kmSelectables:
						foreach (KMSelectable selectable in kmSelectables)
						{
							selectable.OnInteract();
							selectable.OnInteractEnded();
							yield return new WaitForSeconds(0.1f);
						}
						break;

					case Quaternion quaternion:
						HoldableCommander.RotateByLocalQuaternion(quaternion);
						break;

					case string str:
						break;

					default:
						break;
				}
				yield return processCommand.Current;
			} while (result);
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

	protected abstract IEnumerator RespondToCommandInternal(string command);

	protected IEnumerator RespondToCommandCommon(string command)
	{
		yield break;
	}

	protected ICommandResponseNotifier ResponseNotifier;
	protected IRCConnection ircConnection;
	protected string UserNickName;

	public string HelpMessage;
	public CoroutineCanceller Canceller;
	public KMHoldableCommander HoldableCommander;
	public FloatingHoldable Holdable;
	protected HashSet<KMSelectable> heldSelectables = new HashSet<KMSelectable>();



}
