using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[ModuleID("groceryStore")]
public class GroceryStoreShim : ComponentSolverShim
{
	public GroceryStoreShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = new KMSelectable[] { (KMSelectable) AddButtonField.GetValue(_component), (KMSelectable) LeaveButtonField.GetValue(_component) };
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

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		float max = _component.GetValue<float>("maxBudget");
		Dictionary<string, float> prices = _component.GetValue<Dictionary<string, float>>("itemPrices");
		while (_component.GetValue<float>("total") + prices[_component.GetValue<string>("currentItem")] <= max)
			yield return DoInteractionClick(_buttons[0]);
		yield return DoInteractionClick(_buttons[1], 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("GroceryStoreBehav", "groceryStore");
	private static readonly FieldInfo AddButtonField = ComponentType.GetField("addToCartBtn", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo LeaveButtonField = ComponentType.GetField("payAndLeaveBtn", BindingFlags.Public | BindingFlags.Instance);

	private readonly object _component;

	private readonly KMSelectable[] _buttons;
}
