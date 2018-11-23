using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Missions;
using UnityEngine;

public static class MissionBinderCommands
{
	[Command(@"select")]
	public static IEnumerator Select(FloatingHoldable holdable) => SelectOnPage(holdable);

	[Command(@"select +(\d+)")]
	public static IEnumerator SelectIndex(FloatingHoldable holdable, [Group(1)] int index) => SelectOnPage(holdable, index: index);

	[Command(@"select +(?!\d+$)(.+)")]
	public static IEnumerator SelectSearch(FloatingHoldable holdable, [Group(1)] string search) => SelectOnPage(holdable, search: search.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries));

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
		var currentPage = holdable.GetComponentsInChildren<Selectable>(false).FirstOrDefault(x => x != holdable);
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

	private static IEnumerator SelectOnPage(FloatingHoldable holdable, int index = 0, IList<string> search = null)
	{
		if (index > 0 || search != null)
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
				if (index > 0)
				{
					if (++i == index)
					{
						newSelectable = _currentSelectables[_currentSelectableIndex];
						break;
					}
				}
				// Search mode
				else
				{
					var mission = _currentSelectables[_currentSelectableIndex].GetComponent<MissionTableOfContentsMissionEntry>();

					if (mission == null)
						continue;

					string missionName = mission.EntryText.text.ToLowerInvariant();

					// Trigger the mission if EITHER
					// • the first search term matches the mission ID (e.g., "2.1"); OR
					// • all search terms are found in the mission name
					if (mission.SubsectionText.text.Equals(search?[0], StringComparison.InvariantCultureIgnoreCase) || search.All(s => missionName.ContainsIgnoreCase(s)))
					{
						newSelectable = _currentSelectables[_currentSelectableIndex];
						break;
					}
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

		// Prevent users from trying to start the tutorial missions, assuming they actually selected a mission.
		var selectedMission = _currentSelectable.GetComponent<MissionTableOfContentsMissionEntry>();
		if (selectedMission != null && MissionManager.Instance.GetMission(selectedMission.MissionID).IsTutorial)
		{
			Audio.PlaySound(KMSoundOverride.SoundEffect.Strike, _currentSelectable.transform);
			IRCConnection.SendMessage("The tutorial missions are currently unsupported and cannot be selected.");
			yield break;
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
