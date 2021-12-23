public sealed class ClaimQueueItem
{
	public string UserNickname { get; }
	public bool ViewRequested { get; }
	public bool ViewPinRequested { get; }
	public ClaimQueueItem(string userNickname, bool viewRequested, bool viewPinRequested)
	{
		UserNickname = userNickname;
		ViewRequested = viewRequested;
		ViewPinRequested = viewPinRequested;
	}
}
