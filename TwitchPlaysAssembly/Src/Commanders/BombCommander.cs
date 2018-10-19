using Assets.Scripts.Input;
using Assets.Scripts.Records;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class BombCommander
{
	#region Constructors
	public BombCommander(Bomb bomb)
	{
		ReuseBombCommander(bomb);
	}
	#endregion

	#region Interface Implementation
	public void ReuseBombCommander(Bomb bomb)
	{
		Bomb = bomb;
		timerComponent = Bomb.GetTimer();
		widgetManager = Bomb.WidgetManager;
		Selectable = Bomb.GetComponent<Selectable>();
		FloatingHoldable = Bomb.GetComponent<FloatingHoldable>();
		_selectableManager = KTInputManager.Instance.SelectableManager;
		BombTimeStamp = DateTime.Now;
		bombStartingTimer = CurrentTimer;
		bombSolvableModules = 0;
		bombSolvedModules = 0;
		SolvedModules = new Dictionary<string, List<TwitchModule>>();
	}

	public IEnumerator RespondToCommand(Message messageObj)
	{
		string message = messageObj.Text.ToLowerInvariant().Trim();

		if (message.EqualsAny("hold", "pick up"))
		{
			IEnumerator holdCoroutine = HoldBomb(_heldFrontFace);
			while (holdCoroutine.MoveNext())
			{
				yield return holdCoroutine.Current;
			}
		}
		else if (message.EqualsAny("turn", "turn round", "turn around", "rotate", "flip", "spin"))
		{
			IEnumerator turnCoroutine = TurnBomb();
			while (turnCoroutine.MoveNext())
			{
				yield return turnCoroutine.Current;
			}
		}
		else if (message.EqualsAny("drop", "let go", "put down"))
		{
			IEnumerator letGoCoroutine = LetGoBomb();
			while (letGoCoroutine.MoveNext())
			{
				yield return letGoCoroutine.Current;
			}
		}
		else if (message.RegexMatch(out Match edgeworkMatch, GameRoom.Instance.ValidEdgeworkRegex))
		{
			if (!TwitchPlaySettings.data.EnableEdgeworkCommand && !TwitchPlaySettings.data.AnarchyMode)
			{
				IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombEdgework, twitchBombHandle.edgeworkText.text), messageObj.UserNickName, !messageObj.IsWhisper);
			}
			else
			{
				IEnumerator edgeworkCoroutine = ShowEdgework(edgeworkMatch);
				while (edgeworkCoroutine.MoveNext())
				{
					yield return edgeworkCoroutine.Current;
				}
			}
		}
	}
	#endregion

	#region Helper Methods

	private IEnumerator HoldBomb(bool frontFace = true)
	{
		IEnumerator gameRoomHoldBomb = GameRoom.Instance?.BombCommanderHoldBomb(Bomb, frontFace);
		bool continueInvocation = true;
		if (gameRoomHoldBomb != null && gameRoomHoldBomb.MoveNext() && gameRoomHoldBomb.Current is bool continueInvoke)
		{
			continueInvocation = continueInvoke;
			do
			{
				yield return gameRoomHoldBomb.Current;
			} while (gameRoomHoldBomb.MoveNext());
		}

		if (!continueInvocation || FloatingHoldable == null) yield break;
		FloatingHoldable.HoldStateEnum holdState = FloatingHoldable.HoldState;
		bool doForceRotate = false;

		if (holdState != FloatingHoldable.HoldStateEnum.Held)
		{
			SelectObject(Selectable);
			doForceRotate = true;
			BombMessageResponder.moduleCameras?.ChangeBomb(this);
		}
		else if (frontFace != _heldFrontFace)
		{
			doForceRotate = true;
		}

		if (!doForceRotate) yield break;
		float holdTime = FloatingHoldable.PickupTime;
		IEnumerator forceRotationCoroutine = ForceHeldRotation(frontFace, holdTime);
		while (forceRotationCoroutine.MoveNext())
		{
			yield return forceRotationCoroutine.Current;
		}
	}

	public IEnumerator TurnBomb()
	{
		IEnumerator gameRoomTurnBomb = GameRoom.Instance?.BombCommanderTurnBomb(Bomb);
		bool continueInvocation = true;
		if (gameRoomTurnBomb != null && gameRoomTurnBomb.MoveNext() && gameRoomTurnBomb.Current is bool continueInvoke)
		{
			continueInvocation = continueInvoke;
			do
			{
				yield return gameRoomTurnBomb.Current;
			} while (gameRoomTurnBomb.MoveNext());
		}

		if (!continueInvocation) yield break;
		IEnumerator holdBombCoroutine = HoldBomb(!_heldFrontFace);
		while (holdBombCoroutine.MoveNext())
		{
			yield return holdBombCoroutine.Current;
		}
	}

	public IEnumerator LetGoBomb()
	{
		IEnumerator gameRoomDropBomb = GameRoom.Instance?.BombCommanderDropBomb(Bomb);
		bool continueInvocation = true;
		if (gameRoomDropBomb != null && gameRoomDropBomb.MoveNext() && gameRoomDropBomb.Current is bool continueInvoke)
		{
			continueInvocation = continueInvoke;
			do
			{
				yield return gameRoomDropBomb.Current;
			} while (gameRoomDropBomb.MoveNext());
		}

		if (!continueInvocation || FloatingHoldable == null) yield break;
		if (FloatingHoldable.HoldState != FloatingHoldable.HoldStateEnum.Held) yield break;

		IEnumerator turnBombCoroutine = HoldBomb();
		while (turnBombCoroutine.MoveNext())
		{
			yield return turnBombCoroutine.Current;
		}

		while (FloatingHoldable.HoldState == FloatingHoldable.HoldStateEnum.Held)
		{
			DeselectObject();
			yield return new WaitForSeconds(0.1f);
		}
	}

	private IEnumerator ShowEdgework(Match edgeworkMatch)
	{
		const string allEdges = "all edges";
		IEnumerator gameRoomShowEdgework = GameRoom.Instance?.BombCommanderBombEdgework(Bomb, edgeworkMatch);
		bool continueInvocation = true;
		if (gameRoomShowEdgework != null && gameRoomShowEdgework.MoveNext() && gameRoomShowEdgework.Current is bool continueInvoke)
		{
			continueInvocation = continueInvoke;
			do
			{
				yield return gameRoomShowEdgework.Current;
			} while (gameRoomShowEdgework.MoveNext());
		}

		if (!continueInvocation || FloatingHoldable == null || edgeworkMatch == null || !edgeworkMatch.Success) yield break;
		BombMessageResponder.moduleCameras?.Hide();

		string edge = edgeworkMatch.Groups[1].Value.ToLowerInvariant().Trim();
		if (string.IsNullOrEmpty(edge))
			edge = allEdges;

		IEnumerator holdCoroutine = HoldBomb(_heldFrontFace);
		while (holdCoroutine.MoveNext())
		{
			yield return holdCoroutine.Current;
		}
		IEnumerator returnToFace;
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

		BombMessageResponder.moduleCameras?.Show();
	}

	public IEnumerable<Dictionary<string, T>> QueryWidgets<T>(string queryKey, string queryInfo = null) => widgetManager.GetWidgetQueryResponses(queryKey, queryInfo).Select(Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, T>>);

	public void FillEdgework(bool silent = false)
	{
		List<string> edgework = new List<string>();
		Dictionary<string, string> portNames = new Dictionary<string, string>
		{
			{ "RJ45", "RJ" },
			{ "StereoRCA", "RCA" },
			{ "ComponentVideo", "Component" },
			{ "CompositeVideo", "Composite" }
		};

		var batteries = QueryWidgets<int>(KMBombInfo.QUERYKEY_GET_BATTERIES).ToList();
		edgework.Add(batteries.All(x => new[] { 1, 2 }.Contains(x["numbatteries"]))
			? $"{batteries.Sum(x => x["numbatteries"])}B {batteries.Count}H"
			: batteries.OrderBy(x => x["numbatteries"]).Select(x => x["numbatteries"]).Distinct()
				.Select(holder => batteries.Count(x => x["numbatteries"] == holder) + "x[" + (holder == 0 ? "Empty" : holder.ToString()) + "]").Join());

		var indicators = QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_INDICATOR).OrderBy(x => x["label"]).ToList();
		var colorindicators = QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_INDICATOR + "Color").OrderBy(x => x["label"]).ToList();

		foreach (var indicator in colorindicators)
		{
			foreach (var vanillaIndicator in indicators)
			{
				if (vanillaIndicator["label"] != indicator["label"]) continue;
				if (vanillaIndicator["on"] != (indicator["color"] == "Black" ? "False" : "True")) continue;
				if (vanillaIndicator.ContainsKey("color")) continue;
				vanillaIndicator["on"] = $"({indicator["color"]})";
				break;
			}
		}

		foreach (var indicator in indicators)
		{
			indicator["on"] = indicator["on"] == "True" ? "*" : "";

			if (indicator.ContainsKey("display"))
				indicator["label"] = indicator["display"] + "(" + indicator["color"] + ")";
		}

		edgework.Add(indicators.OrderBy(x => x["label"]).ThenBy(x => x["on"]).Select(x => x["on"] + x["label"]).Join());

		edgework.Add(QueryWidgets<List<string>>(KMBombInfo.QUERYKEY_GET_PORTS).Select(x => x["presentPorts"].Select(port => portNames.ContainsKey(port) ? portNames[port] : port).OrderBy(y => y).Join(", ")).Select(x => x == "" ? "Empty" : x).Select(x => "[" + x + "]").Join(" "));

		edgework.Add(QueryWidgets<int>(KMBombInfoExtensions.WidgetQueryTwofactor).Select(x => x["twofactor_key"].ToString()).Join(", "));

		edgework.Add(QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER).First()["serial"]);

		string edgeworkString = edgework.Where(str => str != "").Join(" // ");
		if (twitchBombHandle.edgeworkText.text == edgeworkString) return;

		twitchBombHandle.edgeworkText.text = edgeworkString;

		if (!silent)
			IRCConnection.SendMessage(TwitchPlaySettings.data.BombEdgework, edgeworkString);
	}

	public IEnumerator Focus(Selectable selectable, float focusDistance, bool frontFace)
	{
		IEnumerator gameRoomFocus = GameRoom.Instance?.BombCommanderFocus(Bomb, selectable, focusDistance, frontFace);
		bool continueInvocation = true;
		if (gameRoomFocus != null && gameRoomFocus.MoveNext() && gameRoomFocus.Current is bool continueInvoke)
		{
			continueInvocation = continueInvoke;
			do
			{
				yield return gameRoomFocus.Current;
			} while (gameRoomFocus.MoveNext());
		}

		if (!continueInvocation || FloatingHoldable == null) yield break;
		IEnumerator holdCoroutine = HoldBomb(frontFace);
		while (holdCoroutine.MoveNext())
		{
			yield return holdCoroutine.Current;
		}

		float focusTime = FloatingHoldable.FocusTime;
		FloatingHoldable.Focus(selectable.transform, focusDistance, false, false, focusTime);

		selectable.HandleSelect(false);
		selectable.HandleInteract();
	}

	public IEnumerator Defocus(Selectable selectable, bool frontFace)
	{
		IEnumerator gameRoomDefocus = GameRoom.Instance?.BombCommanderDefocus(Bomb, selectable, frontFace);
		bool continueInvocation = true;
		if (gameRoomDefocus != null && gameRoomDefocus.MoveNext() && gameRoomDefocus.Current is bool continueInvoke)
		{
			continueInvocation = continueInvoke;
			do
			{
				yield return gameRoomDefocus.Current;
			} while (gameRoomDefocus.MoveNext());
		}

		if (!continueInvocation || FloatingHoldable == null) yield break;

		FloatingHoldable.Defocus(false, false);
		selectable.HandleCancel();
		selectable.HandleDeselect();
	}

	public void RotateByLocalQuaternion(Quaternion localQuaternion)
	{
		if (!GameRoom.Instance.BombCommanderRotateByLocalQuaternion(Bomb, localQuaternion) || FloatingHoldable == null) return;
		Transform baseTransform = _selectableManager.GetBaseHeldObjectTransform();

		float currentZSpin = _heldFrontFace ? 0.0f : 180.0f;

		_selectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, currentZSpin) * localQuaternion);
		_selectableManager.HandleFaceSelection();
	}

	public void RotateCameraByLocalQuaternion(BombComponent bombComponent, Quaternion localQuaternion)
	{
		Transform twitchPlaysCameraTransform = bombComponent?.transform.Find("TwitchPlayModuleCamera");
		Camera cam = twitchPlaysCameraTransform?.GetComponentInChildren<Camera>();
		if (cam == null) return;

		int originalLayer = -1;
		for (int i = 0; i < 32 && originalLayer < 0; i++)
		{
			if ((cam.cullingMask & (1 << i)) != 1 << i) continue;
			originalLayer = i;
		}

		int layer = localQuaternion == Quaternion.identity ? originalLayer : 31;

		foreach (Transform trans in bombComponent.gameObject.GetComponentsInChildren<Transform>(true))
		{
			trans.gameObject.layer = layer;
		}

		twitchPlaysCameraTransform.localRotation = Quaternion.Euler(_heldFrontFace ? -localQuaternion.eulerAngles : localQuaternion.eulerAngles);
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

	private void SelectObject(Selectable selectable)
	{
		selectable.HandleSelect(true);
		_selectableManager.Select(selectable, true);
		_selectableManager.HandleInteract();
		selectable.OnInteractEnded();
	}

	private void DeselectObject() => _selectableManager.HandleCancel();

	private IEnumerator ForceHeldRotation(bool frontFace, float duration)
	{
		if (FloatingHoldable == null) yield break;
		Transform baseTransform = _selectableManager.GetBaseHeldObjectTransform();

		float oldZSpin = _selectableManager.GetZSpin();
		float targetZSpin = frontFace ? 0.0f : 180.0f;

		float initialTime = Time.time;
		while (Time.time - initialTime < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			float currentZSpin = Mathf.SmoothStep(oldZSpin, targetZSpin, lerp);

			Quaternion currentRotation = Quaternion.Euler(0.0f, 0.0f, currentZSpin);
			Vector3 HeldObjectTiltEulerAngles = _selectableManager.GetHeldObjectTiltEulerAngles();
			HeldObjectTiltEulerAngles.x = Mathf.Clamp(HeldObjectTiltEulerAngles.x, -95f, 95f);
			HeldObjectTiltEulerAngles.z -= _selectableManager.GetZSpin() - currentZSpin;

			_selectableManager.SetZSpin(currentZSpin);
			_selectableManager.SetControlsRotation(baseTransform.rotation * currentRotation);
			_selectableManager.SetHeldObjectTiltEulerAngles(HeldObjectTiltEulerAngles);
			_selectableManager.HandleFaceSelection();
			yield return null;
		}

		Vector3 HeldObjectTileEulerAnglesFinal = _selectableManager.GetHeldObjectTiltEulerAngles();
		HeldObjectTileEulerAnglesFinal.x = Mathf.Clamp(HeldObjectTileEulerAnglesFinal.x, -95f, 95f);
		HeldObjectTileEulerAnglesFinal.z -= _selectableManager.GetZSpin() - targetZSpin;

		_selectableManager.SetZSpin(targetZSpin);
		_selectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, targetZSpin));
		_selectableManager.SetHeldObjectTiltEulerAngles(HeldObjectTileEulerAnglesFinal);
		_selectableManager.HandleFaceSelection();
	}

	private IEnumerator DoFreeYRotate(float initialYSpin, float initialPitch, float targetYSpin, float targetPitch, float duration)
	{
		if (FloatingHoldable == null) yield break;
		if (!_heldFrontFace)
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

		RecordManager RecordManager = RecordManager.Instance;
		GameRecord GameRecord = RecordManager.GetCurrentRecord();
		StrikeSource[] Strikes = GameRecord.Strikes;
		if (Strikes.Length != strikeLimit)
		{
			StrikeSource[] newStrikes = new StrikeSource[Math.Max(strikeLimit, 1)];
			Array.Copy(Strikes, newStrikes, Math.Min(Strikes.Length, newStrikes.Length));
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
			timerComponent.SetRateModifier(rates[Math.Min(strikeCount, 4)]);
			Bomb.StrikeIndicator.StrikeCount = strikeCount;
		}
	}

	public bool IsSolved => Bomb.IsSolved();

	private float CurrentTimerElapsed => timerComponent.TimeElapsed;

	public float CurrentTimer
	{
		get => timerComponent.TimeRemaining;
		set => timerComponent.TimeRemaining = (value < 0) ? 0 : value;
	}

	public string CurrentTimerFormatted => timerComponent.GetFormattedTime(CurrentTimer, true);

	public string StartingTimerFormatted => timerComponent.GetFormattedTime(bombStartingTimer, true);

	public string GetFullFormattedTime => Math.Max(CurrentTimer, 0).FormatTime();

	public string GetFullStartingTime => Math.Max(bombStartingTimer, 0).FormatTime();

	public int StrikeCount
	{
		get => Bomb.NumStrikes;
		set
		{
			if (value < 0) value = 0; //Simon says is unsolvable with less than zero strikes.
			Bomb.NumStrikes = value;
			HandleStrikeChanges();
		}
	}

	public int StrikeLimit
	{
		get => Bomb.NumStrikesToLose;
		set { Bomb.NumStrikesToLose = value; HandleStrikeChanges(); }
	}

	public int NumberModules => bombSolvableModules;

	private static string[] solveBased = new[] { "MemoryV2", "SouvenirModule", "TurnTheKeyAdvanced", "HexiEvilFMN" };
	private bool removedSolveBasedModules = false;
	public void RemoveSolveBasedModules()
	{
		if (removedSolveBasedModules) return;
		removedSolveBasedModules = true;

		foreach (KMBombModule module in Bomb.GetComponentsInChildren<KMBombModule>().Where(x => solveBased.Contains(x.ModuleType)))
		{
			TwitchModule handle = BombMessageResponder.Instance.ComponentHandles.Where(x => x.BombComponent.GetComponent<KMBombModule>() != null)
				.FirstOrDefault(x => x.BombComponent.GetComponent<KMBombModule>() == module);
			if (handle != null)
			{
				handle.Unsupported = true;
				if (handle.Solver != null)
					handle.Solver.UnsupportedModule = true;
			}
			else
				ComponentSolver.HandleForcedSolve(module);
		}
	}
	#endregion

	public Bomb Bomb = null;
	private Selectable Selectable = null;
	private FloatingHoldable FloatingHoldable = null;
	public DateTime BombTimeStamp;
	public Dictionary<string, List<TwitchModule>> SolvedModules;

	private SelectableManager _selectableManager = null;

	public TwitchBombHandle twitchBombHandle = null;
	public TimerComponent timerComponent = null;
	private WidgetManager widgetManager = null;
	public int bombSolvableModules;
	public int bombSolvedModules;
	public float bombStartingTimer;

	private bool _heldFrontFace => _selectableManager.GetActiveFace() == FaceEnum.Front;
}
