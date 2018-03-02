using System;
using System.Collections;
using System.Collections.Generic;
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
	}

	private readonly List<string> _screens = new List<string>();
	private void MakeScreens()
	{
		if (_screens.Count > 0) return;
		string[] lines = oldScreen.Replace("\n", "").Replace(";",";\n").Replace("\n}"," }").Wrap(wantedWidth).Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < lines.Length;)
		{
			string screen = "";
			for (int j = 0; j < wantedHeight && i < lines.Length; j++, i++)
			{
				screen += lines[i] + ((j < (wantedHeight - 1)) ? "\n" : "");
			}
			_screens.Add(screen);
		}
	}

	private IEnumerator _clarifyRoutine = null;
	private IEnumerator ClarifyWebDesign()
	{
		active = true;
		var p = text.transform.localPosition;
		oldScreen = text.text;
		text.fontSize = 25;
		MakeScreens();

		text.transform.localPosition = new Vector3(p.x + 0.01f, p.y, p.z);
		while (active)
		{
			foreach (string screen in _screens)
			{
				if (!active) break;
				text.text = screen;
				for (int i = 0; i < 12 && active; i++)
					yield return new WaitForSeconds(0.25f);
			}
		}
		text.fontSize = 15;
		text.text = oldScreen;
		text.transform.localPosition = new Vector3(p.x - 0.01f, p.y, p.z);
		_clarifyRoutine = null;
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.Equals("clarify", StringComparison.InvariantCultureIgnoreCase))
		{
			yield return null;
			yield return null;
			if (_clarifyRoutine == null)
			{
				_clarifyRoutine = ClarifyWebDesign();
				BombComponent.StartCoroutine(_clarifyRoutine);
			}
			else
			{
				active = false;
			}
		}

		else
		{
			KMSelectable[] command = (KMSelectable[])_ProcessCommandMethod.Invoke(_component, new object[] { inputCommand });
			if (command == null || command.Length == 0) yield break;
			yield return null;
			foreach (KMSelectable button in command)
			{
				yield return DoInteractionClick(button);
			}
		}
	}
	static WebDesignComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("webdesign");
		_ProcessCommandMethod = _componentType.GetMethod("ProcessTwitchCommand", BindingFlags.NonPublic | BindingFlags.Instance );
		_text = _componentType.GetField("text", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static MethodInfo _ProcessCommandMethod = null;
	private static FieldInfo _text = null;
	private TextMesh text = null;
	private bool active;
	private string oldScreen = null;

	private object _component = null;
	private int wantedWidth = 18;
	private int wantedHeight = 8;
}