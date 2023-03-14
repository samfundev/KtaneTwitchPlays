using System;
using System.Collections;

public class EightPagesShim : ComponentSolverShim
{
	public EightPagesShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_page = _component.GetValue<KMSelectable>("Button");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();

		if (needyComponent.State == NeedyComponent.NeedyStateEnum.Running && _component.GetValue<bool>("pickedUpPage") && _component.GetValue<bool>("Trapped"))
		{
			Module.BombComponent.GetComponent<KMNeedyModule>().HandlePass();
			_component.CallMethod("HandleSolve");
		}

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running || _component.GetValue<bool>("pickedUpPage") || _component.GetValue<bool>("Trapped"))
			{
				yield return true;
				continue;
			}

			yield return null;
			yield return DoInteractionClick(_page);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("EightPagesModuleScript");

	private readonly object _component;
	private readonly KMSelectable _page;
}
