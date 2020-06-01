using System;
using System.Collections;

public class SwanShim : ComponentSolverShim
{
	public SwanShim(TwitchModule module)
		: base(module, "theSwan")
	{
		_component = Module.BombComponent.GetComponent(ComponentType);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		int resetsPreCommand = _component.GetValue<int>("systemResetCounter");

		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;

		// Award a point upon a successful system reset.
		if (_component.GetValue<int>("systemResetCounter") != resetsPreCommand)
			yield return "awardpoints 1";
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("theSwanScript");
	private readonly object _component;
}
