using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CommandParser
{
	private Queue<string> parts = new Queue<string>();
	public string OriginalCommand;

	public CommandParser(string command)
	{
		parts = new Queue<string>(command.ToLowerInvariant().SplitFull(' '));
		OriginalCommand = command;
	}

	public CommandParser Literal(params string[] literals)
	{
		Assert(DequeuePart().EqualsAny(literals));
		return this;
	}

	public CommandParser OptionalLiteral(out bool success, params string[] literals)
	{
		try
		{
			success = DequeuePart().EqualsAny(literals);
		}
		catch (ParsingFailedException)
		{
			success = false;
		}
		catch (Exception)
		{
			throw;
		}

		return this;
	}

	public CommandParser String(out string value)
	{
		value = DequeuePart();
		return this;
	}

	public CommandParser OptionalString(out string value)
	{
		try
		{
			String(out string value2);
			value = value2;
		}
		catch (ParsingFailedException)
		{
			value = null;
		}
		catch (Exception)
		{
			throw;
		}

		return this;
	}

	public CommandParser Integer(out int integer, int? min = null, int? max = null)
	{
		string part = DequeuePart();

		int? possibleInteger = part.TryParseInt();
		Assert(possibleInteger != null);

		integer = (int) possibleInteger;
		Assert(!(min != null && integer <= min) || (max != null && integer >= max));

		return this;
	}

	public CommandParser OptionalInteger(out int? integer, int? min = null, int? max = null)
	{
		try
		{
			Integer(out int integer2, min, max);
			integer = integer2;
		}
		catch (ParsingFailedException)
		{
			integer = null;
		}
		catch (Exception)
		{
			throw;
		}

		return this;
	}

	public CommandParser Float(out float number, float? min = null, float? max = null)
	{
		string part = DequeuePart();
		float? possibleFloat = part.TryParseFloat();
		Assert(possibleFloat != null);

		number = (float) possibleFloat;
		Assert(!(min != null && number <= min) || (max != null && number >= max));

		return this;
	}

	public CommandParser OptionalFloat(out float? number, float? min = null, float? max = null)
	{
		try
		{
			Float(out float number2, min, max);
			number = number2;
		}
		catch (ParsingFailedException)
		{
			number = null;
		}
		catch (Exception)
		{
			throw;
		}

		return this;
	}

	public CommandParser Options(out string option, params string[] options)
	{
		string part = DequeuePart();

		foreach (var possibleOption in options)
		{
			if (possibleOption != part)
				continue;

			option = possibleOption;
			return this;
		}

		throw new ParsingFailedException();
	}

	public CommandParser OptionalOptions(out string option, params string[] options)
	{
		try
		{
			Options(out string option2, options);
			option = option2;
		}
		catch (ParsingFailedException)
		{
			option = null;
		}
		catch (Exception)
		{
			throw;
		}

		return this;
	}

	public CommandParser Regex(string pattern, out Match match)
	{
		var text = parts.Join();
		Assert(text.RegexMatch(out match, pattern));

		parts = new Queue<string>(text.Substring(match.Index + match.Length).SplitFull(' '));

		return this;
	}

	public CommandParser OptionalRegex(string pattern, out Match match)
	{
		try
		{
			Regex(pattern, out Match match2);
			match = match2;
		}
		catch (ParsingFailedException)
		{
			match = null;
		}
		catch (Exception)
		{
			throw;
		}

		return this;
	}

	public class ParsingFailedException : Exception
	{
		public ParsingFailedException()
		{
		}

		public ParsingFailedException(string message) : base(message)
		{
		}

		public ParsingFailedException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}

	public IEnumerator RunCommand(Func<CommandParser, IEnumerator> action)
	{
		try
		{
			return action(this);
		}
		catch (ParsingFailedException)
		{
			return null;
		}
		catch (Exception)
		{
			throw;
		}
	}

	private void Assert(bool condition)
	{
		if (condition) return;
		throw new ParsingFailedException();
	}

	private string DequeuePart()
	{
		Assert(parts.Count != 0);

		return parts.Dequeue();
	}
}