namespace TwitchPlays.ScoreMethods
{
	public class PerAction : ScoreMethod
	{
		public PerAction(float points) : base(points)
		{
		}

		public override string Description => Points.Pluralize("point") + " per action";

		public override float CalculateScore(string user) => 0;

		public override float CalculateDifficulty() => 0;
	}
}