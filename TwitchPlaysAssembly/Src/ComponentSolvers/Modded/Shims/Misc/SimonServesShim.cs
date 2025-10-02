using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[ModuleID("simonServes")]
internal class SimonServesShim : ComponentSolverShim
{
	private static readonly Type ComponentType = ReflectionHelper.FindType("simonServesScript");
	private readonly object _component;
	private readonly string serialNumber;
	private readonly int[,,] people; //holds each person's priority list for each course
	private readonly string[] peopleOrder = new string[] { "Riley", "Brandon", "Gabriel", "Veronica", "Wendy", "Kayle" };
	private readonly string[] foodColors = new string[] { "Red", "White", "Blue", "Brown", "Green", "Yellow", "Orange", "Pink" };
	private bool moduleSolved, tpStrike = false;

	public SimonServesShim(TwitchModule module) : base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);

		module.BombComponent.OnPass += _ =>
		{
			moduleSolved = true;
			return false;
		};

		module.BombComponent.OnStrike += _ =>
		{
			tpStrike = true;
			return false;
		};

		people = _component.GetValue<int[,,]>("people");
		serialNumber = _component.GetValue<string>("serialNumber");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;
		int stage = _component.GetValue<int>("stage");
		if (stage == -1)
		{
			yield return RespondToCommandInternal("KNOCK");

			int oldStageVal = stage;
			do
			{
				yield return true;
				stage = _component.GetValue<int>("stage");
			} while (oldStageVal == stage);
		}

		while (stage >= 0 && stage <= 3)
		{
			List<string[]> answerList = GetServingAnswers(stage);
			string answer = "Serve " + string.Join(" ", answerList.Select(servingPair => servingPair[0][0] + (servingPair[1] == "Brown" ? "N" : servingPair[1][0].ToString())).ToArray());
			yield return RespondToCommandInternal(answer);

			if (tpStrike)
			{
				yield return "sendtochaterror There was an issue with the autosolver. Contact the developer";
				yield break;
			}

			int oldStageVal = stage;
			do
			{
				yield return true;
				stage = _component.GetValue<int>("stage");
			} while (oldStageVal == stage);
		}

		if (stage == 4)
		{
			int payingBillIndex = GetBillIndex();
			string answer = "BILL " + (payingBillIndex == -1 ? "Table" : peopleOrder[payingBillIndex][0].ToString());
			yield return RespondToCommandInternal(answer);
			if (tpStrike)
			{
				yield return "sendtochaterror There was an issue with the autosolver. Contact the developer";
				yield break;
			}
		}

		while (!moduleSolved)
		{
			yield return true;
		}
	}

	private List<string[]> GetServingAnswers(int stage)
	{
		int[] servingOrder = _component.GetValue<int[]>("servingOrder");
		int[] foods = _component.GetValue<int[]>("foods");
		List<int> takenFoods = new List<int>();
		for (int personIndex = 0; personIndex < 6; personIndex++)
		{
			int personNum = servingOrder[personIndex];

			for (int i = 0; i < 8; i++)
			{
				int mostDesiredFood = people[personNum, stage, i];

				if (foods.Contains(mostDesiredFood) && !takenFoods.Contains(mostDesiredFood))
				{
					takenFoods.Add(mostDesiredFood);
					break;
				}
			}
		}

		List<string[]> answer = new List<string[]>();
		for (int i = 0; i < 6; i++)
		{
			answer.Add(new string[] { peopleOrder[servingOrder[i]], foodColors[takenFoods[i]] });
		}

		return answer;
	}

	private int GetBillIndex()
	{
		int mainCourseLastPick = _component.GetValue<int>("mainCourseLastPick");
		int payingBillIndex = -1;
		for (int i = 0; i < 6; i++)
		{
			if (peopleOrder[(mainCourseLastPick + i) % 6].ToUpper().Intersect(serialNumber.ToUpper()).Any())
			{
				payingBillIndex = (mainCourseLastPick + i) % 6;
				break;
			}
		}

		return payingBillIndex;
	}
}
