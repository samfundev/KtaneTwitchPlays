using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ThreeSentenceHorrorComponentSolver : ReflectionComponentSolver
{
	public ThreeSentenceHorrorComponentSolver(TwitchModule module) :
		base(module, "threeSentenceHorror", "!{0} escape [Attempt to escape] | On Twitch Plays footsteps and breathing will be sent to chat and the delay before \"drop everything\" is checked is extended | Note that ANYTHING you send counts towards your voice")
	{
		IRCConnection.Instance.OnMessageReceived += AddToVoice;
		Module.OnDestroyed += () => IRCConnection.Instance.OnMessageReceived -= AddToVoice;
		Module.StartCoroutine(VoiceCooldown());
		Module.StartCoroutine(HandleNotesAndChat());
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (!command.Equals("escape")) yield break;

		yield return null;
		yield return Click(0, 0);
	}

	public void AddToVoice(IRCMessage _)
	{
		if (_component.GetValue<int>("_isSpooking") == 1)
			_cooldownValue++;
		if (_cooldownValue == 3)
		{
			_component.CallMethod("Strike");
			_component.CallMethod("DebugMsg", "They heard you...");
		}
	}

	private IEnumerator VoiceCooldown()
	{
		while (!Module.BombComponent.IsSolved)
		{
			yield return null;
			if (_component.GetValue<int>("_isSpooking") == 1 && _cooldownValue != 0)
			{
				yield return new WaitForSeconds(3f);
				if (_cooldownValue != 0)
					_cooldownValue--;
			}
			else if (_cooldownValue != 0)
				_cooldownValue = 0;
		}
	}

	private IEnumerator HandleNotesAndChat()
	{
		while (!Module.BombComponent.IsSolved)
		{
			yield return null;
			if (_shouldHide && _component.GetValue<GameObject>("KeyCollectSub").GetComponent<Text>().text == "")
			{
				ModuleCameras.Instance.ModulesHidingNotes--;
				_shouldHide = false;
			}
			else if (!_shouldHide && _component.GetValue<GameObject>("KeyCollectSub").GetComponent<Text>().text != "")
			{
				ModuleCameras.Instance.ModulesHidingNotes++;
				_shouldHide = true;
			}
		}
	}

	private int _cooldownValue;
	private bool _shouldHide;
}