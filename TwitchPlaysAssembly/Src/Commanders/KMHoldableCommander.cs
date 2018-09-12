using System;
using System.Collections;
using UnityEngine;

public class KMHoldableCommander
{
	public KMHoldableCommander(FloatingHoldable holdable)
	{
		Holdable = holdable;
		Selectable = Holdable.GetComponent<Selectable>();
		FloatingHoldable = Holdable.GetComponent<FloatingHoldable>();
		Handler = HoldableFactory.CreateHandler(this, holdable);
	}

	public IEnumerator RespondToCommand(string userNickName, string message, bool isWhisper = false)
	{
		message = message.Trim();
		if (message.EqualsAny("hold", "pick up"))
		{
			IEnumerator holdCoroutine = Hold(_heldFrontFace);
			while (holdCoroutine.MoveNext())
			{
				yield return holdCoroutine.Current;
			}
		}
		else if (message.EqualsAny("turn", "turn round", "turn around", "rotate", "flip", "spin"))
		{
			IEnumerator holdCoroutine = Hold(!_heldFrontFace);
			while (holdCoroutine.MoveNext())
			{
				yield return holdCoroutine.Current;
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
		else
		{
			IEnumerator handler = Handler.RespondToCommand(userNickName, message, isWhisper);
			bool result;
			DebugHelper.Log($"Coroutine for holdable {Holdable.name} started");
			do
			{
				try
				{
					result = handler.MoveNext();
				}
				catch (Exception ex)
				{
					DebugHelper.LogException(ex, "Could not process holdable handler command due to an Exception:");
					yield break;
				}
				if (result)
					yield return handler.Current;
			} while (result);
			DebugHelper.Log($"Coroutine for holdable {Holdable.name} finished");
		}
	}

	public IEnumerator Hold(bool frontFace = true)
	{
		FloatingHoldable holdable = FloatingHoldable.GetComponent<FloatingHoldable>();
		FloatingHoldable.HoldStateEnum holdState = holdable.HoldState;
		bool doForceRotate = false;

		if (holdState != global::FloatingHoldable.HoldStateEnum.Held)
		{
			SelectObject(Selectable.GetComponent<Selectable>());
			doForceRotate = true;
		}
		else if (frontFace != _heldFrontFace)
		{
			doForceRotate = true;
		}

		if (!doForceRotate) yield break;

		float holdTime = holdable.PickupTime;
		IEnumerator forceRotationCoroutine = ForceHeldRotation(frontFace, holdTime);
		while (forceRotationCoroutine.MoveNext())
		{
			yield return forceRotationCoroutine.Current;
		}
	}

	public IEnumerator TurnHoldable()
	{
		IEnumerator holdCoroutine = Hold(!_heldFrontFace);
		while (holdCoroutine.MoveNext())
		{
			yield return holdCoroutine.Current;
		}
	}

	public IEnumerator LetGoBomb()
	{
		if (FloatingHoldable.GetComponent<FloatingHoldable>().HoldState != global::FloatingHoldable.HoldStateEnum.Held) yield break;

		IEnumerator turnCoroutine = Hold(true);
		while (turnCoroutine.MoveNext())
		{
			yield return turnCoroutine.Current;
		}

		while (FloatingHoldable.GetComponent<FloatingHoldable>().HoldState == global::FloatingHoldable.HoldStateEnum.Held)
		{
			DeselectObject(Selectable.GetComponent<Selectable>());
			yield return new WaitForSeconds(0.1f);
		}
	}

	public void RotateByLocalQuaternion(Quaternion localQuaternion)
	{
		SelectableManager selectableManager = KTInputManager.Instance.SelectableManager;
		Transform baseTransform = selectableManager.GetBaseHeldObjectTransform();

		float currentZSpin = _heldFrontFace ? 0.0f : 180.0f;

		selectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, currentZSpin) * localQuaternion);
		selectableManager.HandleFaceSelection();
	}

	private void SelectObject(Selectable selectable)
	{
		SelectableManager selectableManager = KTInputManager.Instance.SelectableManager;
		selectable.HandleSelect(true);
		selectableManager.Select(selectable, true);
		selectableManager.HandleInteract();
		selectable.OnInteractEnded();
	}

	private void DeselectObject(Selectable selectable)
	{
		SelectableManager selectableManager = KTInputManager.Instance.SelectableManager;
		selectableManager.HandleCancel();
	}

	private IEnumerator ForceHeldRotation(bool frontFace, float duration)
	{
		SelectableManager selectableManager = KTInputManager.Instance.SelectableManager;
		Transform baseTransform = selectableManager.GetBaseHeldObjectTransform();

		float oldZSpin = selectableManager.GetZSpin();
		float targetZSpin = frontFace ? 0.0f : 180.0f;

		float initialTime = Time.time;
		while (Time.time - initialTime < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			float currentZSpin = Mathf.SmoothStep(oldZSpin, targetZSpin, lerp);

			Quaternion currentRotation = Quaternion.Euler(0.0f, 0.0f, currentZSpin);
			Vector3 HeldObjectTiltEulerAngles = selectableManager.GetHeldObjectTiltEulerAngles();
			HeldObjectTiltEulerAngles.x = Mathf.Clamp(HeldObjectTiltEulerAngles.x, -95f, 95f);
			HeldObjectTiltEulerAngles.z -= selectableManager.GetZSpin() - currentZSpin;

			selectableManager.SetZSpin(currentZSpin);
			selectableManager.SetControlsRotation(baseTransform.rotation * currentRotation);
			selectableManager.SetHeldObjectTiltEulerAngles(HeldObjectTiltEulerAngles);
			selectableManager.HandleFaceSelection();
			yield return null;
		}

		Vector3 HeldObjectTileEulerAnglesFinal = selectableManager.GetHeldObjectTiltEulerAngles();
		HeldObjectTileEulerAnglesFinal.x = Mathf.Clamp(HeldObjectTileEulerAnglesFinal.x, -95f, 95f);
		HeldObjectTileEulerAnglesFinal.z -= selectableManager.GetZSpin() - targetZSpin;

		selectableManager.SetZSpin(targetZSpin);
		selectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, targetZSpin));
		selectableManager.SetHeldObjectTiltEulerAngles(HeldObjectTileEulerAnglesFinal);
		selectableManager.HandleFaceSelection();
	}

	private IEnumerator DoFreeYRotate(float initialYSpin, float initialPitch, float targetYSpin, float targetPitch, float duration)
	{
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

	public MonoBehaviour Holdable;
	public HoldableHandler Handler;
	public string ID = null;

	public MonoBehaviour Selectable;
	public MonoBehaviour FloatingHoldable;

	private bool _heldFrontFace => KTInputManager.Instance.SelectableManager.GetZSpin() > 270f || KTInputManager.Instance.SelectableManager.GetZSpin() < 90f;
}
