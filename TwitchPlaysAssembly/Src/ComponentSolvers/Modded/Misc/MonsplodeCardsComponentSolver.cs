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

		var help = (string)_Names[4].GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		if (!modInfo.helpTextOverride) modInfo.helpText = help + "\nIf you're having trouble viewing the names or version of the cards, use !{0} clarify left or right to view them in chat. Use !{0} clarifycycle to view each card while cycling. Do note, rarity is not copied.";
		for (int i = 0; i < 4; i++)
		{
			Names[i] = (TextMesh)_Names[i].GetValue(_component);
		}
	}

	private void Update()
	{
		currentDeck = (int)(_Names[5]).GetValue(_component);
		deckSize = (int)(_Names[6]).GetValue(_component);
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().Equals("clarify left"))
		{
			yield return null;
			yield return $"sendtochat {string.Format("Owned: {0} [{1}]", Names[0].text.Replace('\n', ' '), Names[1].text)}";
		}
		else if (inputCommand.ToLowerInvariant().Equals("clarify right"))
		{
			yield return null;
			yield return $"sendtochat {string.Format("Offered: {0} [{1}]", Names[2].text.Replace('\n', ' '), Names[3].text)}";
		}
		else if (inputCommand.ToLowerInvariant().Equals("clarify"))
		{
			yield return null;
			yield return $"sendtochat {string.Format("Currently viewing owned: {0} [{1}], offered: {2} [{3}]", Names[0].text.Replace('\n', ' '), Names[1].text, Names[2].text.Replace('\n', ' '), Names[3].text)}";
		}
		else if (inputCommand.ToLowerInvariant().EqualsAny("clarifycycle", "cycleclarify"))
		{
			yield return null;
			Update();
			cardRight = _CardPress[0].MakeGenericMethod();
			cardLeft = _CardPress[1].MakeGenericMethod();
			int deck = currentDeck;
			var output = new string[3] { "", "", "" };
			while (currentDeck != 0)
			{
				cardLeft.Invoke(_component, null);
				Update();
				yield return new WaitForSeconds(0.1f);
			}
			for (int i = 0; i < deckSize; i++)
			{
				output[i] = Names[0].text.Replace('\n', ' ') + " [" + Names[1].text + "]";
				yield return new WaitForSecondsWithCancel(5f,false);
				cardRight.Invoke(_component, null);
				Update();
			}
			while (currentDeck != deck)
			{
				cardLeft.Invoke(_component, null);
				Update();
				yield return new WaitForSeconds(0.1f);
			}
			yield return "trycancel The monsplode trading card clarify cycle was not completed";
			yield return $"senddelayedmessage 5.0 Owned: {output[0]}, {output[1]}, {output[2]} \nOffered: {Names[2].text.Replace('\n', ' ')} [{Names[3].text}]";
			yield break;
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
		_CardPress[0] = _componentType.GetMethod("NextCardPress", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		_CardPress[1] = _componentType.GetMethod("PrevCardPress", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		_Names[0] = _componentType.GetField("deckTM", BindingFlags.Public | BindingFlags.Instance);
		_Names[2] = _componentType.GetField("offerTM", BindingFlags.Public | BindingFlags.Instance);
		_Names[1] = _componentType.GetField("deckVersion", BindingFlags.Public | BindingFlags.Instance);
		_Names[3] = _componentType.GetField("offerVersion", BindingFlags.Public | BindingFlags.Instance);
		_Names[4] = _componentType.GetField("TwitchHelpMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		_Names[5] = _componentType.GetField("currentDeck", BindingFlags.Public | BindingFlags.Instance);
		_Names[6] = _componentType.GetField("deckSize", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static MethodInfo _ProcessCommandMethod = null, cardRight = null, cardLeft = null;
	private static MethodInfo[] _CardPress = new MethodInfo[2];
	private TextMesh[] Names = new TextMesh[4];
	public int currentDeck, deckSize;

	private object _component = null;
	private static FieldInfo[] _Names = new FieldInfo[7];
}
