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
			switch (module.Solver.ModInfo.moduleID)
			{
				// Cookie Jars
				case "cookieJars":
					return Mathf.Clamp(module.Bomb.BombSolvableModules * Points * TwitchPlaySettings.data.DynamicScoreMultiplier, 1f, float.PositiveInfinity);
				// Forget Everything
				// Forget Enigma
				case "HexiEvilFMN":
				case "forgetEnigma":
					return Mathf.Clamp(module.Bomb.BombSolvableModules, 1, 100) * Points * TwitchPlaySettings.data.DynamicScoreMultiplier;
				default:
					return module.Bomb.BombSolvableModules * Points * TwitchPlaySettings.data.DynamicScoreMultiplier;
			}
		}

		public override string Description => Points.Pluralize("point") + " per module";
	}
}