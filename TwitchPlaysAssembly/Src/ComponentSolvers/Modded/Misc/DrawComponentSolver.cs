using System.Collections;
using UnityEngine;

public class DrawComponentSolver : ReflectionComponentSolver
{
	public DrawComponentSolver(TwitchModule module) :
		base(module, "DrawBehav", "!{0} go [Presses the go button] | !{0} fire [Presses the fire button] | On Twitch Plays the screen will stay green for an extra 5 seconds and the screen change will be announced in chat")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("go"))
		{
			if (!_component.GetValue<bool>("_isActive"))
			{
				yield return "sendtochaterror You can't interact with the module right now.";
				yield break;
			}
			if (_component.GetValue<bool>("_pressedGo"))
			{
				yield return "sendtochaterror You cannot press go right now.";
				yield break;
			}

			yield return null;
			yield return "strike";
			yield return Click(1, 0);
			process = Module.StartCoroutine(WaitForGreen());
		}
		else if (command.Equals("fire"))
		{
			if (!_component.GetValue<bool>("_isActive"))
			{
				yield return "sendtochaterror You can't interact with the module right now.";
				yield break;
			}
			if (!_component.GetValue<bool>("_pressedGo"))
			{
				yield return "sendtochaterror You cannot press fire right now.";
				yield break;
			}

			yield return null;
			yield return Click(0, 0);
			Module.StopCoroutine(process);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running)
			{
				yield return true;
				continue;
			}

			if (!_component.GetValue<bool>("_pressedGo"))
				yield return Click(1, 0);
			while (!_component.GetValue<bool>("_canShoot")) yield return null;
			yield return Click(0, 0);
		}
	}

	private IEnumerator WaitForGreen()
	{
		while (!_component.GetValue<bool>("_canShoot"))
		{
			yield return null;
			if (!_component.GetValue<bool>("_isActive"))
				yield break;
		}
		((MonoBehaviour) _component).StopAllCoroutines();
		IRCConnection.SendMessage("The screen has turned green on Module " + Module.Code + " (Draw)!");
		yield return new WaitForSeconds(5.25f);
		if (!_component.GetValue<bool>("_isActive")) yield break;
		Material passed = _component.GetValue<Material>("screenMat");
		passed.color = Color.black;
		componentTypes["DrawBehav"].SetValue("screenMat", passed, _component);
		yield return new WaitForSeconds(.25f);
		if (!_component.GetValue<bool>("_isActive")) yield break;
		Module.BombComponent.GetComponent<KMNeedyModule>().HandleStrike();
		componentTypes["DrawBehav"].SetValue("_canShoot", false, _component);
		componentTypes["DrawBehav"].SetValue("_pressedGo", false, _component);
	}

	private Coroutine process;
}