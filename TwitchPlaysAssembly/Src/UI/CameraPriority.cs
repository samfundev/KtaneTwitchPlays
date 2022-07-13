using System;

[Flags]
public enum CameraPriority
{
	/// <summary>This module is unviewed.</summary>
	Unviewed,
	/// <summary>This module has been interacted with.</summary>
	Interacted,
	/// <summary>This module is claimed.</summary>
	Claimed,
	/// <summary>This module is explicitly viewed.</summary>
	Viewed,
	/// <summary>This module is pinned.</summary>
	Pinned
}
