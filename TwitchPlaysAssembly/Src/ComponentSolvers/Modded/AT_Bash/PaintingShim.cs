using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ModuleID("Painting")]
public class PaintingShim : CommandComponentSolverShim
{
	private readonly List<TextMesh> labels;

	public PaintingShim(TwitchModule module)
		: base(module, "PaintingModule")
	{
		labels = _component
			.GetValue<object>("_painting")
			.GetValue<IList>("Cells")
			.Cast<object>()
			.Select(paintingCell => paintingCell.GetValue<TextMesh>("twitchPlaysLabel"))
			.ToList();
	}

	[Command("paint (.+) (.+)")]
	public IEnumerator Paint(string label, string _color, string cmd)
	{
		TextMesh targetCell = labels.Find(textMesh => textMesh.text == label);
		if (targetCell == null) yield break;

		yield return RespondUnshimmed(cmd);
	}
}
