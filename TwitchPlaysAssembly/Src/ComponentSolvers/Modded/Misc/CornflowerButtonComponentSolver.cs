using System.Collections;
using System.Linq;
using UnityEngine;

public class CornflowerButtonComponentSolver : ReflectionComponentSolver
{
	public CornflowerButtonComponentSolver(TwitchModule module) :
		base(module, "CornflowerButtonScript", "!{0} press (#) [Presses the button (optionally '#' times)] | !{0} camwalloff [Turns off camera wall for 5 seconds] | !{0} selectables <tpID> [Outputs the list of selectables and their IDs for the module with TP ID 'tpID'] | !{0} highlight <tpID> <selID> [Highlights the selectable with ID 'selID' on the module with TP ID 'tpID'] | Pressing the alarm clock's SNOOZE button will highlight it")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("press"))
		{
			yield return null;
			yield return Click(0);
		}
		else if (command.Equals("camwalloff"))
		{
			yield return null;
			ModuleCameras.Mode mode = TwitchGame.ModuleCameras.CameraWallMode;
			TwitchGame.ModuleCameras.CameraWallMode = ModuleCameras.Mode.Disabled;
			yield return new WaitForSeconds(5);
			TwitchGame.ModuleCameras.CameraWallMode = mode;
		}
		else if (command.StartsWith("press ") && split.Length == 2)
		{
			if (!int.TryParse(split[1], out int check)) yield break;
			if (check < 1) yield break;

			yield return null;
			for (int i = 0; i < check; i++)
				yield return Click(0);
		}
		else if (command.StartsWith("selectables ") && split.Length == 2)
		{
			if (TwitchGame.Instance.Modules.Where(x => x.Code.EqualsIgnoreCase(split[1])).Count() == 0) yield break;

			yield return null;
			TwitchModule mod = TwitchGame.Instance.Modules.Where(x => x.Code.EqualsIgnoreCase(split[1])).ToList()[0];
			Selectable[] modSels = mod.Selectable.Children.Where(x => x != null).Distinct().ToArray();
			string selectablesStr = "";
			for (int i = 0; i < modSels.Length; i++)
			{
				if (i != modSels.Length - 1)
					selectablesStr += modSels[i].name + " (" + (i + 1) + "), ";
				else
					selectablesStr += modSels[i].name + " (" + (i + 1) + ")";
			}
			yield return $"sendtochat Selectables for module {mod.Code} ({mod.BombComponent.GetModuleDisplayName()}): {selectablesStr}";
		}
		else if (command.StartsWith("highlight ") && split.Length == 3)
		{
			if (TwitchGame.Instance.Modules.Where(x => x.Code.EqualsIgnoreCase(split[1])).Count() == 0) yield break;
			TwitchModule mod = TwitchGame.Instance.Modules.Where(x => x.Code.EqualsIgnoreCase(split[1])).ToList()[0];
			Selectable[] modSels = mod.Selectable.Children.Where(x => x != null).Distinct().ToArray();
			if (!int.TryParse(split[2], out int check)) yield break;
			if (!check.InRange(1, modSels.Length)) yield break;

			yield return null;
			modSels[check - 1].SetHighlight(true);
		}
	}
}