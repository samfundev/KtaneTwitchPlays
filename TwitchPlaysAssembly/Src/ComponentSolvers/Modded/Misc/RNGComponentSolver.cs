using System.Collections;
using System.Linq;
using KModkit;

public class RNGComponentSolver : ReflectionComponentSolver
{
	public RNGComponentSolver(TwitchModule module) :
		base(module, "rngScript", "!{0} generate [press generate] | !{0} accept [press accept] | On Twitch Plays this module has an additional 30 seconds")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if ("generate".StartsWith(command))
		{
			yield return null;
			yield return Click(0, 0);
			yield return "sendtochat Displayed number: " + _component.GetValue<int>("randomNL") + _component.GetValue<int>("randomNR");
		}
		else if ("accept".StartsWith(command))
		{
			yield return null;
			yield return Click(1, 0);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();
		var bombInfo = Module.BombComponent.GetComponent<KMBombInfo>();

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running)
			{
				yield return true;
				continue;
			}

			if (_component.GetValue<int>("isActive") == 1)
				yield return RespondToCommandInternal("generate");

			var serialEven = bombInfo.GetSerialNumberNumbers().Last() % 2 == 0;
			var serialVowel = bombInfo.GetSerialNumberLetters().Any("AEIOU".Contains);
			var numberEven = _component.GetValue<int>("randomNR") % 2 == 0;
			var numberHalf = _component.GetValue<int>("randomNL") < 5;

			var acceptable = serialEven == numberEven && serialVowel == numberHalf;
			yield return RespondToCommandInternal(acceptable ? "accept" : "generate");
		}
	}
}
