using System.Collections;
using UnityEngine;

namespace TwitchPlays.ScoreMethods
{
	public class ClaimTime : ScoreMethod
	{
		private readonly TwitchModule module;
		private readonly NeedyComponent needyComponent;

		public ClaimTime(float points, TwitchModule module) : base(points)
		{
			this.module = module;

			module.StartCoroutine(TrackModule());

			needyComponent = module.BombComponent.GetComponent<NeedyComponent>();
		}

		private IEnumerator TrackModule()
		{
			var lastTime = Time.time;
			while (!module.Solved)
			{
				if (module.Claimed && (needyComponent == null || needyComponent.State != NeedyComponent.NeedyStateEnum.Running))
				{
					var player = module.PlayerName;
					if (!Scores.ContainsKey(player))
						Scores[player] = 0;

					Scores[player] += (Time.time - lastTime) * Points;
				}

				lastTime = Time.time;
				yield return null;
			}
		}

		public override float CalculateScore(string user)
		{
			if (!Scores.TryGetValue(user, out float score))
				score = 0;
			else
				Scores.Remove(user);

			return score;
		}

		public override string Description => Points.Pluralize("point") + " per second";
	}
}