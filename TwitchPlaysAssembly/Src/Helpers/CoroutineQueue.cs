using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineQueue : MonoBehaviour
{
	private void Awake()
	{
		_coroutineQueue = new Queue<IEnumerator>();
		_forceSolveQueue = new Queue<IEnumerator>();
		_bombIDProcessed = new Queue<int>();
	}

	private void Update()
	{
		if (!_processing && _coroutineQueue.Count > 0)
		{
			_processing = true;
			_activeCoroutine = StartCoroutine(ProcessQueueCoroutine());
		}

		if (_processingForcedSolve || _forceSolveQueue.Count <= 0) return;
		_processingForcedSolve = true;
		_activeForceSolveCoroutine = StartCoroutine(ProcessForcedSolveCoroutine());
	}

	public static void AddForcedSolve(IEnumerator subcoroutine) => _forceSolveQueue.Enqueue(subcoroutine);

	public void AddToQueue(IEnumerator subcoroutine)
	{
		_coroutineQueue.Enqueue(subcoroutine);
		QueueModified = true;
	}

	public void AddToQueue(IEnumerator subcoroutine, int bombID)
	{
		AddToQueue(subcoroutine);
		_bombIDProcessed.Enqueue(bombID);
	}

	public void CancelFutureSubcoroutines()
	{
		foreach (TwitchMessage twitchMessage in IRCConnection.Instance.MessageScrollContents
			.GetComponentsInChildren<TwitchMessage>())
			twitchMessage.RemoveMessage();

		_coroutineQueue.Clear();
		_bombIDProcessed.Clear();
	}

	public void StopQueue()
	{
		if (_activeCoroutine != null)
		{
			StopCoroutine(_activeCoroutine);
			_activeCoroutine = null;
		}

		_processing = false;

		CoroutineCanceller.ResetCancel();
	}

	public void StopForcedSolve()
	{
		if (_activeForceSolveCoroutine != null)
		{
			StopCoroutine(_activeForceSolveCoroutine);
			_activeForceSolveCoroutine = null;
		}
		_processingForcedSolve = false;
		_forceSolveQueue.Clear();
	}

	private IEnumerator ProcessQueueCoroutine()
	{
		CoroutineCanceller.ResetCancel();

		while (_coroutineQueue.Count > 0)
		{
			IEnumerator coroutine = _coroutineQueue.Dequeue();
			if (_bombIDProcessed.Count > 0)
				CurrentBombID = _bombIDProcessed.Dequeue();
			bool result = true;
			while (result)
			{
				try
				{
					result = coroutine.MoveNext();
				}
				catch (Exception e)
				{
					DebugHelper.LogException(e, "An exception occurred while executing a queued coroutine:");
					result = false;
				}
				if (result) yield return coroutine.Current;
			}
		}

		_processing = false;
		_activeCoroutine = null;

		CoroutineCanceller.ResetCancel();
	}

	private IEnumerator ProcessForcedSolveCoroutine()
	{
		while (_forceSolveQueue.Count > 0)
		{
			IEnumerator coroutine = _forceSolveQueue.Dequeue();
			bool result = true;
			while (result)
			{
				try
				{
					result = coroutine.MoveNext();
				}
				catch (Exception e)
				{
					DebugHelper.LogException(e, "An exception occurred while executing a force-solve coroutine:");
					result = false;
				}
				if (!result) continue;

				switch (coroutine.Current)
				{
					case bool boolean when boolean:
						_forceSolveQueue.Enqueue(coroutine);
						yield return null;
						result = false;
						break;
					default:
						yield return coroutine.Current;
						break;
				}
			}
		}

		_processingForcedSolve = false;
		_activeForceSolveCoroutine = null;
	}

	private static Queue<IEnumerator> _forceSolveQueue;
	private bool _processingForcedSolve;
	private Coroutine _activeForceSolveCoroutine;

	public bool QueueModified;
	private Queue<IEnumerator> _coroutineQueue;
	private bool _processing;
	private Coroutine _activeCoroutine;

	public int CurrentBombID = -1;
	private Queue<int> _bombIDProcessed;
}
