using UnityEngine;

class IRCConnectionData : MonoBehaviour
{
#pragma warning disable CS0649
	public TwitchMessage MessagePrefab;

	public CanvasGroup HighlightGroup;

	public GameObject MessageScrollContents;
	public RectTransform MainWindowTransform;
	public RectTransform HighlightTransform;

	public GameObject ConnectionAlert;
#pragma warning disable CS0649
}
