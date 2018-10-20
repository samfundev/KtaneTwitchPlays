using System.Collections;
using UnityEngine.Serialization;

public class PostGameMessageResponder : MessageResponder
{
	[FormerlySerializedAs("twitchLeaderboardPrefab")] public TwitchLeaderboard TwitchLeaderboardPrefab;

	private PostGameCommander _postGameCommander;
	private TwitchLeaderboard _leaderboardDisplay;

	#region Unity Lifecycle
	private void OnEnable()
	{
		StartCoroutine(CheckForResultsPage());

		_leaderboardDisplay = Instantiate(TwitchLeaderboardPrefab);

		Leaderboard.Instance.SaveDataToFile();
	}

	private void OnDisable()
	{
		StopAllCoroutines();

		_postGameCommander = null;

		DestroyObject(_leaderboardDisplay);
		_leaderboardDisplay = null;
	}
	#endregion

	#region Protected/Private Methods
	protected override void OnMessageReceived(Message message)
	{
		if (_postGameCommander != null)
		{
			CoroutineQueue.AddToQueue(_postGameCommander.RespondToCommand(message, null));
		}
	}

	private IEnumerator CheckForResultsPage()
	{
		yield return null;

		while (true)
		{
			ResultPage[] resultPages = FindObjectsOfType<ResultPage>();
			if (resultPages != null && resultPages.Length > 0)
			{
				ResultPage resultPageBehaviour = resultPages[0];
				_postGameCommander = new PostGameCommander(resultPageBehaviour);
				break;
			}

			yield return null;
		}
		InputInterceptor.EnableInput();
	}
	#endregion
}
