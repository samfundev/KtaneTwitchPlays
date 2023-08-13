using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ThreeSentenceHorrorComponentSolver : ReflectionComponentSolver
{
	public ThreeSentenceHorrorComponentSolver(TwitchModule module) :
		base(module, "threeSentenceHorror", "!{0} escape [Attempt to escape] | On Twitch Plays footsteps and breathing will be sent to chat | Note that ANYTHING you send counts towards your voice")
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
			yield return new WaitForSeconds(1.5f);
			if (_cooldownValue != 0)
				_cooldownValue--;
		}
	}

	private IEnumerator HandleNotesAndChat()
	{
		while (!Module.BombComponent.IsSolved)
		{
			yield return null;
			if (_component.GetValue<GameObject>("KeyCollectSub").GetComponent<Text>().text == "I swear I left that key somewhere...")
			{
				ModuleCameras.Instance.SetHudVisibility(false);
				while (_component.GetValue<GameObject>("KeyCollectSub").GetComponent<Text>().text == "I swear I left that key somewhere...") yield return null;
				ModuleCameras.Instance.SetHudVisibility(true);
			}
			else if (_component.GetValue<GameObject>("KeyCollectSub").GetComponent<Text>().text == "I think someone's coming, I better get out of here.")
			{
				ModuleCameras.Instance.SetHudVisibility(false);
				while (_component.GetValue<GameObject>("KeyCollectSub").GetComponent<Text>().text == "I think someone's coming, I better get out of here.") yield return null;
				ModuleCameras.Instance.SetHudVisibility(true);
			}
		}
	}

	private int _cooldownValue;
}