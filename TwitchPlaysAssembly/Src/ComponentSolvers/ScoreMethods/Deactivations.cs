using System.Collections;
using UnityEngine;
using static NeedyComponent;

namespace TwitchPlays.ScoreMethods
{
	public class Deactivations : ScoreMethod
	{
		private readonly TwitchModule module;

		public Deactivations(float points, TwitchModule module) : base(points)
		{
			if (module == null)
				return;

			this.module = module;

			module.StartCoroutine(TrackModule());
		}

		private IEnumerator TrackModule()
		{
			NeedyComponent needyModule = module.BombComponent.GetComponent<NeedyComponent>();
			NeedyStateEnum lastState = needyModule.State;
			while (true)
			{
				switch (needyModule.State)
				{
					case NeedyStateEnum.BombComplete:
					case NeedyStateEnum.Terminated:
						yield break;
					case NeedyStateEnum.Cooldown when lastState == NeedyStateEnum.Running:
						if (module.Claimed)
						{
							var player = module.PlayerName;
							if (!Scores.ContainsKey(player))
								Scores[player] = 0;

							Scores[player] += Points;
						}
						module.Solver.AwardRewardBonus();
						break;
				}

				lastState = needyModule.State;
				yield return new WaitForSeconds(0.1f);
			}
		}

		public override float CalculateScore(string user)
		{
			if (user == null || !Scores.TryGetValue(user, out float score))
				score = 0;
			else
				Scores.Remove(user);

			return score;
		}

		public override float CalculateDifficulty() => Mathf.InverseLerp(0.25f, 4.0f, Points);

		public override string Description => Points.Pluralize("point") + " per deactivation";
	}
}