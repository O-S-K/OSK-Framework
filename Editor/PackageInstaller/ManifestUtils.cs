using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ManifestUtils
{
    [Serializable]
    private class ManifestData
    {
        public Dictionary<string, string> dependencies;
    }

    public static string GetInstalledPackageVersion(string packageName)
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        if (!File.Exists(manifestPath)) return null;

        string json = File.ReadAllText(manifestPath);
        var manifest = JsonUtility.FromJson<ManifestData>(json);

        if (manifest.dependencies != null && manifest.dependencies.ContainsKey(packageName))
            return manifest.dependencies[packageName];

        return null; // Chưa cài
    }
}