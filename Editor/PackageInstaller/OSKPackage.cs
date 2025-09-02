using System;
using UnityEngine;

[Serializable]
public class OSKPackage
{
    public string name;
    public string gitUrl;
    public string latestVersion;
    public string installedVersion;
    public PackageState state;
}

public enum PackageState
{
    NotInstalled,
    UpToDate,
    UpdateAvailable,
    Fetching
}