using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class MonsplodeCardsComponentSolver : ComponentSolver
{
	public MonsplodeCardsComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
	base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		Names[0] = (TextMesh)_Names[0].GetValue(_component);
		Names[1] = (TextMesh)_Names[1].GetValue(_component);
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().Equals("clarify left"))
		{
			yield return null;
			yield return $"sendtochat {string.Format("Owned: {0}", Names[0].text)}";
		}
		else if (inputCommand.ToLowerInvariant().Equals("clarify right"))
		{
			yield return null;
			yield return $"sendtochat {string.Format("Offered: {0}", Names[1].text)}";
		}
		else if (inputCommand.ToLowerInvariant().Equals("clarify"))
		{
			yield return null;
			yield return $"sendtochat {string.Format("Currently viewing owned: {0} offered: {1}", Names[0].text, Names[1].text)}";
		}
		else
		{
			IEnumerator command = (IEnumerator)_ProcessCommandMethod.Invoke(_component, new object[] { inputCommand });
			if (command == null) yield break;
			while (command.MoveNext())
			{
				yield return command.Current;
			}
		}
	}

	static MonsplodeCardsComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("MonsplodeCardModule");
		_ProcessCommandMethod = _componentType.GetMethod("ProcessTwitchCommand", BindingFlags.NonPublic | BindingFlags.Instance);
		_Names[0] = _componentType.GetField("deckTM", BindingFlags.Public | BindingFlags.Instance);
		_Names[1] = _componentType.GetField("offerTM", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static MethodInfo _ProcessCommandMethod = null;
	private TextMesh[] Names = new TextMesh[2];

	private object _component = null;
	private static FieldInfo[] _Names = new FieldInfo[2];
}
