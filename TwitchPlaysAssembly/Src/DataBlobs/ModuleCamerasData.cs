using UnityEngine;
using UnityEngine.UI;

public class ModuleCamerasData : MonoBehaviour
{
	public Text headerPrefab = null;
	public Text timerPrefab = null;
	public Text timerShadowPrefab = null;
	public Text strikesPrefab = null;
	public Text strikeLimitPrefab = null;
	public Text solvesPrefab = null;
	public Text totalModulesPrefab = null;
	public Text confidencePrefab = null;
	public Camera cameraPrefab = null;
	public RectTransform bombStatus = null;
	public int firstBackupCamera = 3;
	public Text[] notesTexts = null;
}
