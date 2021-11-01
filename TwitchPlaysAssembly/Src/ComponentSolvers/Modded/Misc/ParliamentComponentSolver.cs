using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UnityEngine;

public class ParliamentComponentSolver : ReflectionComponentSolver
{
	public ParliamentComponentSolver(TwitchModule module) :
		base(module, "ParliamentModule", "!{0} support/oppose [Supports or opposes the current bill] | !{0} fptp/mmp [Selects the specified system] | !{0} win/lose [Selects win or lose]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("support"))
		{
			yield return null;
			yield return Click(0, 0);
		}
		else if (command.Equals("oppose"))
		{
			yield return null;
			yield return Click(2, 0);
		}
		else if ((command.Equals("fptp") && !_component.GetValue<bool>("finalStage")) || (command.Equals("win") && _component.GetValue<bool>("finalStage")))
		{
			yield return null;
			yield return Click(1, 0);
		}
		else if ((command.Equals("mmp") && !_component.GetValue<bool>("finalStage")) || (command.Equals("lose") && _component.GetValue<bool>("finalStage")))
		{
			yield return null;
			yield return Click(3, 0);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		if (!_component.GetValue<bool>("timeToResign"))
		{
			for (int i = 0; i < 3; i++)
			{
				int currentPoll = _component.GetValue<int>("currentPoll");
				int numberOfBatteries = Module.BombComponent.GetComponent<KMBombInfo>().GetBatteryCount();
				int numberOfPorts = Module.BombComponent.GetComponent<KMBombInfo>().GetPortCount();
				string serialNumber = Module.BombComponent.GetComponent<KMBombInfo>().GetSerialNumber();
				bool opposedLastBill = _component.GetValue<bool>("opposedLastBill");
				Party party = _component.GetValue<Party>("party");
				BillOpener billOpener = _component.GetValue<BillOpener>("billOpener");
				BillMiddle billMiddle = _component.GetValue<BillMiddle>("billMiddle");
				BillEnding billEnding = _component.GetValue<BillEnding>("billEnding");
				// Copy-pasted press handling code for the support and oppose button from Parliament
				if (currentPoll <= 17)
					yield return Click(0, .2f);
				else if (party == Party.republican && billOpener == BillOpener.oppose)
					yield return Click(0, .2f);
				else if (billOpener == BillOpener.fund && (billMiddle == BillMiddle.healthcare || billMiddle == BillMiddle.vaccines))
				{
					if (party == Party.socialist || party == Party.communist || party == Party.liberal || party == Party.birthday)
						yield return Click(0, .2f);
					else
						yield return Click(2, .2f);
				}
				else if (billMiddle == BillMiddle.hats && numberOfPorts > 2)
				{
					if (currentPoll > 51)
						yield return Click(0, .2f);
					else
						yield return Click(2, .2f);
				}
				else if (billOpener == BillOpener.condemn)
				{
					if (currentPoll > 60 || numberOfPorts == 0)
						yield return Click(0, .2f);
					else
						yield return Click(2, .2f);
				}
				else if (billEnding == BillEnding.cats)
				{
					if (serialNumber.Contains("C"))
						yield return Click(0, .2f);
					else if (serialNumber.Contains("A"))
						yield return Click(2, .2f);
					else if (serialNumber.Contains("T"))
					{
						if (numberOfBatteries % 2 == 0)
							yield return Click(0, .2f);
						else
							yield return Click(2, .2f);
					}
					else if (serialNumber.Contains("S"))
						yield return Click(0, .2f);
					else
					{
						if (numberOfPorts % 2 == 0)
							yield return Click(0, .2f);
						else
							yield return Click(2, .2f);
					}
				}
				else if ((billOpener == BillOpener.oppose || billOpener == BillOpener.prevent) && billEnding == BillEnding.waterfowl)
					yield return Click(0, .2f);
				else if (billOpener == BillOpener.endorse && billMiddle == BillMiddle.freedom)
				{
					if (numberOfBatteries > 2)
						yield return Click(0, .2f);
					else
						yield return Click(2, .2f);
				}
				else
				{
					Dictionary<Party, int[]> positions = new Dictionary<Party, int[]>()
					{
						{ Party.republican,   new[] { 2, 1, 0, -1, -3, 5 } },
						{ Party.democratic,   new[] { 4, 3, 2, -3,  3, 0 } },
						{ Party.conservative, new[] { 6, 5, 4, -5, -2, 4 } },
						{ Party.liberal,      new[] { 8, 7, 6, -7,  2, 1 } },
						{ Party.socialist,    new[] { 1, 2, 1,  0, -1, 4 } },
						{ Party.communist,    new[] { 3, 4, 3, -3, -2, 3 } },
						{ Party.birthday,     new[] { 5, 6, 6, -2, -1, 2 } },
						{ Party.lan,          new[] { 7, 8, 4, -4, -2, 1 } },
					};

					int billOpenerInt = (int) billOpener;
					int billEndingInt = (int) billEnding;

					// Swap condemn and oppose
					if (billOpenerInt == 4)
					{
						billOpenerInt++;
					}
					else if (billOpenerInt == 5)
					{
						billOpenerInt--;
					}

					// Move cats to the end of the enum.
					if (billEndingInt == 3)
					{
						billEndingInt += 2;
					}
					else if (billEndingInt > 3)
					{
						billEndingInt--;
					}

					int letterPosition = billOpenerInt * 5 + billEndingInt + positions[party][(int)billMiddle];
					Debug.Log(letterPosition);
					if (letterPosition < 0)
						letterPosition = 26 - letterPosition;
					else if (letterPosition > 25)
						letterPosition = letterPosition - 26;
					if (letterPosition < 5)
						yield return Click(0, .2f);
					else if (letterPosition >= 5 && letterPosition < 10)
					{
						if (numberOfBatteries >= numberOfPorts)
							yield return Click(0, .2f);
						else
							yield return Click(2, .2f);
					}
					else if (letterPosition >= 10 && letterPosition < 15)
					{
						if (serialNumber.Contains("V") || serialNumber.Contains("O") || serialNumber.Contains("T") || serialNumber.Contains("E"))
							yield return Click(0, .2f);
						else
							yield return Click(2, .2f);
					}
					else if (letterPosition >= 15 && letterPosition < 20)
						yield return Click(2, .2f);
					else
					{
						if (opposedLastBill)
							yield return Click(0, .2f);
						else
							yield return Click(2, .2f);
					}
				}
			}
		}
		if (_component.GetValue<bool>("timeToResign") && !_component.GetValue<bool>("finalStage"))
		{
			int numberOfVowels = _component.GetValue<int>("numberOfVowels");
			int numberOfConsonants = _component.GetValue<int>("numberOfConsonants");
			int litIndicators = Module.BombComponent.GetComponent<KMBombInfo>().GetOnIndicators().Count();
			Party party = _component.GetValue<Party>("party");
			// Copy-pasted press handling code for the fptp and mmp button from Parliament
			if (numberOfVowels >= (numberOfConsonants - 2))
			{
				if (litIndicators % 2 != 0)
				{
					if ((party == Party.socialist || party == Party.lan || party == Party.birthday || party == Party.conservative))
						yield return Click(3, .2f);
					else
						yield return Click(1, .2f);
				}
				else
				{
					if (party == Party.liberal || party == Party.communist || party == Party.socialist)
						yield return Click(1, .2f);
					else
						yield return Click(3, .2f);
				}
			}
			else
			{
				if (litIndicators % 2 == 0)
					yield return Click(3, .2f);
				else
					yield return Click(1, .2f);
			}
		}
		if (_component.GetValue<bool>("finalStage"))
		{
			int currentPoll = _component.GetValue<int>("currentPoll");
			int electionMethod = _component.GetValue<int>("electionMethod");
			int numberOfBatteries = Module.BombComponent.GetComponent<KMBombInfo>().GetBatteryCount();
			int numberOfPorts = Module.BombComponent.GetComponent<KMBombInfo>().GetPortCount();
			Party party = _component.GetValue<Party>("party");
			// Copy-pasted press handling code for the win and lose button from Parliament
			if (electionMethod == 0)
			{
				if (((numberOfBatteries + numberOfPorts) * 10) > currentPoll)
					yield return Click(1, .2f);
				else
					yield return Click(3, .2f);
			}
			else
			{
				if (currentPoll >= 40 && !(party == Party.birthday || party == Party.lan))
					yield return Click(1, .2f);
				else
					yield return Click(3, .2f);
			}
		}
	}

	enum Party { republican, democratic, conservative, liberal, socialist, communist, birthday, lan };
	enum BillOpener { prevent, promote, fund, endorse, condemn, oppose };
	enum BillMiddle { healthcare, support, hats, freedom, vaccines, rights };
	enum BillEnding { veterans, children, dogs, cats, waterfowl, liberals };
}