using System;
using System.Collections;
using UnityEngine;

public class KMHoldableCommander : ICommandResponder
{

	public KMHoldableCommander(FloatingHoldable holdable, IRCConnection ircConnection, CoroutineCanceller canceller)
	{
		Holdable = holdable;
		Selectable = Holdable.GetComponent<Selectable>();
		FloatingHoldable = Holdable.GetComponent<FloatingHoldable>();
		_selectableManager = KTInputManager.Instance.SelectableManager;
		Handler = HoldableFactory.CreateHandler(this, holdable, ircConnection, canceller);
	}

	public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier, IRCConnection connection)
	{
		if (message.EqualsAny("hold", "pick up"))
		{
			responseNotifier?.ProcessResponse(CommandResponse.Start);

			IEnumerator holdCoroutine = Hold(_heldFrontFace);
			while (holdCoroutine.MoveNext())
			{
				yield return holdCoroutine.Current;
			}
			responseNotifier?.ProcessResponse(CommandResponse.EndNotComplete);
		}
		else if (message.EqualsAny("turn", "turn round", "turn around", "rotate", "flip", "spin"))
		{
			responseNotifier?.ProcessResponse(CommandResponse.Start);

			IEnumerator holdCoroutine = Hold(!_heldFrontFace);
			while (holdCoroutine.MoveNext())
			{
				yield return holdCoroutine.Current;
			}

			responseNotifier?.ProcessResponse(CommandResponse.EndNotComplete);
		}
		else if (message.EqualsAny("drop", "let go", "put down"))
		{
			responseNotifier?.ProcessResponse(CommandResponse.Start);

			IEnumerator letGoCoroutine = LetGoBomb();
			while (letGoCoroutine.MoveNext())
			{
				yield return letGoCoroutine.Current;
			}

			responseNotifier?.ProcessResponse(CommandResponse.EndNotComplete);
		}
		else
		{
			responseNotifier?.ProcessResponse(CommandResponse.Start);
			IEnumerator handler = Handler.RespondToCommand(userNickName, message, responseNotifier, connection);
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
					responseNotifier?.ProcessResponse(CommandResponse.EndError);
					yield break;
				}
				if (result)
					yield return handler.Current;
			} while (result);
			DebugHelper.Log($"Coroutine for holdable {Holdable.name} finished");
			responseNotifier?.ProcessResponse(CommandResponse.EndNotComplete);
		}
	}


	public IEnumerator Hold(bool frontFace = true)
	{
		FloatingHoldable.HoldStateEnum holdState = FloatingHoldable.HoldState;
		bool doForceRotate = false;

		if (holdState != FloatingHoldable.HoldStateEnum.Held)
		{
			SelectObject(Selectable);
			doForceRotate = true;
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
		if (FloatingHoldable.HoldState != FloatingHoldable.HoldStateEnum.Held) yield break;

		IEnumerator turnCoroutine = Hold(true);
		while (turnCoroutine.MoveNext())
		{
			yield return turnCoroutine.Current;
		}

		while (FloatingHoldable.HoldState == FloatingHoldable.HoldStateEnum.Held)
		{
			DeselectObject(Selectable);
			yield return new WaitForSeconds(0.1f);
		}
	}


	public void RotateByLocalQuaternion(Quaternion localQuaternion)
	{
		Transform baseTransform = _selectableManager.GetBaseHeldObjectTransform();

		float currentZSpin = _heldFrontFace ? 0.0f : 180.0f;

		_selectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, currentZSpin) * localQuaternion);
		_selectableManager.HandleFaceSelection();
	}

	private void SelectObject(Selectable selectable)
	{
		selectable.HandleSelect(true);
		_selectableManager.Select(selectable, true);
		_selectableManager.HandleInteract();
		selectable.OnInteractEnded();
	}

	private void DeselectObject(Selectable selectable)
	{
		_selectableManager.HandleCancel();
	}

	private IEnumerator ForceHeldRotation(bool frontFace, float duration)
	{
		Transform baseTransform = _selectableManager.GetBaseHeldObjectTransform();

		float oldZSpin = _heldFrontFace ? 0.0f : 180.0f;
		float targetZSpin = frontFace ? 0.0f : 180.0f;

		float initialTime = Time.time;
		while (Time.time - initialTime < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			float currentZSpin = Mathf.SmoothStep(oldZSpin, targetZSpin, lerp);

			Quaternion currentRotation = Quaternion.Euler(0.0f, 0.0f, currentZSpin);

			_selectableManager.SetZSpin(currentZSpin);
			_selectableManager.SetControlsRotation(baseTransform.rotation * currentRotation);
			_selectableManager.HandleFaceSelection();
			yield return null;
		}

		_selectableManager.SetZSpin(targetZSpin);
		_selectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, targetZSpin));
		_selectableManager.HandleFaceSelection();

		_heldFrontFace = frontFace;
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

	public FloatingHoldable Holdable = null;
	public HoldableHandler Handler = null;
	public string ID = null;
	

	public Selectable Selectable = null;
	public FloatingHoldable FloatingHoldable = null;
	private SelectableManager _selectableManager = null;
	private bool _heldFrontFace = true;
}
