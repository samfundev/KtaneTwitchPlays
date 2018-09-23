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

	public static void AddForcedSolve(IEnumerator subcoroutine)
	{
		_forceSolveQueue.Enqueue(subcoroutine);
	}

	public void AddToQueue(IEnumerator subcoroutine)
	{
		_coroutineQueue.Enqueue(subcoroutine);
		queueModified = true;
	}

	public void AddToQueue(IEnumerator subcoroutine, int bombID)
	{
		AddToQueue(subcoroutine);
		_bombIDProcessed.Enqueue(bombID);
	}

	public void CancelFutureSubcoroutines()
	{
		foreach (TwitchMessage twitchMessage in IRCConnection.Instance.messageScrollContents
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
				catch
				{
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
				catch
				{
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

	private static Queue<IEnumerator> _forceSolveQueue = null;
	private bool _processingForcedSolve = false;
	private Coroutine _activeForceSolveCoroutine = null;

	public bool queueModified = false;
	private Queue<IEnumerator> _coroutineQueue = null;
	private bool _processing = false;
	private Coroutine _activeCoroutine = null;

	public int CurrentBombID = -1;
	private Queue<int> _bombIDProcessed = null;
}
