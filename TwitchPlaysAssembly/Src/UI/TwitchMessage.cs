using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TwitchMessage : MonoBehaviour
{
	public Color NormalColor = Color.white;
	public Color HighlightColor = Color.white;
	public Color CompleteColor = Color.white;
	public Color ErrorColor = Color.white;
	public Color IgnoreColor = Color.white;

	public string UserName;
	public Color UserColor = Color.black;

	private Image _messageBackground;
	private Text _messageText;

	private void Awake()
	{
		_messageBackground = GetComponent<Image>();
		_messageText = GetComponentInChildren<Text>();
		_messageBackground.color = NormalColor;
	}

	public void SetMessage(string text) => _messageText.text = text;

	public IEnumerator DoBackgroundColorChange(Color targetColor, float duration = 0.2f)
	{
		Color initialColor = _messageBackground.color;
		float currentTime = Time.time;

		while (Time.time - currentTime < duration)
		{
			float lerp = (Time.time - currentTime) / duration;
			_messageBackground.color = Color.Lerp(initialColor, targetColor, lerp);
			yield return null;
			if (_messageBackground == null)
				yield break;
		}

		_messageBackground.color = targetColor;
	}

	public void RemoveMessage() => IRCConnection.Instance.ScrollOutStartTime[this] = Time.time;
}
