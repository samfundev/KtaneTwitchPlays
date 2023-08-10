using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Input;
using Assets.Scripts.Records;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Encapsulates a bomb (of which there may be multiple in a game).</summary>
public class TwitchBomb : MonoBehaviour
{
	#region Public Fields
	public CanvasGroup CanvasGroup;
	public Image EdgeworkID;
	public Text EdgeworkIDText;
	public Text EdgeworkText;
	public RectTransform EdgeworkWindowTransform;
	public RectTransform EdgeworkHighlightTransform;

	[HideInInspector]
	private Bomb _bomb;
	public Bomb Bomb {
		get => _bomb;
		set {
			_bomb = value;

			var floatingHoldable = Bomb.GetComponent<FloatingHoldable>();
			floatingHoldable.OnHold += () => TwitchGame.ModuleCameras?.ChangeBomb(this);
			floatingHoldable.OnLetGo += () => TwitchGame.ModuleCameras?.ChangeBomb(null);
		}
	}

	[HideInInspector]
	public int BombID = -1;

	[HideInInspector]
	public DateTime BombTimeStamp;

	[HideInInspector]
	public object BackdoorComponent;

	[HideInInspector]
	public bool BackdoorHandleHack;

	public string Code;
	#endregion

	#region Private fields & properties
	private string _edgeworkCode;

	private string _bombName;

	private bool _flipEnabled = true;

	public string BombName
	{
		get => _bombName;
		set
		{
			_bombName = value;
			if (TwitchGame.ModuleCameras != null) TwitchGame.ModuleCameras.UpdateHeader();
		}
	}

	private static bool HeldFrontFace => KTInputManager.Instance.SelectableManager.GetActiveFace() == FaceEnum.Front;
	#endregion

	#region Unity Lifecycle
	private void Awake()
	{
		Code = "bomb";
		_edgeworkCode = "edgework";

		CanvasGroup = transform.Find("UI").GetComponent<CanvasGroup>();
		EdgeworkWindowTransform = CanvasGroup.transform.Find("EdgeworkWindow").GetComponent<RectTransform>();
		EdgeworkHighlightTransform = CanvasGroup.transform.Find("EdgeworkHighlight").GetComponent<RectTransform>();
		EdgeworkID = EdgeworkWindowTransform.Find("ID").GetComponent<Image>();
		EdgeworkIDText = EdgeworkWindowTransform.Find("ID").Find("IDText").GetComponent<Text>();
		EdgeworkText = EdgeworkWindowTransform.Find("Header").Find("HeaderText").GetComponent<Text>();

		if (TwitchPlaySettings.data.EnableEdgeworkCameras)
		{
			EdgeworkWindowTransform.gameObject.SetActive(false);
			EdgeworkHighlightTransform.gameObject.SetActive(false);
			EdgeworkID.gameObject.SetActive(false);
			EdgeworkIDText.gameObject.SetActive(false);
			EdgeworkText.gameObject.SetActive(false);
		}
	}

	private void Start()
	{
		if (BombID > -1)
		{
			Code = "bomb" + (BombID + 1);
			_edgeworkCode = "edgework" + (BombID + 1);
		}

		EdgeworkIDText.text = string.Format("!{0}", _edgeworkCode);
		EdgeworkText.text = TwitchPlaySettings.data.BlankBombEdgework;

		CanvasGroup.alpha = BombID == 0 ? 1 : 0;
	}

	private void Update()
	{
		if (BackdoorComponent == null) return;

		bool newState = BackdoorComponent.GetValue<bool>("BeingHacked");
		if (BackdoorHandleHack == newState) return;
		BackdoorHandleHack = newState;

		var moduleCameras = TwitchGame.ModuleCameras;
		if (moduleCameras == null) return;

		bool visible = !BackdoorHandleHack;
		moduleCameras.SetBombUIVisibility(visible);
		SetMainUIWindowVisibility(visible);
	}

	private void OnDestroy() => StopAllCoroutines();
	#endregion

	public IEnumerator HideMainUIWindow()
	{
		EdgeworkWindowTransform.localScale = Vector3.zero;
		EdgeworkHighlightTransform.localScale = Vector3.zero;
		IRCConnection.Instance.MainWindowTransform.localScale = Vector3.zero;
		IRCConnection.Instance.HighlightTransform.localScale = Vector3.zero;
		yield return null;
	}

	public IEnumerator ShowMainUIWindow()
	{
		EdgeworkWindowTransform.localScale = Vector3.one;
		EdgeworkHighlightTransform.localScale = Vector3.one;
		IRCConnection.Instance.MainWindowTransform.localScale = Vector3.one;
		IRCConnection.Instance.HighlightTransform.localScale = Vector3.one;
		yield return null;
	}

	public void SetMainUIWindowVisibility(bool visible)
	{
		Vector3 scale = visible ? Vector3.one : Vector3.zero;

		EdgeworkWindowTransform.localScale = scale;
		EdgeworkHighlightTransform.localScale = scale;
		IRCConnection.Instance.MainWindowTransform.localScale = scale;
		IRCConnection.Instance.HighlightTransform.localScale = scale;
	}

	public void CauseExplosionByVote() => StartCoroutine(DelayBombExplosionCoroutine(TwitchPlaySettings.data.BombDetonateCommand, "Voted detonation", 1.0f));

	public void CauseExplosionByModuleCommand(string message, string reason) => StartCoroutine(DelayBombExplosionCoroutine(message, reason, 0.1f));

	public void CauseExplosionByTrainingModeTimeout() => StartCoroutine(DelayBombExplosionCoroutine(TwitchPlaySettings.data.BombDetonateCommand, "Training Mode Timeout", 1.0f));

	public float CurrentTimer
	{
		get => Bomb.GetTimer().TimeRemaining;
		set => Bomb.GetTimer().TimeRemaining = (value < 0) ? 0 : value;
	}

	public void CauseVersusExplosion() => StartCoroutine(DelayBombExplosionCoroutine(null, "Evil defeated Good", 0.1f));

	#region Private Methods
	public IEnumerator DelayBombExplosionCoroutine() => DelayBombExplosionCoroutine(TwitchPlaySettings.data.BombDetonateCommand, "Detonate Command", 1.0f);

	private IEnumerator DelayBombExplosionCoroutine(string message, string reason, float delay)
	{
		StrikeCount = StrikeLimit - 1;
		if (!string.IsNullOrEmpty(message))
			IRCConnection.SendMessage(message);
		yield return new WaitForSeconds(delay);
		CauseStrikesToExplosion(reason);
	}

	public IEnumerator HoldBomb(bool? frontFace = null)
	{
		SelectableArea area = Bomb.GetComponentInChildren<SelectableArea>();
		if (area.gameObject.layer != 11) yield break;

		var holdable = Bomb.GetComponent<FloatingHoldable>();

		var gameRoomHoldBomb = GameRoom.Instance?.BombCommanderHoldBomb(Bomb, frontFace);
		if (gameRoomHoldBomb != null && gameRoomHoldBomb.MoveNext() && gameRoomHoldBomb.Current is bool continueInvoke)
		{
			do
				yield return gameRoomHoldBomb.Current;
			while (gameRoomHoldBomb.MoveNext());
			if (!continueInvoke || holdable == null)
				yield break;
		}

		var holdState = holdable.HoldState;
		bool doForceRotate = false;

		if (holdState != FloatingHoldable.HoldStateEnum.Held)
		{
			Bomb.GetComponent<Selectable>().Trigger();
			doForceRotate = true;
		}
		else if (frontFace != HeldFrontFace)
		{
			doForceRotate = true;
		}

		if (!doForceRotate)
			yield break;
		float holdTime = holdable.PickupTime;
		var forceRotationCoroutine = ForceHeldRotation(frontFace, holdTime);
		while (forceRotationCoroutine.MoveNext())
			yield return forceRotationCoroutine.Current;
	}

	public IEnumerator LetGoBomb()
	{
		var holdable = Bomb.GetComponent<FloatingHoldable>();

		IEnumerator gameRoomDropBomb = GameRoom.Instance?.BombCommanderDropBomb(Bomb);
		if (gameRoomDropBomb != null && gameRoomDropBomb.MoveNext() && gameRoomDropBomb.Current is bool continueInvoke)
		{
			do
				yield return gameRoomDropBomb.Current;
			while (gameRoomDropBomb.MoveNext());
			if (!continueInvoke || holdable == null)
				yield break;
		}

		while (holdable.HoldState == FloatingHoldable.HoldStateEnum.Held)
		{
			KTInputManager.Instance.SelectableManager.HandleCancel();
			yield return new WaitForSeconds(0.1f);
		}
	}

	public IEnumerator ShowEdgework(string edge)
	{
		const string allEdges = "all edges";
		IEnumerator gameRoomShowEdgework = GameRoom.Instance?.BombCommanderBombEdgework(Bomb, edge);
		if (gameRoomShowEdgework != null && gameRoomShowEdgework.MoveNext() && gameRoomShowEdgework.Current is bool continueInvoke)
		{
			do
				yield return gameRoomShowEdgework.Current;
			while (gameRoomShowEdgework.MoveNext());
			if (!continueInvoke)
				yield break;
		}

		TwitchGame.ModuleCameras?.Hide();

		edge = edge.ToLowerInvariant().Trim();
		if (string.IsNullOrEmpty(edge))
			edge = allEdges;

		IEnumerator holdCoroutine = HoldBomb(HeldFrontFace);
		while (holdCoroutine.MoveNext())
		{
			yield return holdCoroutine.Current;
		}

		float offset = edge.EqualsAny("45", "-45") ? 0.0f : 45.0f;

		if (edge.EqualsAny(allEdges, "right", "r", "45", "-45"))
		{
			IEnumerator firstEdge = DoFreeYRotate(0.0f, 0.0f, 90.0f, 90.0f, 0.3f);
			while (firstEdge.MoveNext())
			{
				yield return firstEdge.Current;
			}
			yield return new WaitForSeconds(2.0f);
		}

		if (edge.EqualsAny("bottom right", "right bottom", "br", "rb", "45", "-45"))
		{
			IEnumerator firstSecondEdge = edge.EqualsAny(allEdges, "45", "-45")
				? DoFreeYRotate(90.0f, 90.0f, 45.0f, 90.0f, 0.3f)
				: DoFreeYRotate(0.0f, 0.0f, 45.0f, 90.0f, 0.3f);
			while (firstSecondEdge.MoveNext())
			{
				yield return firstSecondEdge.Current;
			}
			yield return new WaitForSeconds(1f);
		}

		if (edge.EqualsAny(allEdges, "bottom", "b", "45", "-45"))
		{
			IEnumerator secondEdge = edge.EqualsAny(allEdges, "45", "-45")
				? DoFreeYRotate(45.0f + offset, 90.0f, 0.0f, 90.0f, 0.3f)
				: DoFreeYRotate(0.0f, 0.0f, 0.0f, 90.0f, 0.3f);
			while (secondEdge.MoveNext())
			{
				yield return secondEdge.Current;
			}
			yield return new WaitForSeconds(2.0f);
		}

		if (edge.EqualsAny("left bottom", "bottom left", "lb", "bl", "45", "-45"))
		{
			IEnumerator secondThirdEdge = edge.EqualsAny(allEdges, "45", "-45")
				? DoFreeYRotate(0.0f, 90.0f, -45.0f, 90.0f, 0.3f)
				: DoFreeYRotate(0.0f, 0.0f, -45.0f, 90.0f, 0.3f);
			while (secondThirdEdge.MoveNext())
			{
				yield return secondThirdEdge.Current;
			}
			yield return new WaitForSeconds(1f);
		}

		if (edge.EqualsAny(allEdges, "left", "l", "45", "-45"))
		{
			IEnumerator thirdEdge = edge.EqualsAny(allEdges, "45", "-45")
				? DoFreeYRotate(-45.0f + offset, 90.0f, -90.0f, 90.0f, 0.3f)
				: DoFreeYRotate(0.0f, 0.0f, -90.0f, 90.0f, 0.3f);
			while (thirdEdge.MoveNext())
			{
				yield return thirdEdge.Current;
			}
			yield return new WaitForSeconds(2.0f);
		}

		if (edge.EqualsAny("top left", "left top", "tl", "lt", "45", "-45"))
		{
			IEnumerator thirdFourthEdge = edge.EqualsAny(allEdges, "45", "-45")
				? DoFreeYRotate(-90.0f, 90.0f, -135.0f, 90.0f, 0.3f)
				: DoFreeYRotate(0.0f, 0.0f, -135.0f, 90.0f, 0.3f);
			while (thirdFourthEdge.MoveNext())
			{
				yield return thirdFourthEdge.Current;
			}
			yield return new WaitForSeconds(1f);
		}

		if (edge.EqualsAny(allEdges, "top", "t", "45", "-45"))
		{
			IEnumerator fourthEdge = edge.EqualsAny(allEdges, "45", "-45")
				? DoFreeYRotate(-135.0f + offset, 90.0f, -180.0f, 90.0f, 0.3f)
				: DoFreeYRotate(0.0f, 0.0f, -180.0f, 90.0f, 0.3f);
			while (fourthEdge.MoveNext())
			{
				yield return fourthEdge.Current;
			}
			yield return new WaitForSeconds(2.0f);
		}

		if (edge.EqualsAny("top right", "right top", "tr", "rt", "45", "-45"))
		{
			IEnumerator fourthFirstEdge = edge.EqualsAny(allEdges, "45", "-45")
				? DoFreeYRotate(-180.0f, 90.0f, -225.0f, 90.0f, 0.3f)
				: DoFreeYRotate(0.0f, 0.0f, -225.0f, 90.0f, 0.3f);
			while (fourthFirstEdge.MoveNext())
			{
				yield return fourthFirstEdge.Current;
			}
			yield return new WaitForSeconds(1f);
		}

		IEnumerator returnToFace;
		switch (edge)
		{
			case "right":
			case "r":
				returnToFace = DoFreeYRotate(90.0f, 90.0f, 0.0f, 0.0f, 0.3f);
				break;
			case "right bottom":
			case "bottom right":
			case "br":
			case "rb":
				returnToFace = DoFreeYRotate(45.0f, 90.0f, 0.0f, 0.0f, 0.3f);
				break;
			case "bottom":
			case "b":
				returnToFace = DoFreeYRotate(0.0f, 90.0f, 0.0f, 0.0f, 0.3f);
				break;
			case "left bottom":
			case "bottom left":
			case "lb":
			case "bl":
				returnToFace = DoFreeYRotate(-45.0f, 90.0f, 0.0f, 0.0f, 0.3f);
				break;
			case "left":
			case "l":
				returnToFace = DoFreeYRotate(-90.0f, 90.0f, 0.0f, 0.0f, 0.3f);
				break;
			case "left top":
			case "top left":
			case "lt":
			case "tl":
				returnToFace = DoFreeYRotate(-135.0f, 90.0f, 0.0f, 0.0f, 0.3f);
				break;
			case "top":
			case "t":
				returnToFace = DoFreeYRotate(-180.0f, 90.0f, 0.0f, 0.0f, 0.3f);
				break;
			default:
				returnToFace = DoFreeYRotate(-225.0f + offset, 90.0f, 0.0f, 0.0f, 0.3f);
				break;
		}
		while (returnToFace.MoveNext())
		{
			yield return returnToFace.Current;
		}

		TwitchGame.ModuleCameras?.Show();
	}

	const string WidgetQueryTwofactor = "twofactor";
	const string WidgetQueryManufacture = "manufacture";
	const string WidgetQueryDay = "day";
	const string WidgetQueryRandomTime = "time";
	const string WidgetQueryVoltage = "volt";

	public IEnumerable<Dictionary<string, T>> QueryWidgets<T>(string queryKey, string queryInfo = null) => MysteryWidgetShim.FilterQuery(queryKey, Bomb.WidgetManager.GetWidgetQueryResponses(queryKey, queryInfo).Select(JsonConvert.DeserializeObject<Dictionary<string, T>>));

	public string FillEdgework()
	{
		var edgework = new List<string>();
		var portNames = new Dictionary<string, string>
		{
			{ "RJ45", "RJ" },
			{ "StereoRCA", "RCA" },
			{ "ComponentVideo", "Component" },
			{ "CompositeVideo", "Composite" }
		};

		// Mystery Widget
		MysteryWidgetShim.ClearUnused();

		foreach (GameObject cover in MysteryWidgetShim.Covers.Where(x => x != null))
		{
			edgework.Add("Hidden");
		}

		Dictionary<string, string> ruleSeed = QueryWidgets<string>("RuleSeedModifier").FirstOrDefault();
		if (ruleSeed?.ContainsKey("seed") ?? false)
			edgework.Add($"RuleSeed:{ruleSeed["seed"]}");

		List<Dictionary<string, int>> batteries = QueryWidgets<int>(KMBombInfo.QUERYKEY_GET_BATTERIES).ToList();
		edgework.Add(batteries.All(x => new[] { 1, 2 }.Contains(x["numbatteries"]))
			? $"{batteries.Sum(x => x["numbatteries"])}B {batteries.Count}H"
			: batteries.OrderBy(x => x["numbatteries"]).Select(x => x["numbatteries"]).Distinct()
				.Select(holder => batteries.Count(x => x["numbatteries"] == holder) + "x[" + (holder == 0 ? "Empty" : holder.ToString()) + "]").Join());

		List<Dictionary<string, string>> indicators = QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_INDICATOR).OrderBy(x => x["label"]).ToList();
		List<Dictionary<string, string>> colorIndicators = QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_INDICATOR + "Color").OrderBy(x => x["label"]).ToList();

		foreach (Dictionary<string, string> indicator in colorIndicators)
		{
			foreach (Dictionary<string, string> vanillaIndicator in indicators)
			{
				if (vanillaIndicator["label"] != indicator["label"]) continue;
				if (vanillaIndicator["on"] != (indicator["color"] == "Black" ? "False" : "True")) continue;
				if (vanillaIndicator.ContainsKey("color")) continue;
				vanillaIndicator["on"] = $"({indicator["color"]})";
				break;
			}
		}

		foreach (Dictionary<string, string> indicator in indicators)
		{
			indicator["on"] = indicator["on"] == "True" ? "*" : "";
			if (indicator.ContainsKey("display"))
			{
				indicator["label"] = indicator.ContainsKey("color") ? indicator["display"] + "(" + indicator["color"] + ")" : indicator["display"];
			}
		}
		edgework.Add(indicators.OrderBy(x => x["label"]).ThenBy(x => x["on"]).Select(x => x["on"] + x["label"]).Join());
		edgework.Add(QueryWidgets<List<string>>(KMBombInfo.QUERYKEY_GET_PORTS).Select(x => x["presentPorts"].Select(port => portNames.ContainsKey(port) ? portNames[port] : port).OrderBy(y => y).Join(", ")).Select(x => x?.Length == 0 ? "Empty" : x).Select(x => "[" + x + "]").Join());
		edgework.Add(QueryWidgets<int>(WidgetQueryTwofactor).Select(x => x["twofactor_key"].ToString()).Join(", "));
		Dictionary<string, string> voltageMeter = QueryWidgets<string>("volt").FirstOrDefault();
		if (voltageMeter?.ContainsKey("voltage") ?? false)
			edgework.Add($"{voltageMeter["voltage"]}V");
		edgework.Add(QueryWidgets<string>(WidgetQueryManufacture).Select(x => x["month"] + " - " + x["year"]).Join());
		edgework.Add(QueryWidgets<string>(WidgetQueryDay).Select(x =>
		{
			var enabled = x["colorenabled"] == "True";
			var monthChar = enabled ? "(O)" : "(M)";
			var dateChar = enabled ? "(C)" : "(D)";
			return string.Format("{0}({1}) {2}", x["day"], x["daycolor"],
			int.Parse(x["monthcolor"]).Equals(0) ? (x["month"] + monthChar + "-" + x["date"] + dateChar) : (x["date"] + dateChar + "-" + x["month"] + monthChar));
		}).Join());
		edgework.Add(QueryWidgets<string>(WidgetQueryRandomTime).Select(x =>
		{
			var str1 = x["time"].Substring(0, 2);
			var str2 = x["time"].Substring(2, 2);
			var str3 = x["am"] == "True" ? "am" : x["pm"] == "True" ? "pm" : "";
			if ((str3 == "am" || str3 == "pm") && int.Parse(str1) < 10) str1 = str1.Substring(1, 1);
			return str1 + ":" + str2 + str3;
		}).Join(", "));

		edgework.Add(QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER).First()["serial"]);

		string edgeworkString = edgework.Where(str => !string.IsNullOrEmpty(str)).Join(" // ");
		if (EdgeworkText.text != edgeworkString)
			EdgeworkText.text = edgeworkString;
		return edgeworkString;
	}

	public IEnumerator Focus(Selectable selectable, float focusDistance, bool frontFace, bool select = true)
	{
		IEnumerator gameRoomFocus = GameRoom.Instance?.BombCommanderFocus(Bomb, selectable, focusDistance, frontFace);
		if (gameRoomFocus != null && gameRoomFocus.MoveNext() && gameRoomFocus.Current is bool continueInvoke)
		{
			do
				yield return gameRoomFocus.Current;
			while (gameRoomFocus.MoveNext());
			if (!continueInvoke)
				yield break;
		}

		IEnumerator holdCoroutine = HoldBomb(frontFace);
		while (holdCoroutine.MoveNext())
			yield return holdCoroutine.Current;

		var holdable = Bomb.GetComponent<FloatingHoldable>();
		float focusTime = holdable.FocusTime;
		holdable.Focus(selectable.transform, focusDistance, false, false, focusTime);

		if (select) selectable.HandleSelect(false);
		selectable.HandleInteract();
	}

	public IEnumerator Defocus(Selectable selectable, bool frontFace, bool deselect = true)
	{
		IEnumerator gameRoomDefocus = GameRoom.Instance?.BombCommanderDefocus(Bomb, selectable, frontFace);
		if (gameRoomDefocus != null && gameRoomDefocus.MoveNext() && gameRoomDefocus.Current is bool continueInvoke)
		{
			do
				yield return gameRoomDefocus.Current;
			while (gameRoomDefocus.MoveNext());
			if (!continueInvoke)
				yield break;
		}

		selectable.OnDefocus?.Invoke();

		Bomb.GetComponent<FloatingHoldable>().Defocus(false, false);
		if (deselect) selectable.HandleDeselect();
		selectable.HandleCancel();
	}

	public void RotateByLocalQuaternion(Quaternion localQuaternion)
	{
		if (!GameRoom.Instance.BombCommanderRotateByLocalQuaternion(Bomb, localQuaternion))
			return;

		Transform baseTransform = KTInputManager.Instance.SelectableManager.GetBaseHeldObjectTransform();

		float currentZSpin = HeldFrontFace ? 0.0f : 180.0f;

		KTInputManager.Instance.SelectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, currentZSpin) * localQuaternion);
		KTInputManager.Instance.SelectableManager.HandleFaceSelection();
	}

	public static void RotateCameraByLocalQuaternion(GameObject gameObj, Quaternion localQuaternion)
	{
		Transform twitchPlaysCameraTransform = gameObj?.transform.Find("TwitchPlayModuleCamera");
		Camera cam = twitchPlaysCameraTransform?.GetComponentInChildren<Camera>();
		if (cam == null) return;

		int originalLayer = -1;
		for (int i = 0; i < 32 && originalLayer < 0; i++)
		{
			if ((cam.cullingMask & (1 << i)) != 1 << i) continue;
			originalLayer = i;
		}

		int layer = localQuaternion == Quaternion.identity ? originalLayer : 31;

		foreach (Transform trans in gameObj.GetComponentsInChildren<Transform>(true))
			trans.gameObject.layer = layer;

		twitchPlaysCameraTransform.localRotation = Quaternion.Euler(HeldFrontFace ? -localQuaternion.eulerAngles : localQuaternion.eulerAngles);
	}

	public void CauseStrikesToExplosion(string reason)
	{
		for (int strikesToMake = StrikeLimit - StrikeCount; strikesToMake > 0; --strikesToMake)
		{
			CauseStrike(reason);
		}
	}

	private void CauseStrike(string reason)
	{
		StrikeSource strikeSource = new StrikeSource
		{
			ComponentType = Assets.Scripts.Missions.ComponentTypeEnum.Mod,
			InteractionType = InteractionTypeEnum.Other,
			Time = CurrentTimerElapsed,
			ComponentName = reason
		};

		RecordManager recordManager = RecordManager.Instance;
		recordManager.RecordStrike(strikeSource);

		Bomb.OnStrike(null);
	}

	private static IEnumerator ForceHeldRotation(bool? frontFace, float duration)
	{
		var sm = KTInputManager.Instance.SelectableManager;
		var baseTransform = sm.GetBaseHeldObjectTransform();

		float oldZSpin = sm.GetZSpin();
		float targetZSpin = frontFace != null ? ((bool) frontFace ? 0.0f : 180.0f) : oldZSpin;

		float initialTime = Time.time;
		while (Time.time - initialTime < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			float currentZSpin = Mathf.SmoothStep(oldZSpin, targetZSpin, lerp);

			Vector3 heldObjectTiltEulerAngles = sm.GetHeldObjectTiltEulerAngles();
			heldObjectTiltEulerAngles.x = Mathf.Clamp(heldObjectTiltEulerAngles.x, -95f, 95f);
			heldObjectTiltEulerAngles.z -= sm.GetZSpin() - currentZSpin;

			sm.SetZSpin(currentZSpin);
			sm.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(heldObjectTiltEulerAngles));
			sm.SetHeldObjectTiltEulerAngles(heldObjectTiltEulerAngles);
			sm.HandleFaceSelection();
			yield return null;
		}

		Vector3 heldObjectTileEulerAnglesFinal = sm.GetHeldObjectTiltEulerAngles();
		heldObjectTileEulerAnglesFinal.x = Mathf.Clamp(heldObjectTileEulerAnglesFinal.x, -95f, 95f);
		heldObjectTileEulerAnglesFinal.z -= sm.GetZSpin() - targetZSpin;

		sm.SetZSpin(targetZSpin);
		sm.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(heldObjectTileEulerAnglesFinal));
		sm.SetHeldObjectTiltEulerAngles(heldObjectTileEulerAnglesFinal);
		sm.HandleFaceSelection();
	}

	private IEnumerator DoFreeYRotate(float initialYSpin, float initialPitch, float targetYSpin, float targetPitch, float duration)
	{
		if (Bomb.GetComponent<FloatingHoldable>() == null)
			yield break;

		if (!HeldFrontFace)
		{
			initialPitch *= -1;
			initialYSpin *= -1;
			targetPitch *= -1;
			targetYSpin *= -1;
		}

		float initialTime = Time.time;
		while (Time.time - initialTime < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			float currentYSpin = Mathf.SmoothStep(initialYSpin, targetYSpin, lerp);
			float currentPitch = Mathf.SmoothStep(initialPitch, targetPitch, lerp);

			Quaternion currentRotation = Quaternion.Euler(currentPitch, 0, 0) * Quaternion.Euler(0, currentYSpin, 0);
			RotateByLocalQuaternion(currentRotation);
			yield return null;
		}
		Quaternion target = Quaternion.Euler(targetPitch, 0, 0) * Quaternion.Euler(0, targetYSpin, 0);
		RotateByLocalQuaternion(target);
	}

	private void HandleStrikeChanges()
	{
		int strikeLimit = StrikeLimit;
		int strikeCount = Math.Min(StrikeCount, StrikeLimit);

		GameRecord GameRecord = RecordManager.Instance.GetCurrentRecord();
		StrikeSource[] strikes = GameRecord.Strikes;
		if (strikes.Length != strikeLimit)
		{
			StrikeSource[] newStrikes = new StrikeSource[Math.Max(strikeLimit, 1)];
			Array.Copy(strikes, newStrikes, Math.Min(strikes.Length, newStrikes.Length));
			GameRecord.Strikes = newStrikes;
		}

		if (strikeCount == strikeLimit)
		{
			if (strikeLimit < 1)
			{
				Bomb.NumStrikesToLose = 1;
				strikeLimit = 1;
			}
			Bomb.NumStrikes = strikeLimit - 1;
			CommonReflectedTypeInfo.GameRecordCurrentStrikeIndexField.SetValue(GameRecord, strikeLimit - 1);
			CauseStrike("Strike count / limit changed.");
		}
		else
		{
			Debug.Log($"[Bomb] Strike from TwitchPlays! {StrikeCount} / {StrikeLimit} strikes");
			CommonReflectedTypeInfo.GameRecordCurrentStrikeIndexField.SetValue(GameRecord, strikeCount);
			float[] rates = { 1, 1.25f, 1.5f, 1.75f, 2 };
			Bomb.GetTimer().SetRateModifier(rates[Math.Min(strikeCount, 4)]);
			Bomb.StrikeIndicator.StrikeCount = strikeCount;
		}
	}

	public IEnumerator TurnBomb()
	{
		if (TwitchPlaySettings.data.BombFlipCooldown > float.Epsilon && !_flipEnabled)
			yield break;

		var gameRoomTurnBomb = GameRoom.Instance?.BombCommanderTurnBomb(Bomb);
		if (gameRoomTurnBomb != null && gameRoomTurnBomb.MoveNext() && gameRoomTurnBomb.Current is bool continueInvoke)
		{
			do
				yield return gameRoomTurnBomb.Current;
			while (gameRoomTurnBomb.MoveNext());
			if (!continueInvoke)
				yield break;
		}

		var holdBombCoroutine = HoldBomb(KTInputManager.Instance.SelectableManager.GetActiveFace() != FaceEnum.Front);
		while (holdBombCoroutine.MoveNext())
			yield return holdBombCoroutine.Current;

		if (TwitchPlaySettings.data.BombFlipCooldown > float.Epsilon)
		{
			_flipEnabled = false;
			StartCoroutine(new WaitForSeconds(TwitchPlaySettings.data.BombFlipCooldown).Yield(() => _flipEnabled = true)); //don't wait on this coroutine to finish the current one
		}
	}

	public IEnumerator KeepAlive()
	{
		while (!IsSolved)
		{
			if (CurrentTimer <= 60)
				CurrentTimer = BombStartingTimer;

			yield return new WaitForSeconds(0.1f);
		}
	}

	public bool IsSolved => Bomb.IsSolved();

	private float CurrentTimerElapsed => Bomb.GetTimer().TimeElapsed;

	public string CurrentTimerFormatted => Bomb.GetTimer().GetFormattedTime(CurrentTimer, true);

	public string GetFullFormattedTime => Math.Max(CurrentTimer, 0).FormatTime();

	public string GetFullStartingTime => Math.Max(BombStartingTimer, 0).FormatTime();

	public int StrikeCount
	{
		get => Bomb.NumStrikes;
		set
		{
			// Simon Says is unsolvable with less than zero strikes.
			if (value < 0)
				value = 0;
			Bomb.NumStrikes = value;
			HandleStrikeChanges();
		}
	}

	public int StrikeLimit
	{
		get => Bomb.NumStrikesToLose;
		set { Bomb.NumStrikesToLose = value; HandleStrikeChanges(); }
	}

	public int BombSolvableModules => Bomb.GetSolvableComponentCount();
	public int BombSolvedModules => Bomb.GetSolvedComponentCount();
	public List<string> BombSolvableModuleIDs => Bomb.BombComponents.Where(x => x.IsSolvable).Select(x => x.GetModuleID()).ToList();
	public List<string> BombSolvedModuleIDs => Bomb.BombComponents.Where(x => x.IsSolvable && x.IsSolved).Select(x => x.GetModuleID()).ToList();

	public float BombStartingTimer;
	#endregion
}
