using System;
using System.Collections;

public class DriveInWindowShim : ComponentSolverShim
{
	public DriveInWindowShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		Module.StartCoroutine(HideNotesForOrder());
	}

	private IEnumerator HideNotesForOrder()
	{
		while (!Module.BombComponent.IsSolved)
		{
			yield return null;
			if (!_shouldHide && _component.GetValue<bool>("isPlaying"))
			{
				ModuleCameras.Instance.ModulesHidingNotes++;
				_shouldHide = true;
			}
			else if (_shouldHide && !_component.GetValue<bool>("isPlaying"))
			{
				ModuleCameras.Instance.ModulesHidingNotes--;
				_shouldHide = false;
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("DIWindowScript", "DIWindow");

	private readonly object _component;
	private bool _shouldHide;
}
