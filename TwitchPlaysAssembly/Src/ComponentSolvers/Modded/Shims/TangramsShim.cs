using System;
using System.Collections;
using UnityEngine;

public class TangramsShim : ComponentSolverShim
{
	public TangramsShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	// Attempted copy over from PR sent to Tangrams, but it does not work properly in all places (mainly where if a selection has already been made)
	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		KMSelectable[] pins = _component.GetValue<object>("_chip").GetValue<KMSelectable[]>("PinSelectables");
		if (_component.GetValue<object>("_displayBar").GetValue<float>("Progress") != 0.0f && !ComponentType.CallMethod<bool>("IsValidConnection", _component, _component.GetValue<object>("_selectedConnection")))
		{
			((MonoBehaviour) _component).StopAllCoroutines();
			ComponentType.SetValue("Progress", 0.0f, _component.GetValue<object>("_displayBar"));
			ComponentType.CallMethod("ModuleFinish", _component, true);
			yield break;
		}
		else
			while (_component.GetValue<object>("_displayBar").GetValue<float>("Progress") != 0.0f) yield return true;
		IList valids = _component.GetValue<object>("_tangram").GetValue<IList>("_validInputOutputConnections");
		bool selectedValid = false;
		if (_component.GetValue<object>("_selectedConnection").GetValue<object>("PointA") != null)
		{
			for (int k = 0; k < valids.Count; k++)
			{
				if (valids[k].GetValue<object>("PointA").Equals(_component.GetValue<object>("_selectedConnection").GetValue<object>("PointA")))
					selectedValid = true;
			}
			if (!selectedValid)
			{
				ComponentType.CallMethod("ModuleFinish", _component, true);
				yield break;
			}
		}
		for (int j = 0; j < _component.GetValue<IList>("_previouslySelectedConnections").Count; j++)
		{
			for (int k = 0; k < valids.Count; k++)
			{
				if (valids[k].GetValue<object>("PointA").Equals(_component.GetValue<IList>("_previouslySelectedConnections")[j].GetValue<object>("PointA")))
				{
					valids.RemoveAt(k);
					k--;
				}
			}
		}
		int end = _component.GetValue<int>("RequiredInputCount") - _component.GetValue<IList>("_previouslySelectedConnections").Count;
		for (int i = 0; i < end; i++)
		{
			object choice = valids[UnityEngine.Random.Range(0, valids.Count)];
			if (!selectedValid)
				yield return DoInteractionClick(pins[Array.IndexOf(_component.GetValue<object>("_tangram").GetValue<object>("Grid").GetValue<object[]>("ExternalConnections"), choice.GetValue<object>("PointA"))]);
			else
				selectedValid = false;
			yield return DoInteractionClick(pins[Array.IndexOf(_component.GetValue<object>("_tangram").GetValue<object>("Grid").GetValue<object[]>("ExternalConnections"), choice.GetValue<object>("PointB"))], 0);
			for (int k = 0; k < valids.Count; k++)
			{
				if (valids[k].GetValue<object>("PointA").Equals(choice.GetValue<object>("PointA")))
				{
					valids.RemoveAt(k);
					k--;
				}
			}
			ComponentType.SetValue("_tpWaitingForResult", true, _component);
			while (_component.GetValue<bool>("_tpWaitingForResult")) yield return true;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("TangramModule");

	private readonly object _component;
}