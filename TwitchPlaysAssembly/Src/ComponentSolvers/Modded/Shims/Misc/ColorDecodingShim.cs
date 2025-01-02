using System.Collections;
using System.Collections.Generic;

public class ColorDecodingShim : ReflectionComponentSolverShim
{
	public ColorDecodingShim(TwitchModule module)
		: base(module, "ColorDecoding", "ColorDecoding")
	{
		_buttons = _component.GetValue<KMSelectable[]>("InputButtons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		IList constraint_tables = _component.GetValue<IList>("constraint_tables");
		while (!Module.Solved)
		{
			List<int> valid_indexes = _component.GetValue<List<int>>("valid_indexes");
			IDictionary dict = _component.GetValue<object>("display").CallMethod<IDictionary>("getConstraintHashMap");
			int indicatorNum = _component.GetValue<object>("indicator").CallMethod<int>("getTableNum");

			for (int i = 0; i < valid_indexes.Count; i++)
			{
				foreach (int key in dict.Keys)
				{
					if (!_component.GetValue<List<int>>("correctly_pressed_slots_stage").Contains(key) && dict[key].Equals(((IList) constraint_tables[indicatorNum])[valid_indexes[i]]))
					{
						yield return DoInteractionClick(_buttons[key]);
						break;
					}
				}
			}
		}
	}

	private readonly KMSelectable[] _buttons;
}
