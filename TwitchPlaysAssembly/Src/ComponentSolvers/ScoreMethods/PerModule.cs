using UnityEngine;

namespace TwitchPlays.ScoreMethods
{
	public class PerModule : ScoreMethod
	{
		private readonly TwitchModule module;

		public PerModule(float points, TwitchModule module) : base(points)
		{
			this.module = module;
		}

		public override float CalculateScore(string user)
		{
			return module.Solver.ModInfo.moduleID switch
			{
				// Cookie Jars
				"cookieJars" => Mathf.Clamp(module.Bomb.BombSolvableModules * Points * TwitchPlaySettings.data.DynamicScoreMultiplier, 1f, float.PositiveInfinity),
				// Forget Everything
				// Forget Enigma
				"HexiEvilFMN" or "forgetEnigma" => Mathf.Clamp(module.Bomb.BombSolvableModules, 1, 100) * Points * TwitchPlaySettings.data.DynamicScoreMultiplier,
				_ => module.Bomb.BombSolvableModules * Points * TwitchPlaySettings.data.DynamicScoreMultiplier,
			};
		}
		public override string Description => Points.Pluralize("point") + " per module";
	}
}