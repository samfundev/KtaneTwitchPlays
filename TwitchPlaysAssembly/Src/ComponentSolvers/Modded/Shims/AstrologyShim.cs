public class AstrologyShim : ComponentSolverShim
{
	public AstrologyShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());

		module.BombComponent.OnStrike += _ =>
		{
			ReleaseHeldButtons();
			return false;
		};
	}
}
