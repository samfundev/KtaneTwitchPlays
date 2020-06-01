using System.Reflection;
using Assets.Scripts.Leaderboards;
using Assets.Scripts.Records;
using Assets.Scripts.Services;
using Assets.Scripts.Services.Steam;
using Assets.Scripts.Stats;
using Events;

/// <summary>
/// Controls whether or not records will be saved to the leaderboard and if stats can be changed.
/// Call <see cref="Install"/> to set it up and then call <see cref="DisableLeaderboards"/> to disable the leaderboards.
/// Leaderboards will re-enabled once the game goes back to the setup state. Make sure to disable them again after that if they should still be disabled.
/// </summary>
static class LeaderboardController
{
	private static readonly FieldInfo InstanceField = typeof(AbstractServices).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);

	/// <summary>Installs everything necessary to work and only needs to be called once. Records will still be saved if this is not called.</summary>
	public static void Install()
	{
		// Re-enable the leaderboards once we get back to the setup
		GameEvents.OnGameStateChange += state =>
		{
			if (state == SceneManager.State.Setup)
				EnableLeaderboards();
		};

		// Check if another copy of this script has already installed the filter service.
		if (InstanceField.GetValue(null).GetType().Name != "SteamFilterService")
		{
			InstanceField.SetValue(null, new SteamFilterService());
		}
	}

	/// <summary>
	/// Disables records being saved to leaderboards and stat changes.
	/// Leaderboards are re-enabled once the game goes back to the setup state.
	/// Make sure to disable them again after that if they should still be disabled
	/// </summary>
	public static void DisableLeaderboards()
	{
		StatsManager.Instance.DisableStatChanges =
		RecordManager.Instance.DisableBestRecords = true;
	}

	private static void EnableLeaderboards()
	{
		StatsManager.Instance.DisableStatChanges =
		RecordManager.Instance.DisableBestRecords = false;
	}
}

class SteamFilterService : ServicesSteam
{
	private static readonly PropertyInfo SubmitFieldProperty = typeof(LeaderboardListRequest).GetProperty("SubmitScore", BindingFlags.Public | BindingFlags.Instance);

	public override void ExecuteLeaderboardRequest(LeaderboardRequest request)
	{
		LeaderboardListRequest listRequest = request as LeaderboardListRequest;
		if (RecordManager.Instance.DisableBestRecords && listRequest?.SubmitScore == true && listRequest?.MissionID == GameplayState.MissionToLoad)
		{
			SubmitFieldProperty.SetValue(listRequest, false, null);
		}

		base.ExecuteLeaderboardRequest(request);
	}
}
