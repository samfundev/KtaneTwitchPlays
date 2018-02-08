using System.Collections;
using System.Reflection;
using UnityEngine;

public class CoroutineHoldableHandler : HoldableHandler
{
	public CoroutineHoldableHandler(KMHoldableCommander commander, FloatingHoldable holdable, Component commandComponent, MethodInfo handler, string helpMessage, FieldInfo cancelbool) 
		: base(commander, holdable)
	{
		CommandComponent = commandComponent;
		HandlerMethod = handler;
		CancelBool = cancelbool;
		HelpMessage = helpMessage;
	}

	protected override IEnumerator RespondToCommandInternal(string command)
	{
		IEnumerator handler = (IEnumerator) HandlerMethod.Invoke(CommandComponent, new object[] {command});
		CancelBool?.SetValue(CommandComponent, false);
		while (handler.MoveNext())
		{
			yield return handler.Current;
			
		}
	}
}
