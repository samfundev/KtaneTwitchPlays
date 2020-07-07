using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Commands for the dynamic mission generator.</summary>
/// <prefix>dmg </prefix>
public static class DMGCommands
{
	private static Type pageNavigationType = ReflectionHelper.FindType("PageNavigation");

	private static Stack<KMSelectable> _backStack;
	private static KMSelectable currentPage
	{
		get => _backStack.Peek();
	}

	/// <name>Run</name>
	/// <syntax>run</syntax>
	/// <summary>Runs the dynamic mission generator with specific text. If enabled, players can only use this in Training Mode. Otherwise, only admins can access the DMG.</summary>
	[Command("run (.+)")]
	public static IEnumerator Run(TwitchHoldable holdable, string user, bool isWhisper, [Group(1)] string text)
	{
		if (!UserAccess.HasAccess(user, AccessLevel.Admin, true))
		{
			if (!TwitchPlaySettings.data.EnableDMGForEveryone)
			{
				IRCConnection.SendMessage("Only admins can use the DMG.");
				yield break;
			}
			if (!OtherModes.TrainingModeOn)
			{
				IRCConnection.SendMessage("Only admins can use DMG when not in training mode.");
				yield break;
			}
		}

		var pageNavigation = UnityEngine.Object.FindObjectOfType(pageNavigationType);
		_backStack = pageNavigation.GetValue<Stack<KMSelectable>>("_backStack");

		while (true)
		{
			DebugHelper.Log(currentPage.name);
			if (currentPage.name.EqualsAny("PageOne(Clone)", "Home(Clone)"))
				break;
		
			KTInputManager.Instance.HandleCancel();
			yield return new WaitForSeconds(0.1f);
		}

		if (currentPage.name == "Home(Clone)")
		{
			var entryIndex = ReflectionHelper.FindType("PageManager")
				.GetValue<IList>("HomePageEntryList")
				.Cast<object>()
				.IndexOf(entry => entry.GetValue<string>("DisplayName") == "Dynamic Mission Generator");

			if (entryIndex == -1)
			{
				IRCConnection.SendMessage("DMG is not installed.");
				yield break;
			}

			currentPage.Children[entryIndex].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}

		currentPage.gameObject.Traverse<InputField>("Canvas", "InputField").text = text;
		yield return new WaitForSeconds(0.1f);
		currentPage.Children.First(button => button.name.Contains("Run")).OnInteract();
	}
}
