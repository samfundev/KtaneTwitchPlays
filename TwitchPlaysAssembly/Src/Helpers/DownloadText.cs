using UnityEngine;
using UnityEngine.Networking;

public class DownloadText : CustomYieldInstruction
{
	UnityWebRequest request;
	UnityWebRequestAsyncOperation asyncOperation;
	int retryCount;

	public DownloadText(string url)
	{
		request = UnityWebRequest.Get(url);
		asyncOperation = request.SendWebRequest();
	}

	bool success => !request.isNetworkError && !request.isHttpError;

	public override bool keepWaiting
	{
		get
		{
			if (!asyncOperation.isDone)
				return true;

			if (!success && retryCount < 5)
			{
				retryCount++;

				request = UnityWebRequest.Get(request.url);
				asyncOperation = request.SendWebRequest();
				return true;
			}

			return false;
		}
	}

	public string Text => success ? request.downloadHandler.text : null;
}