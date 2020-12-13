using System.Collections.Generic;
using System.Linq;

namespace TwitchPlays.ScoreMethods
{
	public abstract class ScoreMethod
	{
		public readonly float Points;
		protected readonly Dictionary<string, float> Scores = new Dictionary<string, float>();

		protected ScoreMethod(float points)
		{
			Points = points;
		}

		public abstract float CalculateScore(string user);

		public abstract string Description { get; }

		public List<string> Players => Scores.Keys.ToList();
	}
}