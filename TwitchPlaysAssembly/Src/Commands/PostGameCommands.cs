using System.Collections;
using UnityEngine;

public static class PostGameCommands
{
	#region Commands
	[Command(@"(continue|back)")]
	public static IEnumerator Continue() => doButton(Object.FindObjectOfType<ResultPage>()?.ContinueButton);

	[Command(@"retry")]
	public static IEnumerator Retry(string user, bool isWhisper)
	{
		if (!TwitchPlaySettings.data.EnableRetryButton)
		{
			IRCConnection.SendMessage(TwitchPlaySettings.data.RetryInactive, user, isWhisper);
			return doButton(Object.FindObjectOfType<ResultPage>().ContinueButton);
		}
		else
		{
			TwitchPlaySettings.SetRetryReward();
			return doButton(Object.FindObjectOfType<ResultPage>().RetryButton);
		}
	}
	#endregion

	private static IEnumerator doButton(Selectable btn)
	{
		// Press the button
		btn.Trigger();
		yield return null;

		// If the button was pressed while the text “Time remaining: XXX” was still appearing,
		// the button doesn’t actually trigger but instead shortcuts this typing.
		// Therefore, just press the button a second time.
		btn.Trigger();
	}
}
