using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TwitchMessage : MonoBehaviour, ICommandResponseNotifier
{
    public Color normalColor = Color.white;
    public Color highlightColor = Color.white;
    public Color completeColor = Color.white;
    public Color errorColor = Color.white;
    public Color ignoreColor = Color.white;

    public Leaderboard leaderboard = null;
    public string userName = null;
    public Color userColor = Color.black;

    private Image _messageBackground = null;
    private Text _messageText = null;

    private void Awake()
    {
        _messageBackground = GetComponent<Image>();
        _messageText = GetComponentInChildren<Text>();
        _messageBackground.color = normalColor;
    }

	public void SetMessage(string text)
    {
        _messageText.text = text;
    }

    public void ProcessResponse(CommandResponse response, int value)
    {
        switch (response)
        {
            case CommandResponse.Start:
                StopAllCoroutines();
                StartCoroutine(DoBackgroundColorChange(highlightColor));
                break;
            case CommandResponse.EndNotComplete:
                StopAllCoroutines();
                StartCoroutine(DoBackgroundColorChange(normalColor));
                break;
            case CommandResponse.EndComplete:
                StopAllCoroutines();
                StartCoroutine(DoBackgroundColorChange(completeColor));
                if (leaderboard != null)
                {
                    leaderboard.AddSolve(userName, userColor);
                }
                break;
            case CommandResponse.EndError:
                StopAllCoroutines();
                StartCoroutine(DoBackgroundColorChange(errorColor));
                if (leaderboard != null)
                {
                    leaderboard.AddStrike(userName, userColor, value);
                }
                break;
            case CommandResponse.NoResponse:
                StopAllCoroutines();
                StartCoroutine(DoBackgroundColorChange(ignoreColor));
                break;

            default:
                break;
        }
    }

    private IEnumerator DoBackgroundColorChange(Color targetColor, float duration = 0.2f)
    {
        Color initialColor = _messageBackground.color;
        float currentTime = Time.time;

        while (Time.time - currentTime < duration)
        {
            float lerp = (Time.time - currentTime) / duration;
            _messageBackground.color = Color.Lerp(initialColor, targetColor, lerp);
            yield return null;
        }

        _messageBackground.color = targetColor;
    }
}
