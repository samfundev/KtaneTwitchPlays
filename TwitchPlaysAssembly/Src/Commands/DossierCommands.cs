using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Commands that can be used in the dossier menu.</summary>
/// <prefix>dossier </prefix>
public static class DossierCommands
{
	/// <name>Select</name>
	/// <syntax>select</syntax>
	/// <summary>Selects the currently highlighted item.</summary>
	[Command(@"select")]
	public static IEnumerator Select(FloatingHoldable holdable, string user) => SelectOnPage(holdable, user);

	/// <name>Select Index</name>
	/// <syntax>select [index]</syntax>
	/// <summary>Selects an item based on it's index on the menu.</summary>
	[Command(@"select (\d+)")]
	public static IEnumerator SelectIndex(FloatingHoldable holdable, string user, [Group(1)] int index) => SelectOnPage(holdable, user, index: index);

	/// <name>Up / Down</name>
	/// <syntax>up (amount)\ndown (amount)</syntax>
	/// <summary>Moves up or down the menu by a number items.</summary>
	[Command(@"(up|u|down|d)(?: (\d+))?")]
	public static IEnumerator Navigate([Group(1)] string direction, [Group(2)] int? optAmount)
	{
		var amount = optAmount ?? 1;
		var offset = direction.StartsWith("u", StringComparison.InvariantCultureIgnoreCase) ? -1 : 1;
		for (int i = 0; i < amount; i++)
		{
			MoveOnPage(offset);
			yield return new WaitForSeconds(0.2f);
		}
	}

	public static void InitializePage(FloatingHoldable holdable)
	{
		Selectable holdableSelectable = holdable.GetComponent<Selectable>();
		var currentPage = Array.Find(holdable.GetComponentsInChildren<Selectable>(false), x => x != holdableSelectable);
		_currentSelectable = currentPage?.GetCurrentChild();
		_currentSelectable?.HandleSelect(true);
		_currentSelectables = currentPage?.Children;
		_currentSelectableIndex = _currentSelectables?.IndexOf(sel => sel == _currentSelectable) ?? -1;
	}

	#region Private Methods
	private static void MoveOnPage(int offset)
	{
		if (_currentSelectableIndex == -1 || _currentSelectables == null || _currentSelectable == null)
			return;

		int oldSelectableIndex = _currentSelectableIndex;

		for (_currentSelectableIndex += offset; _currentSelectableIndex >= 0 && _currentSelectableIndex < _currentSelectables.Length; _currentSelectableIndex += offset)
		{
			if (_currentSelectables[_currentSelectableIndex] == null)
				continue;
			_currentSelectable.HandleDeselect();
			_currentSelectable = _currentSelectables[_currentSelectableIndex];
			_currentSelectable.HandleSelect(true);
			return;
		}

		_currentSelectableIndex = oldSelectableIndex;
	}

	private static IEnumerator SelectOnPage(FloatingHoldable holdable, string user, int index = 0)
	{
		if (TwitchPlaysService.Instance.CurrentState != KMGameInfo.State.Gameplay)
			yield break;

		// If the dossier page changes while we're holding the menu, we need to reinitialize the menu.
		// Dossier Modifier can do this.
		InitializePage(holdable);

		if (index > 0)
		{
			if ((_currentSelectables == null) || (index > _currentSelectables.Length))
				yield break;

			int oldSelectableIndex = _currentSelectableIndex;

			int i = 0;
			Selectable newSelectable = null;
			for (_currentSelectableIndex = 0; _currentSelectableIndex < _currentSelectables.Length; ++_currentSelectableIndex)
			{
				if (_currentSelectables[_currentSelectableIndex] == null)
					continue;

				// Index mode
				if (++i == index)
				{
					newSelectable = _currentSelectables[_currentSelectableIndex];
					break;
				}
			}

			if (newSelectable == null)
			{
				_currentSelectableIndex = oldSelectableIndex;
				yield break;
			}

			_currentSelectable.HandleDeselect();
			_currentSelectable = newSelectable;
		}

		if (_currentSelectable == null)
			yield break;

		_currentSelectable.HandleSelect(true);
		KTInputManager.Instance.SelectableManager.Select(_currentSelectable, true);

		// Prevent users from trying to select anything that is not the dossier modifier button
		if (!_currentSelectable.name.StartsWith("SolveDossierModifier"))
		{
			Audio.PlaySound(KMSoundOverride.SoundEffect.Strike, _currentSelectable.transform);
			IRCConnection.SendMessage("This option is not accessable to Twitch Plays users.");
			yield break;
		}

		// Award any Dossier Modifier solves to the user who ran the command
		var dossierModifiers = TwitchGame.Instance.Modules.Select(module => module.Solver).Where(solver => solver.ModInfo.moduleID == "TDSDossierModifier");
		foreach (ComponentSolver solver in dossierModifiers)
		{
			solver.ForceAwardSolveToNickName(user);
		}

		KTInputManager.Instance.SelectableManager.HandleInteract();
		_currentSelectable.OnInteractEnded();

		yield return null;
		InitializePage(holdable);
	}

	#endregion

	#region Private Fields
	private static Selectable _currentSelectable = null;
	private static int _currentSelectableIndex = -1;
	private static Selectable[] _currentSelectables = null;
	#endregion
}