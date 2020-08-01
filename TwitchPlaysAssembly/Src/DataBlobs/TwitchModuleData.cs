using UnityEngine;
using UnityEngine.UI;

public class TwitchModuleData : MonoBehaviour
{
	public TwitchMessage messagePrefab = null;

	public CanvasGroup canvasGroupMultiDecker = null;
	public CanvasGroup canvasGroupUnsupported = null;
	public Text idTextMultiDecker = null;
	public Text idTextUnsupported = null;
	public Image claimedUserMultiDecker = null;

	public Color claimedBackgroundColour = new Color(255, 0, 0);
	public Color solvedBackgroundColor = new Color(0, 128, 0);
	public Color markedBackgroundColor = new Color(0, 0, 0);

	public AudioSource takeModuleSound = null;

	public Image bar = null;
}
