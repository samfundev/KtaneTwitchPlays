using System;
using System.Collections;
using System.Reflection;

public class FlavorTextComponentSolver : ComponentSolver
{
	public FlavorTextComponentSolver(TwitchModule module)
		: base(module)
	{
		object component = module.BombComponent.GetComponent(ComponentType);
		_buttons = (KMSelectable[]) ButtonsField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press the button labeled Y with !{0} y. Press the button labeled N with !{0} n.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim();
		switch (inputCommand)
		{
			case "y":
				yield return null;
				yield return DoInteractionClick(_buttons[1]);
				yield break;
			case "n":
				yield return null;
				yield return DoInteractionClick(_buttons[0]);
				yield break;
			default:
				yield break;
		}
	}

	static FlavorTextComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("FlavorText");
		ButtonsField = ComponentType.GetField("buttons", BindingFlags.Instance | BindingFlags.Public);
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo ButtonsField;

	private readonly KMSelectable[] _buttons;
}