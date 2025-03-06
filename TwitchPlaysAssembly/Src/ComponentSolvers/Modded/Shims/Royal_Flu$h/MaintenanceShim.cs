using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ModuleID("maintenance")]
public class MaintenanceShim : ComponentSolverShim
{
	public MaintenanceShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_left = _component.GetValue<KMSelectable>("jobLeft");
		_right = _component.GetValue<KMSelectable>("jobRight");
		_submit = _component.GetValue<KMSelectable>("repairBut");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<string> jobList = _component.GetValue<List<string>>("jobEntries");
		List<string> jobOrder = _component.GetValue<List<string>>("correctJobsOrder");
		string writeOff = _component.GetValue<string>("writeOff");
		if (writeOff == "true")
		{
			int index = _component.GetValue<int>("jobIndex");
			yield return SelectIndex(index, jobList.IndexOf("Write-off"), jobList.Count, _right, _left);
			yield return DoInteractionClick(_submit, 0);
		}
		else
		{
			while (_component.GetValue<int>("stage") != 5)
			{
				int index = _component.GetValue<int>("jobIndex");
				yield return SelectIndex(index, jobList.IndexOf(jobOrder[_component.GetValue<int>("stage") - 1]), jobList.Count, _right, _left);
				yield return DoInteractionClick(_submit, 0);
				if (_component.GetValue<int>("stage") != 5)
					yield return new WaitForSeconds(0.5f);
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("maintenanceScript", "maintenance");

	private readonly object _component;
	private readonly KMSelectable _left;
	private readonly KMSelectable _right;
	private readonly KMSelectable _submit;
}
