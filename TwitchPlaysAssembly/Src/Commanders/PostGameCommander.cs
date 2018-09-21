using System.Collections;
using UnityEngine;

public class PostGameCommander : ICommandResponder
{
	#region Constructors
	public PostGameCommander(ResultPage resultsPage)
	{
		ResultsPage = resultsPage;
	}
	#endregion

	#region Interface Implementation
	public IEnumerator RespondToCommand(Message messageObj, ICommandResponseNotifier responseNotifier)
	{
		Selectable button = null;
		string message = messageObj.Text.ToLowerInvariant().Trim();

		if (message.EqualsAny("!continue", "!back"))
		{
			button = ContinueButton;
		}
		else if (message.Equals("!retry"))
		{
			if (!TwitchPlaySettings.data.EnableRetryButton)
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.RetryInactive, messageObj.UserNickName, !messageObj.IsWhisper);
			}
			else
			{
				TwitchPlaySettings.SetRetryReward();
			}
			button = TwitchPlaySettings.data.EnableRetryButton ? RetryButton : ContinueButton;
		}

		if (button == null)
		{
			yield break;
		}

		// Press the button twice, in case the first is too early and skips the message instead
		for (int i = 0; i < 2; i++)
		{
			DoInteractionStart(button);
			yield return new WaitForSeconds(0.1f);
			DoInteractionEnd(button);
		}
	}
	#endregion

	#region Public Fields

	private Selectable ContinueButton => ResultsPage.ContinueButton;

	private Selectable RetryButton => ResultsPage.RetryButton;

	#endregion

	#region Private Methods
	private static void DoInteractionStart(Selectable selectable)
	{
		selectable.HandleInteract();
	}

	private static void DoInteractionEnd(Selectable selectable)
	{
		selectable.OnInteractEnded();
		selectable.SetHighlight(false);
	}
	#endregion

	#region Private Readonly Fields
	private readonly ResultPage ResultsPage = null;
	#endregion
}
