using System.Collections;
using System.Collections.Generic;
using System.Text;
using OSK;
using UnityEngine;
using UnityEngine.Networking;

public class CSVDownloader : MonoBehaviour
{
    public void DownloadCSV(string url, System.Action<string> callback)
    {
        StartCoroutine(DownloadCoroutine(url, callback));
    }

    private IEnumerator DownloadCoroutine(string url, System.Action<string> callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string csvData = Encoding.UTF8.GetString(webRequest.downloadHandler.data);
                callback(csvData);
            }
            else
            {
                OSKLogger.LogError("Web",$"⚠️ Error downloading CSV: {webRequest.error}");
            }
        }
        Destroy(gameObject); 
    }
}