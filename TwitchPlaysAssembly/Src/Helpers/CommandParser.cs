using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

public static class CommandParser
{
	sealed class StaticCommand
	{
		public CommandAttribute Attr { get; }
		public MethodInfo Method { get; }
		public StaticCommand(CommandAttribute attr, MethodInfo method)
		{
			Attr = attr;
			Method = method;

			var regex = attr.Regex;
			if (regex != null)
			{
				// Handle whitespace for optional parameters
				regex = Regex.Replace(regex, @"( \(.+?\))\?", "(?:$1)?");

				// Substitute empty named capture groups with the value of the corresponding property
				regex = Regex.Replace(regex, @"\(\?<(.+)>\)", (Match match) =>
				{
					var name = match.Groups[1].Value;
					var newRegex = method.DeclaringType.GetValue<string[]>(match.Groups[1].Value).Join("|");
					return $"({newRegex})";
				});

				// Substitute empty capture groups with the regex for the corresponding parameter
				var captures = FindCaptures(regex);
				var parameterIndex = 0;
				var parameters = method.GetParameters().Where(p => p.ParameterType.EqualsAny(typeof(string), typeof(int), typeof(float), typeof(double)) && !(p.ParameterType == typeof(string) && p.Name.EqualsAny("user", "cmd"))).ToArray();
				var offset = 0;
				var typeRegexes = new Dictionary<Type, string>
				{
					{ typeof(string), ".+" },
					{ typeof(int), @"-?\d+" },
					{ typeof(float), @"-?\d+(?:\.\d+)?" },
					{ typeof(double), @"-?\d+(?:\.\d+)?" },
				};
				foreach (var index in captures)
				{
					if (regex[index + 1] != ')') continue;

					var parameterType = parameters[parameterIndex++].ParameterType;
					var typeRegex = typeRegexes[parameterType];
					regex = regex.Substring(0, index + offset) + $"({typeRegex})" + regex.Substring(index + offset + 1);
					offset += typeRegex.Length;
				}

				attr.Regex = regex;
			}
		}

		public bool HasAttribute<T>() => Method.GetCustomAttributes(typeof(T), false).Length != 0;

		private List<int> FindCaptures(string regex)
		{
			var stack = new Stack<int>();
			var indexes = new List<int>();
			for (int i = 0; i < regex.Length; i++)
			{
				var character = regex[i];
				if (character == '\\') i++;
				else if (character == '(') stack.Push(i);
				else if (character == ')')
				{
					var start = stack.Pop();
					if (i - start < 4 || regex[start + 1] != '?' || regex[start + 2] != ':') indexes.Add(start);
				}

			}

			indexes.Sort();
			return indexes;
		}
	}

	private static readonly Dictionary<Type, StaticCommand[]> _commands = new Dictionary<Type, StaticCommand[]>();

	private static StaticCommand[] GetCommands(Type type)
	{
		if (_commands.TryGetValue(type, out var cmds))
			return cmds;

		var cmdsList = new List<StaticCommand>();
		foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
		{
			var attrs = method.GetCustomAttributes(typeof(CommandAttribute), false);
			if (attrs == null || attrs.Length == 0)
				continue;
			cmdsList.Add(new StaticCommand((CommandAttribute) attrs[0], method));
		}

		// Commands with a null regex are default/fallback commands. Make sure those are last in the list.
		return _commands[type] = cmdsList.OrderBy(cmd => cmd.Attr.Regex == null).ToArray();
	}

	delegate bool TryParse<T>(string value, out T result);
	enum NumberParseResult { Success, NotOfDesiredType, Error };

	public static IEnumerator Invoke<TObj>(IRCMessage msg, string cmdStr, TObj extraObject = default, params Type[] commandTypes)
	{
		Match m = null;
		foreach (var cmd in commandTypes.SelectMany(t => GetCommands(t)).OrderBy(cmd => cmd.Attr.Regex == null))
			if (cmd.Attr.Regex == null || (m = Regex.Match(cmdStr, cmd.Attr.Regex, RegexOptions.IgnoreCase)).Success)
			{
				var enumerator = AttemptInvokeCommand(cmd, msg, cmdStr, m, extraObject);
				if (enumerator != null) return enumerator;
			}

		return null;
	}

	private static IEnumerator AttemptInvokeCommand<TObj>(StaticCommand command, IRCMessage msg, string cmdStr, Match m, TObj extraObject)
	{
		if (command.HasAttribute<DebuggingOnlyAttribute>() && !TwitchPlaySettings.data.EnableDebuggingCommands)
			return null;
		if (command.HasAttribute<ElevatorOnlyAttribute>() && !(GameRoom.Instance is ElevatorGameRoom))
			return null;
		if (command.HasAttribute<ElevatorDisallowedAttribute>() && GameRoom.Instance is ElevatorGameRoom)
			return null;

		if (!UserAccess.HasAccess(msg.UserNickName, TwitchPlaySettings.data.AnarchyMode ? command.Attr.AccessLevelAnarchy : command.Attr.AccessLevel, orHigher: true))
		{
			IRCConnection.SendMessageFormat("@{0}, you need {1} access to use that command{2}.",
				msg.UserNickName,
				UserAccess.LevelToString(TwitchPlaySettings.data.AnarchyMode ? command.Attr.AccessLevelAnarchy : command.Attr.AccessLevel),
				TwitchPlaySettings.data.AnarchyMode ? " in anarchy mode" : "");
			// Return true so that the command counts as processed
			return Enumerator.Empty();
		}

		if (extraObject is TwitchModule mdl)
		{
			if (mdl.Solved && !command.HasAttribute<SolvedAllowedAttribute>() && !TwitchPlaySettings.data.AnarchyMode)
			{
				IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AlreadySolved, mdl.Code, mdl.PlayerName, msg.UserNickName, mdl.BombComponent.GetModuleDisplayName());
				// Return true so that the command counts as processed (otherwise you get the above message multiple times)
				return Enumerator.Empty();
			}

			if (mdl.Hidden)
			{
				IRCConnection.SendMessage($"Module {mdl.Code} is currently hidden and cannot be interacted with.");
				return Enumerator.Empty();
			}
		}

		// Check if commands are allowed to be sent to a holdable as they could be disabled in the settings.
		if (extraObject is TwitchHoldable holdable && !UserAccess.HasAccess(msg.UserNickName, AccessLevel.SuperUser, true) &&
			(
				(!TwitchPlaySettings.data.EnableMissionBinder && holdable.CommandType == typeof(MissionBinderCommands)) ||
				(!TwitchPlaySettings.data.EnableFreeplayBriefcase && holdable.CommandType == typeof(FreeplayCommands))
			)
		)
		{
			IRCConnection.SendMessage("That holdable is currently disabled and cannot be interacted with.");
			return Enumerator.Empty();
		}

		Leaderboard.Instance.GetRank(msg.UserNickName, out Leaderboard.LeaderboardEntry entry);
		if (entry?.Team == null && extraObject is TwitchModule && OtherModes.VSModeOn)
		{
			if (TwitchPlaySettings.data.AutoSetVSModeTeams)
			{
				if (TwitchPlaySettings.data.VSModePlayerLockout) IRCConnection.SendMessage($@"{msg.UserNickName}, you have not joined a team, and the bomb has already started. Use !join to play the next bomb.");
				else IRCConnection.SendMessage($@"{msg.UserNickName}, you have not joined a team, and cannot solve modules in this mode until you do, please use !join to be assigned a team.");
			}
			else IRCConnection.SendMessage($@"{msg.UserNickName}, you have not joined a team, and cannot solve modules in this mode until you do, please use !join evil or !join good.");
			// Return true so that the command counts as processed (otherwise you get the above message multiple times)
			return Enumerator.Empty();
		}

		if (!TwitchGame.IsAuthorizedDefuser(msg.UserNickName, msg.IsWhisper))
		{
			return Enumerator.Empty();
		}

		BanData ban = UserAccess.IsBanned(msg.UserNickName);
		if (ban != null)
		{
			if (double.IsPositiveInfinity(ban.BanExpiry))
			{
				IRCConnection.SendMessage($"Sorry @{msg.UserNickName}, You were restricted from using commands. You can request permission to send commands again by talking to the staff.", msg.UserNickName, !msg.IsWhisper);
			}
			else
			{
				int secondsRemaining = (int) (ban.BanExpiry - DateTime.Now.TotalSeconds());

				int daysRemaining = secondsRemaining / 86400; secondsRemaining %= 86400;
				int hoursRemaining = secondsRemaining / 3600; secondsRemaining %= 3600;
				int minutesRemaining = secondsRemaining / 60; secondsRemaining %= 60;
				string timeRemaining = $"{secondsRemaining} seconds.";
				if (daysRemaining > 0) timeRemaining = $"{daysRemaining} days, {hoursRemaining} hours, {minutesRemaining} minutes, {secondsRemaining} seconds.";
				else if (hoursRemaining > 0) timeRemaining = $"{hoursRemaining} hours, {minutesRemaining} minutes, {secondsRemaining} seconds.";
				else if (minutesRemaining > 0) timeRemaining = $"{minutesRemaining} minutes, {secondsRemaining} seconds.";

				IRCConnection.SendMessage($"Sorry @{msg.UserNickName}, You were temporarily restricted from using commands. You can participate again in {timeRemaining} or request permission by talking to the staff.", msg.UserNickName, !msg.IsWhisper);
			}
			return Enumerator.Empty();
		}

		var parameters = command.Method.GetParameters();
		var groupAttrs = parameters.Select(p => (GroupAttribute) p.GetCustomAttributes(typeof(GroupAttribute), false).FirstOrDefault()).ToArray();
		var arguments = new object[parameters.Length];
		var groupIndex = 1;
		for (int i = 0; i < parameters.Length; i++)
		{
			// Built-in parameter names
			if (parameters[i].ParameterType == typeof(string) && parameters[i].Name == "user")
				arguments[i] = msg.UserNickName;
			else if (parameters[i].ParameterType == typeof(string) && parameters[i].Name == "cmd")
				arguments[i] = cmdStr;
			else if (parameters[i].ParameterType == typeof(bool) && parameters[i].Name == "isWhisper")
				arguments[i] = msg.IsWhisper;
			else if (parameters[i].ParameterType == typeof(IRCMessage))
				arguments[i] = msg;
			else if (parameters[i].ParameterType == typeof(KMGameInfo))
				arguments[i] = TwitchPlaysService.Instance.GetComponent<KMGameInfo>();
			else if (parameters[i].ParameterType == typeof(KMGameInfo.State))
				arguments[i] = TwitchPlaysService.Instance.CurrentState;
			else if (parameters[i].ParameterType == typeof(FloatingHoldable) && extraObject is TwitchHoldable twitchHoldable)
				arguments[i] = twitchHoldable.Holdable;

			// Object we passed in (module, bomb, holdable)
			else if (parameters[i].ParameterType.IsAssignableFrom(typeof(TObj)))
				arguments[i] = extraObject;
			// Capturing groups from the regular expression
			else if (m != null)
			{
				var group = m.Groups[groupIndex++];
				NumberParseResult result;

				// Helper function to parse numbers (ints, floats, doubles)
				NumberParseResult IsNumber<TNum>(TryParse<TNum> tryParse)
				{
					var isNullable = parameters[i].ParameterType == typeof(Nullable<>).MakeGenericType(typeof(TNum));
					if (parameters[i].ParameterType != typeof(TNum) && !isNullable)
						return NumberParseResult.NotOfDesiredType;

					if (group.Success && tryParse(group.Value, out TNum rslt))
					{
						arguments[i] = rslt;
						return NumberParseResult.Success;
					}
					if (isNullable)
						return NumberParseResult.Success;
					IRCConnection.SendMessage(group.Success ? "@{0}, “{1}” is not a valid number." : "@{0}, the command could not be parsed.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName, group.Success ? group.Value : null);
					return NumberParseResult.Error;
				}

				// Strings
				if (parameters[i].ParameterType == typeof(string))
					arguments[i] = group.Success ? group.Value : null;

				// Booleans — only specifies whether the group matched or not
				else if (parameters[i].ParameterType == typeof(bool))
					arguments[i] = group.Success;

				// Numbers (int, float, double); includes nullables
				else if (
					(result = IsNumber<int>(int.TryParse)) != NumberParseResult.NotOfDesiredType ||
					(result = IsNumber<float>(float.TryParse)) != NumberParseResult.NotOfDesiredType ||
					(result = IsNumber<double>(double.TryParse)) != NumberParseResult.NotOfDesiredType)
				{
					if (result == NumberParseResult.Error)
						return Enumerator.Empty();
				}
				else if (parameters[i].ParameterType == typeof(Group))
					arguments[i] = group;
			}
			else if (parameters[i].IsOptional)
				arguments[i] = parameters[i].DefaultValue;
			else
			{
				IRCConnection.SendMessage("@{0}, this is a bug; please notify the devs. Error: the “{1}” command has an unrecognized parameter “{2}”. It expects a type of “{3}”, and the extraObject is of type “{4}”.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName, command.Method.Name, parameters[i].Name, parameters[i].ParameterType.Name, extraObject?.GetType().Name);
				return Enumerator.Empty();
			}
		}

		if ((TwitchPlaySettings.data.AnarchyMode ? command.Attr.AccessLevelAnarchy : command.Attr.AccessLevel) > AccessLevel.Defuser)
			AuditLog.Log(msg.UserNickName, UserAccess.HighestAccessLevel(msg.UserNickName), msg.Text);

		var invokeResult = command.Method.Invoke(command.Method.IsStatic ? null : (object) extraObject, arguments);
		if (invokeResult is bool invRes && invRes)
			return null;
		else if (invokeResult is IEnumerator coroutine)
			return coroutine;
		else if (invokeResult != null)
			IRCConnection.SendMessage("@{0}, this is a bug; please notify the devs. Error: the “{1}” command returned something unrecognized.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName, command.Method.Name);

		return Enumerator.Empty();
	}
}