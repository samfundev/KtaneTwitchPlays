public sealed class ClaimQueueItem
{
	public string UserNickname { get; private set; }
	public double Timestamp { get; private set; }
	public bool ViewRequested { get; private set; }
	public bool ViewPinRequested { get; private set; }
	public ClaimQueueItem(string userNickname, double timestamp, bool viewRequested, bool viewPinRequested)
	{
		UserNickname = userNickname;
		Timestamp = timestamp;
		ViewRequested = viewRequested;
		ViewPinRequested = viewPinRequested;
	}
}
