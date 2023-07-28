using System;

public class LockpickMazeShim : ComponentSolverShim
{
	public LockpickMazeShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		var componentType = ReflectionHelper.FindType("LockpickMazeModule", "KritLockpickMaze");
		_component = module.BombComponent.GetComponent(componentType);

		module.BombComponent.OnStrike += _ =>
		{
			// Time can expire in the middle of a movement command, and the pawn's location is not reset when that happens.
			// So we should probably tell the person working on the module where the pawn ended up, hm?
			if (_component.GetValue<int>("TimeLeft") == 0 && _component.GetValue<bool>("LockUnlocked") == true)
			{
				char col = (char)(_component.GetValue<int>("CurrentColumn") + 'A');
				char row = (char)(_component.GetValue<int>("CurrentRow") + '1');
				IRCConnection.SendMessage($"The pawn was at position {col}{row} when time expired on module {module.Code} (Lockpick Maze). It will remain there when the module is restarted.");
			}
			return false;
		};
	}

	private readonly object _component;
}
