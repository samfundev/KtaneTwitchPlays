using System.Collections;
using System.Reflection;
using UnityEngine;

public class CoroutineHoldableHandler : HoldableHandler
{
	public CoroutineHoldableHandler(KMHoldableCommander commander, FloatingHoldable holdable, IRCConnection connection, CoroutineCanceller canceller, Component commandComponent, MethodInfo handler, string helpMessage, FieldInfo cancelbool) 
		: base(commander, holdable, connection, canceller)
	{
		CommandComponent = commandComponent;
		HandlerMethod = handler;
		CancelBool = cancelbool;
		HelpMessage = helpMessage;
	}

	protected override IEnumerator RespondToCommandInternal(string command)
	{
		IEnumerator handler = (IEnumerator) HandlerMethod.Invoke(CommandComponent, new object[] {command});
		bool cancelling = false;
		CancelBool?.SetValue(CommandComponent, false);
		while (handler.MoveNext())
		{
			yield return handler.Current;
			if (Canceller.ShouldCancel && !cancelling)
			{
				CancelBool?.SetValue(CommandComponent, true);
				cancelling = CancelBool != null;
			}
			else if (cancelling && handler.Current is string strCurrent)
			{
				if (!strCurrent.Equals("Cancelled")) continue;
				CancelBool?.SetValue(CommandComponent, false);
				Canceller.ResetCancel();
				yield break;
			}
		}
	}

	protected Component CommandComponent;
	protected FieldInfo CancelBool;
	protected MethodInfo HandlerMethod;
}
