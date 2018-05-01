using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Rules;
using UnityEngine;

public class WireSequenceComponentSolver : ComponentSolver
{
	public WireSequenceComponentSolver(BombCommander bombCommander, WireSequenceComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_wireSequence = (List<WireSequenceComponent.WireConfiguration>) _wireSequenceField.GetValue(bombComponent);
		_upButton = bombComponent.UpButton;
		_downButton = bombComponent.DownButton;
		modInfo = ComponentSolverFactory.GetModuleInfo("WireSequenceComponentSolver", "!{0} cut 7 [cut wire 7] | !{0} down, !{0} d [next stage] | !{0} up, !{0} u [previous stage] | !{0} cut 7 8 9 d [cut multiple wires and continue] | Use the numbers shown on the module", "Wire Sequence");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant();
		Dictionary<MonoBehaviour, string> buttons = new Dictionary<MonoBehaviour, string>();

		if (inputCommand.Equals("cycle", StringComparison.InvariantCultureIgnoreCase))
		{
			yield return null;
			int page = (int)_currentPageField.GetValue(BombComponent);
			for (int i = page-1; i >= 0; i--)
			{
				IEnumerator changePage = ((WireSequenceComponent) BombComponent).ChangePage(i + 1, i);
				while (changePage.MoveNext())
					yield return changePage.Current;
			}
			for (int i = 0; i < page; i++)
			{
				yield return new WaitForSecondsWithCancel(3.0f, false);
				IEnumerator changePage = ((WireSequenceComponent) BombComponent).ChangePage(i, i + 1);
				while (changePage.MoveNext())
					yield return changePage.Current;
			}
			yield return "trycancel The cycle command of Wire Sequences was cancelled";
			yield break;
		}

		if (inputCommand.EqualsAny("up", "u"))
		{
			yield return "up";
			yield return DoInteractionClick(_upButton);
		}
		else if (inputCommand.EqualsAny("down", "d"))
		{
			yield return "down";
			yield return DoInteractionClick(_downButton, "attempting to move down.");
		}
		else
		{
			if (!inputCommand.StartsWith("cut ", StringComparison.InvariantCultureIgnoreCase) &&
				!inputCommand.StartsWith("c ", StringComparison.InvariantCultureIgnoreCase))
			{
				yield break;
			}
			string[] sequence = inputCommand.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string wireIndexString in sequence.Skip(1))
			{
				if (wireIndexString.EqualsAny("up", "u"))
				{
					buttons.Add(_upButton, "This will never cause a strike Kappa");
					break;
				}

				if (wireIndexString.EqualsAny("down", "d"))
				{
					buttons.Add(_downButton, "attempting to move down.");
					break;
				}

				if (!int.TryParse(wireIndexString, out int wireIndex)) yield break;

				wireIndex--;
				if (!CanInteractWithWire(wireIndex)) yield break;

				WireSequenceWire wire = GetWire(wireIndex);
				if (wire == null) yield break;
				if (wire.Snipped || buttons.ContainsKey(wire)) continue;
				buttons.Add(wire, string.Format("cutting Wire {0}.", wireIndex + 1));
			}

			yield return "wire sequence";
			foreach (KeyValuePair<MonoBehaviour, string> button in buttons)
			{
				yield return "trycancel";
				yield return DoInteractionClick(button.Key, button.Value);

			}
		}
	}

	private bool CanInteractWithWire(int wireIndex)
	{
		int wirePageIndex = wireIndex / 3;
		return wirePageIndex == (int)_currentPageField.GetValue(BombComponent);
	}

	private WireSequenceWire GetWire(int wireIndex)
	{
		return _wireSequence[wireIndex].Wire;
	}

	static WireSequenceComponentSolver()
	{
		_wireSequenceComponentType = typeof(WireSequenceComponent);
		_wireSequenceField = _wireSequenceComponentType.GetField("wireSequence", BindingFlags.NonPublic | BindingFlags.Instance);
		_currentPageField = _wireSequenceComponentType.GetField("currentPage", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		if (BombComponent.IsSolved) yield break;

		while (!BombComponent.IsActive)
			yield return true;
		yield return null;
		
		for (int i = 0; i < 4; i++)
		{
			while (((WireSequenceComponent)BombComponent).IsChangingPage)
			{
				yield return true;
			}

			if (!CanInteractWithWire(i * 3)) continue;
			for (int j = 0; j < 3; j++)
			{
				var wire = _wireSequence[(i*3)+j];
				if (wire.NoWire || !RuleManager.Instance.WireSequenceRuleSet.ShouldBeSnipped(wire.Color, wire.Number, wire.To) || wire.IsSnipped) continue;
				yield return DoInteractionClick(wire.Wire);
			}
			DoInteractionClick(_downButton);
			
		}
	}

	private static Type _wireSequenceComponentType = null;
	private static FieldInfo _wireSequenceField = null;
	private static FieldInfo _currentPageField = null;

	private List<WireSequenceComponent.WireConfiguration> _wireSequence = null;
	private Selectable _upButton = null;
	private Selectable _downButton = null;
}
