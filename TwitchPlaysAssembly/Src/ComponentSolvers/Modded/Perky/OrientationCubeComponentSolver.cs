using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class OrientationCubeComponentSolver : ComponentSolver
{
	public OrientationCubeComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_submit = (MonoBehaviour)_submitField.GetValue(bombComponent.GetComponent(_componentType));
		_left = (MonoBehaviour)_leftField.GetValue(bombComponent.GetComponent(_componentType));
		_right = (MonoBehaviour)_rightField.GetValue(bombComponent.GetComponent(_componentType));
		_ccw = (MonoBehaviour)_ccwField.GetValue(bombComponent.GetComponent(_componentType));
		_cw = (MonoBehaviour)_cwField.GetValue(bombComponent.GetComponent(_componentType));
		//virtualAngleEmulator = (float)_virtualField.GetValue(bombComponent.GetComponent(_componentType));
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Move the cube with !{0} press cw l set. The buttons are l, r, cw, ccw, set.");
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		List<MonoBehaviour> buttons = new List<MonoBehaviour>();

		string[] split = inputCommand.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (split.Length < 2 || split[0] != "press")
			yield break;

		if (_submit == null || _left == null || _right == null || _ccw == null || _cw == null)
		{
			yield return "autosolve due to required buttons not present.";
			yield break;
		}

		foreach(string cmd in split.Skip(1))
		{
			switch (cmd)
			{
				case "left": case "l": buttons.Add(_left); interaction.Add("Left rotation"); break;

				case "right": case "r": buttons.Add(_right); interaction.Add("Right rotation"); break;

				case "counterclockwise": case "counter-clockwise": case "ccw":
				case "anticlockwise": case "anti-clockwise": case "acw": buttons.Add(_ccw); interaction.Add("Counterclockwise rotation"); break;

				case "clockwise": case "cw": buttons.Add(_cw); interaction.Add("Clockwise rotation"); break;

				case "set": case "submit":buttons.Add(_submit); interaction.Add("submit"); break;

				default: yield break;
			}   //Check for any invalid commands.  Abort entire sequence if any invalid commands are present.
		}

		yield return "Orientation Cube Solve Attempt";
		var debugStart = "[Orientation Cube TP#" + ComponentHandle.IDTextMultiDecker.text + "]";
		Debug.LogFormat("{0} Inputted commands: {1}", debugStart, String.Join(", ", interaction.ToArray()));

		foreach (MonoBehaviour button in buttons)
		{
			yield return DoInteractionClick(button);
		}
	}

	static OrientationCubeComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("OrientationModule");
		_submitField = _componentType.GetField("SubmitButton", BindingFlags.Public | BindingFlags.Instance);
		_leftField = _componentType.GetField("YawLeftButton", BindingFlags.Public | BindingFlags.Instance);
		_rightField = _componentType.GetField("YawRightButton", BindingFlags.Public | BindingFlags.Instance);
		_ccwField = _componentType.GetField("RollLeftButton", BindingFlags.Public | BindingFlags.Instance);
		_cwField = _componentType.GetField("RollRightButton", BindingFlags.Public | BindingFlags.Instance);
		//_virtualField = _componentType.GetField("virtualViewAngle", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _submitField = null;
	private static FieldInfo _leftField = null;
	private static FieldInfo _rightField = null;
	private static FieldInfo _ccwField = null;
	private static FieldInfo _cwField = null;
	//private static FieldInfo _virtualField = null;

	private List<string> interaction = new List<string>();
	/*private string[] sides = new string[] { "l", "r", "f", "b", "t", "o" };
	private Quaternion emulatedView;
	private bool first = true;
	private float originalAngle;*/

	private MonoBehaviour _submit = null;
	private MonoBehaviour _left = null;
	private MonoBehaviour _right = null;
	private MonoBehaviour _ccw = null;
	private MonoBehaviour _cw = null;
	//private float virtualAngleEmulator;
}
