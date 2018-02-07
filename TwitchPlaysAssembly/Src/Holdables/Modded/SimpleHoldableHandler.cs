using System.Collections;
using System.Reflection;
using UnityEngine;


public class SimpleHoldableHandler : HoldableHandler
{
	public SimpleHoldableHandler(KMHoldableCommander commander, FloatingHoldable holdable, IRCConnection connection, Component commandComponent, MethodInfo handler, string helpMessage)
		: base(commander, holdable, connection)
	{
		CommandComponent = commandComponent;
		HandlerMethod = handler;
		HelpMessage = helpMessage;
	}

	protected override IEnumerator RespondToCommandInternal(string command)
	{
		KMSelectable[] selectables = (KMSelectable[]) HandlerMethod.Invoke(CommandComponent, new object[] {command});
		if (selectables == null)
			yield break;
		yield return null;
		yield return selectables;
	}
}
