using System.Collections;
using UnityEngine;

[ModuleID("CornflowerButtonModule")]
public class CornflowerButtonComponentSolver : ReflectionComponentSolver
{
	public CornflowerButtonComponentSolver(TwitchModule module) :
		base(module, "CornflowerButtonScript", "!{0} press (#) [Presses the button (optionally '#' times)] | !{0} camwalloff [Turns off camera wall for 5 seconds] | !<tpID> selectables  [Outputs the list of selectables and their IDs for the module with TP ID 'tpID'] | !<tpID> highlight <selID> [Highlights the selectable with ID 'selID' on the module with TP ID 'tpID'] | Pressing the alarm clock's SNOOZE button will highlight it")
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
	}
}