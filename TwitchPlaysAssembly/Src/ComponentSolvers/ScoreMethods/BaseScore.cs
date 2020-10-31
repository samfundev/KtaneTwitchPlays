namespace TwitchPlays.ScoreMethods
{
	public class BaseScore : ScoreMethod
	{
		public BaseScore(float points) : base(points)
		{
		}

		public override float CalculateScore(string user) => Points;

		public override string Description => Points.Pluralize("base point");
	}
}