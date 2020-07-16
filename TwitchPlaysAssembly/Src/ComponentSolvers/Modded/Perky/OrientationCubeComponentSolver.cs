using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class OrientationCubeComponentSolver : ComponentSolver
{
	public OrientationCubeComponentSolver(TwitchModule module) :
		base(module)
	{
		var component = module.BombComponent.GetComponent(ComponentType);
		_submit = (MonoBehaviour) SubmitField.GetValue(component);
		_left = (MonoBehaviour) LeftField.GetValue(component);
		_right = (MonoBehaviour) RightField.GetValue(component);
		_ccw = (MonoBehaviour) CcwField.GetValue(component);
		_cw = (MonoBehaviour) CwField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Move the cube with !{0} press cw l set. The buttons are l, r, cw, ccw, set.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
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

		foreach (string cmd in split.Skip(1))
		{
			switch (cmd)
			{
				case "left": case "l": buttons.Add(_left); _interaction.Add("Left rotation"); break;

				case "right": case "r": buttons.Add(_right); _interaction.Add("Right rotation"); break;

				case "counterclockwise":
				case "counter-clockwise":
				case "ccw":
				case "anticlockwise":
				case "anti-clockwise":
				case "acw": buttons.Add(_ccw); _interaction.Add("Counterclockwise rotation"); break;

				case "clockwise": case "cw": buttons.Add(_cw); _interaction.Add("Clockwise rotation"); break;

				case "set": case "submit": buttons.Add(_submit); _interaction.Add("submit"); break;

				default: yield break;
			} //Check for any invalid commands.  Abort entire sequence if any invalid commands are present.
		}

		yield return "Orientation Cube Solve Attempt";
		string debugStart = "[Orientation Cube TP#" + Code + "]";
		DebugHelper.Log($"{debugStart} Inputted commands: {string.Join(", ", _interaction.ToArray())}");

		foreach (MonoBehaviour button in buttons)
		{
			yield return DoInteractionClick(button);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("OrientationModule");
	private static readonly FieldInfo SubmitField = ComponentType.GetField("SubmitButton", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo LeftField = ComponentType.GetField("YawLeftButton", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo RightField = ComponentType.GetField("YawRightButton", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo CcwField = ComponentType.GetField("RollLeftButton", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo CwField = ComponentType.GetField("RollRightButton", BindingFlags.Public | BindingFlags.Instance);
	//private static FieldInfo _virtualField = _componentType.GetField("virtualViewAngle", BindingFlags.NonPublic | BindingFlags.Instance);

	private readonly List<string> _interaction = new List<string>();
	/*private string[] sides = new string[] { "l", "r", "f", "b", "t", "o" };
	private Quaternion emulatedView;
	private bool first = true;
	private float originalAngle;*/

	private readonly MonoBehaviour _submit;
	private readonly MonoBehaviour _left;
	private readonly MonoBehaviour _right;
	private readonly MonoBehaviour _ccw;
	private readonly MonoBehaviour _cw;
	//private float virtualAngleEmulator;
}
