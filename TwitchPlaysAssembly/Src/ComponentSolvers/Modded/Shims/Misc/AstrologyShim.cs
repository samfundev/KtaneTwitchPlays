[ModuleID("spwizAstrology")]
public class AstrologyShim : ComponentSolverShim
{
	public AstrologyShim(TwitchModule module)
		: base(module)
	{
		module.BombComponent.OnStrike += _ =>
		{
			ReleaseHeldButtons();
			return false;
		};
	}
}
