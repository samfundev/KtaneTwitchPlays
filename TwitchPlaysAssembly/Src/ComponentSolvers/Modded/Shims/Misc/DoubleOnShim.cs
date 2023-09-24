using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class DoubleOnShim : ReflectionComponentSolverShim
{
	public DoubleOnShim(TwitchModule module)
		: base(module, "DoubleOnModule", "double_on")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondShimmed(string[] split, string command)
	{
		if (!command.Equals("read"))
		{
			if (!Regex.IsMatch(command, @"^([1-9]\d*[rgbcmy]{2}( +|$))+$")) yield break;
			string[] subCommands = split.Where(s => s.Length > 0).ToArray();
			int[] btnIndices = subCommands.Select(s => int.Parse(s.Take(s.Length - 2).Join("")) - 1).ToArray();
			if (btnIndices.Any(b => b >= _component.GetValue<object>("_puzzle").GetValue<Vector2Int[]>("LEDPositions").Length))
			{
				yield return "sendtochaterror {0}, !{1} invalid LED id.";
				yield break;
			}
		}
		yield return RespondUnshimmed(command);
	}

	private readonly char[] _validChars = { '1', '2', '3', '4', '5', '6', '7', '8', ' ', ',', '|', '-' };
}
