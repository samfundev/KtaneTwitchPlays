using System;
using System.Collections;
using System.Reflection;
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
    public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier, IRCConnection connection)
    {
        Selectable button = null;
        message = message.ToLowerInvariant();

        if (message.EqualsAny("!continue","!back"))
        {
            button = ContinueButton;
        }
        else if (message.Equals("!retry"))
        {
            if (!TwitchPlaySettings.data.EnableRetryButton)
            {
                connection.SendMessage(TwitchPlaySettings.data.RetryInactive);
            }
            button = TwitchPlaySettings.data.EnableRewardMultipleStrikes ? RetryButton : ContinueButton;
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
    public Selectable ContinueButton
    {
        get { return ResultsPage.ContinueButton; }
    }

    public Selectable RetryButton
    {
        get { return ResultsPage.RetryButton; }
    }
    #endregion

    #region Private Methods
    private void DoInteractionStart(Selectable selectable)
    {
        selectable.HandleInteract();
    }

    private void DoInteractionEnd(Selectable selectable)
    {
        selectable.OnInteractEnded();
        selectable.SetHighlight(false);
    }
    #endregion

    #region Private Readonly Fields
    private readonly ResultPage ResultsPage = null;
    #endregion
}

