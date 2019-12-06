using System;
using JetBrains.Annotations;

/// <summary>Marks a method as a command understood by the Twitch Plays system.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[MeansImplicitUse]
sealed class CommandAttribute : Attribute
{
	/// <summary>
	///     A regular expression that matches all forms of the command. Note that which part of the command is matched against
	///     this regex depends on what type of command it is (global, module, bomb, or holdable).</summary>
	public string Regex { get; }

	/// <summary>Access level required to use this command.</summary>
	public AccessLevel AccessLevel { get; }

	/// <summary>Access level required to use this command when anarchy mode is enabled.</summary>
	public AccessLevel AccessLevelAnarchy { get; }

	/// <summary>Constructor.</summary>
	public CommandAttribute(string regex, AccessLevel accessLevel = AccessLevel.User, AccessLevel accessLevelAnarchy = AccessLevel.User)
	{
		Regex = regex == null ? null : $"^{regex}$";
		AccessLevel = accessLevel;
		AccessLevelAnarchy = accessLevelAnarchy;
	}
}

/// <summary>Marks a module command as a command that can be used even if the module is already solved.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
sealed class SolvedAllowedAttribute : Attribute { }

/// <summary>Specifies a bomb command or game command that can only be used in the Elevator room.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
sealed class ElevatorOnlyAttribute : Attribute { }
/// <summary>Specifies a bomb command or game command that cannot be used in the Elevator room.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
sealed class ElevatorDisallowedAttribute : Attribute { }

/// <summary>Specifies a command that can only be used if the EnableDebuggingCommands setting is enabled.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
sealed class DebuggingOnlyAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
sealed class GroupAttribute : Attribute
{
	public int GroupIndex { get; }
	public GroupAttribute(int groupIndex) { GroupIndex = groupIndex; }
}
