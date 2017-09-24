using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class PostGameCommander : ICommandResponder
{
    #region Constructors
    static PostGameCommander()
    {
        _resultPageType = CommonReflectedTypeInfo.ResultPageType;
        _continueButtonField = _resultPageType.GetField("ContinueButton", BindingFlags.Public | BindingFlags.Instance);
        _retryButtonField = _resultPageType.GetField("RetryButton", BindingFlags.Public | BindingFlags.Instance);

        _selectableType = ReflectionHelper.FindType("Selectable");
        _interactMethod = _selectableType.GetMethod("HandleInteract", BindingFlags.Public | BindingFlags.Instance);
        _interactEndedMethod = _selectableType.GetMethod("OnInteractEnded", BindingFlags.Public | BindingFlags.Instance);
        _setHighlightMethod = _selectableType.GetMethod("SetHighlight", BindingFlags.Public | BindingFlags.Instance);
    }

    public PostGameCommander(MonoBehaviour resultsPage)
    {
        ResultsPage = resultsPage;
    }
    #endregion

    #region Interface Implementation
    public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier)
    {
        MonoBehaviour button = null;

        if (message.Equals("!continue", StringComparison.InvariantCultureIgnoreCase) ||
            message.Equals("!back", StringComparison.InvariantCultureIgnoreCase))
        {
            button = ContinueButton;
        }
        else if (message.Equals("!retry", StringComparison.InvariantCultureIgnoreCase))
        {
            button = RetryButton;
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
    public MonoBehaviour ContinueButton
    {
        get
        {
            return (MonoBehaviour)_continueButtonField.GetValue(ResultsPage);
        }
    }

    public MonoBehaviour RetryButton
    {
        get
        {
            return (MonoBehaviour)_retryButtonField.GetValue(ResultsPage);
        }
    }
    #endregion

    #region Private Methods
    private void DoInteractionStart(MonoBehaviour selectable)
    {
        _interactMethod.Invoke(selectable, null);
    }

    private void DoInteractionEnd(MonoBehaviour selectable)
    {
        _interactEndedMethod.Invoke(selectable, null);
        _setHighlightMethod.Invoke(selectable, new object[] { false });
    }
    #endregion

    #region Private Readonly Fields
    private readonly MonoBehaviour ResultsPage = null;
    #endregion

    #region Private Static Fields
    private static Type _resultPageType = null;
    private static FieldInfo _continueButtonField = null;
    private static FieldInfo _retryButtonField = null;

    private static Type _selectableType = null;
    private static MethodInfo _interactMethod = null;
    private static MethodInfo _interactEndedMethod = null;
    private static MethodInfo _setHighlightMethod = null;
    #endregion
}

