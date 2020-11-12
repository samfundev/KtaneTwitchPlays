using UnityEngine;
using UnityEngine.Networking;

public class DownloadText : CustomYieldInstruction
{
	readonly UnityWebRequest request;
	readonly UnityWebRequestAsyncOperation asyncOperation;

	public DownloadText(string url)
	{
		request = UnityWebRequest.Get(url);
		asyncOperation = request.SendWebRequest();
	}

	public override bool keepWaiting => !asyncOperation.isDone;

	public string Text => (!request.isNetworkError && !request.isHttpError) ? request.downloadHandler.text : null;
}