public sealed class ClaimQueueItem
{
	public string UserNickname { get; private set; }
	public bool ViewRequested { get; private set; }
	public bool ViewPinRequested { get; private set; }
	public ClaimQueueItem(string userNickname, bool viewRequested, bool viewPinRequested)
	{
		UserNickname = userNickname;
		ViewRequested = viewRequested;
		ViewPinRequested = viewPinRequested;
	}
}
