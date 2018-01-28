using UnityEngine;
using UnityEngine.UI;

public class TwitchComponentHandleData : MonoBehaviour
{
	public TwitchMessage messagePrefab = null;
	public Image unsupportedPrefab = null;
	public Image idBannerPrefab = null;

	public CanvasGroup canvasGroup = null;
	public CanvasGroup highlightGroup = null;
	public CanvasGroup canvasGroupMultiDecker = null;
	public CanvasGroup canvasGroupUnsupported = null;
	public Text headerText = null;
	public Text idText = null;
	public Text idTextMultiDecker = null;
	public Text idTextUnsupported = null;
	public Image claimedUser = null;
	public Image claimedUserMultiDecker = null;
	public ScrollRect messageScroll = null;
	public GameObject messageScrollContents = null;

	public Image upArrow = null;
	public Image downArrow = null;
	public Image leftArrow = null;
	public Image rightArrow = null;

	public Image upArrowHighlight = null;
	public Image downArrowHighlight = null;
	public Image leftArrowHighlight = null;
	public Image rightArrowHighlight = null;

	public Color claimedBackgroundColour = new Color(255, 0, 0);
	public Color solvedBackgroundColor = new Color(0, 128, 0);
	public Color markedBackgroundColor = new Color(0, 0, 0);

	public AudioSource takeModuleSound = null;
}
