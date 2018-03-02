using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class WebDesignComponentSolver : ComponentSolver
{
	public WebDesignComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
	base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		text = (TextMesh)_text.GetValue(_component);
		btns = (KMSelectable[])_btn.GetValue(_component);
	}

	private void FitToWidth()
	{
		screenHolder = text.text;
		foreach (char c in screenHolder)
		{
			if (c == '\n') screenHolder.Replace("\n", "");
		}
		string[] words = screenHolder.Split(" "[0]);
		newScreen = "";
		string line = "";
		var count = 0;

		foreach(string s in words)
		{
			string temp = line + " " + s;
			if (count > wantedHeight) { screenHolder = temp; text.text = newScreen.Substring(1, newScreen.Length - 1); return; }
			else if (temp.Length > wantedWidth)
			{
				newScreen += line + "\n";
				line = s;
				count++;
			}
			else { line = temp; }
		}
		newScreen += line;
		text.text = newScreen.Substring(1, newScreen.Length - 1);
		screenHolder = "";
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.Equals("clarify", StringComparison.InvariantCultureIgnoreCase)) 
		{
			if (!active)
			{
				yield return null;
				var p = text.transform.localPosition;
				oldScreen = text.text;
				active = true;
				text.fontSize = 30;
				text.transform.localPosition = new Vector3(p.x + 0.024f, p.y, p.z);
				FitToWidth();
				if (!(screenHolder == ""))
				{
					while (!(screenHolder == ""))
					{
						yield return new WaitForSeconds(10f);
						text.text = screenHolder;
						FitToWidth();
					}
				}
				yield return null;

			}
			else if (active)
			{
				yield return null;
				var p = text.transform.localPosition;
				active = false;
				text.fontSize = 15;
				text.text = oldScreen;
				text.transform.localPosition = new Vector3(p.x - 0.024f, p.y, p.z);
				yield return null;
			}
		}

		else
		{
			/* // For some reason this part isn't working, so reimplementation for now.
			  KMSelectable[] command = (KMSelectable[])_ProcessCommandMethod.Invoke(_component, new object[] { inputCommand });
			if (command == null) yield break;
			yield return null;
			yield return command;*/
			switch (inputCommand.ToLowerInvariant().Trim())
			{
				case "accept":
				case "acc":
					yield return null;
					yield return DoInteractionClick(btns[0]);
					yield break;
				case "consider":
				case "con":
					yield return null;
					yield return DoInteractionClick(btns[1]);
					yield break;
				case "reject":
				case "rej":
					yield return null;
					yield return DoInteractionClick(btns[2]);
					yield break;
				default:
					yield return null;
					yield break;
			}
		}
	}
	static WebDesignComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("webdesign");
		//_ProcessCommandMethod = _componentType.GetMethod("ProcessTwitchCommand", BindingFlags.NonPublic | BindingFlags.Instance );
		_text = _componentType.GetField("text", BindingFlags.Public | BindingFlags.Instance);
		_btn = _componentType.GetField("btn", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	//private static MethodInfo _ProcessCommandMethod = null;
	private static FieldInfo _btn = null;
	private KMSelectable[] btns = null;
	private static FieldInfo _text = null;
	private TextMesh text = null;
	private bool active;
	private string oldScreen = null;
	private string newScreen = null;
	private string screenHolder = null;

	private object _component = null;
	private int wantedWidth = 15;
	private int wantedHeight = 6;
}