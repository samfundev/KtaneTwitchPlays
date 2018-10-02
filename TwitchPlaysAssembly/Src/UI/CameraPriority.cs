public enum CameraPriority
{
	/// <summary>This module was explicitly unviewed OR never interacted with.</summary>
	Unviewed = 0,
	/// <summary>This module was interacted with but not claimed or explicitly viewed.</summary>
	Interacted = 1,
	/// <summary>This module was claimed, but not explicitly viewed.</summary>
	Claimed = 2,
	/// <summary>This module was explicitly viewed.</summary>
	Viewed = 3,
	/// <summary>This module is pinned.</summary>
	Pinned = 4
}
