using System.Collections;

public class GroceryStoreShim : ComponentSolverShim
{
	public GroceryStoreShim(TwitchModule module)
		: base(module, "groceryStore")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		switch (inputCommand.ToLowerInvariant().Trim())
		{
			case "pay":
			case "leave":
				inputCommand = "pay and leave";
				break;
			case "add":
			case "add to cart":
				inputCommand = "add item to cart";
				break;
		}

		IEnumerator command = RespondToCommandUnshimmed(inputCommand.ToLowerInvariant().Trim());
		while (command.MoveNext())
			yield return command.Current;
	}
}
