using System;
using UnityEngine.Networking;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class PackageJsonData
{
    public string name;
    public string version;
}

public static class GitUtils
{
    public static async Task<string> GetLatestVersionFromGit(string gitUrl)
    {
        // Lấy file package.json từ GitHub
        string rawUrl = gitUrl.Replace(".git", "") + "/master/package.json";

        using (UnityWebRequest www = UnityWebRequest.Get(rawUrl))
        {
            var asyncOp = www.SendWebRequest();

            while (!asyncOp.isDone) await Task.Yield();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<PackageJsonData>(www.downloadHandler.text);
                return data.version;
            }
            else
            {
                Debug.LogError($"Failed to fetch package.json for {gitUrl}: {www.error}");
                return null;
            }
        }
    }
}