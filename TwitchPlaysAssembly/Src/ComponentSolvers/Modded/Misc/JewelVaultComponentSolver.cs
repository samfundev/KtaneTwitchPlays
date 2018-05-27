using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

public class JewelVaultComponentSolver : ComponentSolver
{
	public JewelVaultComponentSolver(BombCommander bombCommander, BombComponent bombComponent) : base(bombCommander, bombComponent)
	{
		object _component = bombComponent.GetComponent(_componentType);
		_wheels = (KMSelectable[])_wheelsField.GetValue(_component);
		_resetButton = (KMSelectable)_resetButtonField.GetValue(_component);
		_submitButton = (KMSelectable)_submitButtonField.GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Turn the wheels with !{0} turn 3 [Turn wheel 3 (range is 1-4)]. Reset the wheels with !{0} reset. Submit with !{0} submit.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.RegexMatch(out Match match, "^reset$"))
		{
			yield return null;
			yield return DoInteractionClick(_resetButton);

		}
		else if (inputCommand.RegexMatch(out match, "^submit$"))
		{
			yield return null;
			yield return DoInteractionClick(_submitButton);
			yield return null;
			yield return "solve";
		}
		else if (inputCommand.RegexMatch(out match, "^turn ([1-4])$"))
		{
			var wheel = int.Parse(match.Groups[1].Value) - 1;
			yield return null;
			yield return DoInteractionClick(_wheels[wheel]);
		}
	}

	static JewelVaultComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("jewelWheelsScript");
		_wheelsField = _componentType.GetField("wheels", BindingFlags.Public | BindingFlags.Instance);
		_resetButtonField = _componentType.GetField("resetButton", BindingFlags.Public | BindingFlags.Instance);
		_submitButtonField = _componentType.GetField("submitButton", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _wheelsField = null;
	private static FieldInfo _resetButtonField = null;
	private static FieldInfo _submitButtonField = null;

	private readonly KMSelectable[] _wheels = null;
	private readonly KMSelectable _resetButton = null;
	private readonly KMSelectable _submitButton = null;
}
