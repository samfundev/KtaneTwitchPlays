using System;
using System.Collections;
using UnityEngine;


[ModuleID("freeParking")]
internal class FreeParkingShims : ComponentSolverShim
{
	private static readonly Type ComponentType = ReflectionHelper.FindType("FreeParkingScript");
	private readonly object _component;
	private readonly bool pressJail;
	private readonly KMSelectable jailButton, tokenButton, goButton;
	private readonly Array startMoney;
	private readonly float buttonCooldown = 0.1f;

	public FreeParkingShims(TwitchModule module) : base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		pressJail = _component.GetValue<bool>("pressJail");
		jailButton = _component.GetValue<KMSelectable>("jailButton");
		tokenButton = _component.GetValue<KMSelectable>("tokenButton");
		goButton = _component.GetValue<KMSelectable>("goButton");
		startMoney = _component.GetValue<Array>("startMoney");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;
		if (pressJail)
		{
			jailButton.OnInteract();
			yield return new WaitForSeconds(buttonCooldown);

		}
		else
		{
			int amount = _component.GetValue<int>("baseMoneyInt") % 5000;

			tokenButton.OnInteract();
			yield return new WaitForSeconds(buttonCooldown);


			for (int i = startMoney.Length - 1; i >= 0; --i)
			{
				object moneyInfo = startMoney.GetValue(i);
				int moneyValue = moneyInfo.GetValue<int>("value");
				KMSelectable selectable = moneyInfo.GetValue<KMSelectable>("selectable");
				while (amount >= moneyValue)
				{
					selectable.OnInteract();
					yield return new WaitForSeconds(buttonCooldown);
					amount -= moneyValue;
				}
				if (amount == 0)
					break;
			}

			goButton.OnInteract();
			yield return new WaitForSeconds(buttonCooldown);
		}

		while (!_component.GetValue<bool>("moduleSolved"))
		{
			yield return true;
		}
	}

}
