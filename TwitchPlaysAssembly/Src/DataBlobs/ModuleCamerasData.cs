using UnityEngine;
using UnityEngine.UI;

public class ModuleCamerasData : MonoBehaviour
{
	public Text headerPrefab = null;
	public Text timerPrefab = null;
	public Text timerShadowPrefab = null;
	public Text strikesPrefab = null;
	public Text solvesPrefab = null;
	public Text confidencePrefab = null;
	public Camera cameraPrefab = null;
	public RectTransform bombStatus = null;
	public bool cameraWallEnabled = false;
	public Text[] notesTexts = null;
}
