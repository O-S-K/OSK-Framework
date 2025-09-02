#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Text.RegularExpressions;

namespace OSK.Framework.Editor
{
    [InitializeOnLoad]
    public class OSKPackageInstaller : EditorWindow
    {
        // === CONFIGURABLE PACKAGES ===
        private static readonly Dictionary<string, string> RequiredPackages = new Dictionary<string, string>
        {
            { "UIFeel", "https://github.com/O-S-K/UIFeel.git" },
            { "OSK-UIParticle", "https://github.com/O-S-K/OSK-UIParticle.git" },
            { "OSK-Observable", "https://github.com/O-S-K/OSK-Observable.git" }
        };

        // === INTERNAL STATE ===
        private static List<string> missingPackages = new List<string>();
        private static Dictionary<string, bool> selectedPackages = new Dictionary<string, bool>();
        private static Queue<(string name, string url)> installQueue = new Queue<(string, string)>();
        private static AddRequest addRequest;

        private static Dictionary<string, string> installedVersions = new Dictionary<string, string>();
        private static string manifestPath;
        private static string customGitUrl = "";

        static OSKPackageInstaller()
        {
            EditorApplication.update += CheckAndShowInstaller;
        }

        private static void CheckAndShowInstaller()
        {
            EditorApplication.update -= CheckAndShowInstaller;

            RefreshPackageStatus();

            if (missingPackages.Count > 0)
                ShowWindow();
        }

        [MenuItem("OSK Framework/Tools/Package Installer")]
        public static void ShowWindow()
        {
            OSKPackageInstaller window = GetWindow<OSKPackageInstaller>("OSK Package Installer");
            window.minSize = new Vector2(500, 420);
            window.Show();
        }

        // === READ INSTALLED PACKAGES & FIND MISSING ===
        private static void RefreshPackageStatus()
        {
            missingPackages.Clear();
            selectedPackages.Clear();
            installedVersions.Clear();

            manifestPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Packages",
                "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("OSK Package Installer: Cannot find manifest.json!");
                return;
            }

            string manifestContent = File.ReadAllText(manifestPath);

            foreach (var kvp in RequiredPackages)
            {
                string pkgName = kvp.Key;
                string url = kvp.Value;

                // Check if installed
                if (manifestContent.Contains(url))
                {
                    // Extract version if available
                    string pattern = $"\"{Regex.Escape(url)}(@([^\"]+))?\"";
                    var match = Regex.Match(manifestContent, pattern);

                    installedVersions[pkgName] =
                        match.Success && match.Groups.Count > 2 ? match.Groups[2].Value : "latest";
                }
                else
                {
                    missingPackages.Add(pkgName);
                    selectedPackages[pkgName] = true;
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("OSK Framework - Package Manager", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // === Installed Packages Status ===
            GUILayout.Label("Installed Packages:", EditorStyles.miniBoldLabel);
            if (installedVersions.Count == 0)
            {
                EditorGUILayout.HelpBox("No required packages installed yet.", MessageType.Warning);
            }
            else
            {
                foreach (var pkg in RequiredPackages)
                {
                    string version = installedVersions.ContainsKey(pkg.Key)
                        ? installedVersions[pkg.Key]
                        : "<Not Installed>";
                    GUILayout.Label($"• {pkg.Key}  →  <b>{version}</b>",
                        new GUIStyle(EditorStyles.label) { richText = true });
                }
            }

            GUILayout.Space(10);

            // === Missing Packages Section ===
            if (missingPackages.Count > 0)
            {
                GUILayout.Label("Missing Packages:", EditorStyles.miniBoldLabel);
                foreach (string pkg in missingPackages)
                {
                    selectedPackages[pkg] = EditorGUILayout.ToggleLeft(
                        $"{pkg} ({RequiredPackages[pkg]})",
                        selectedPackages[pkg]
                    );
                }

                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All"))
                    foreach (var pkg in missingPackages)
                        selectedPackages[pkg] = true;
                if (GUILayout.Button("Deselect All"))
                    foreach (var pkg in missingPackages)
                        selectedPackages[pkg] = false;
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                if (GUILayout.Button("🚀 Install / Update Selected", GUILayout.Height(30)))
                {
                    QueueSelectedPackages();
                    InstallNextPackage();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✅ All required packages are installed.", MessageType.Info);
            }

            GUILayout.Space(15);

            // === Custom Package Installer ===
            GUILayout.Label("Install Custom Git Package:", EditorStyles.boldLabel);
            customGitUrl = EditorGUILayout.TextField("Git URL:", customGitUrl);

            GUI.enabled = !string.IsNullOrWhiteSpace(customGitUrl);
            if (GUILayout.Button("➕ Install / Update Custom Package", GUILayout.Height(25)))
            {
                installQueue.Clear();
                installQueue.Enqueue(("Custom", customGitUrl));
                InstallNextPackage();
            }

            GUI.enabled = true;

            GUILayout.Space(15);

            if (GUILayout.Button("Refresh Status", GUILayout.Height(25)))
            {
                RefreshPackageStatus();
            }

            if (GUILayout.Button("Open Manifest File", GUILayout.Height(25)))
            {
                EditorUtility.RevealInFinder(manifestPath);
            }
        }

        // === INSTALLATION QUEUE ===
        private static void QueueSelectedPackages()
        {
            installQueue.Clear();
            foreach (var pkg in missingPackages)
            {
                if (selectedPackages[pkg])
                    installQueue.Enqueue((pkg, RequiredPackages[pkg]));
            }
        }

        private static void InstallNextPackage()
        {
            if (installQueue.Count == 0)
            {
                Debug.Log(
                    "<color=green>OSK Package Installer: All selected packages have been installed or updated!</color>");
                RefreshPackageStatus();
                return;
            }

            var (name, url) = installQueue.Dequeue();
            Debug.Log($"OSK Package Installer: Installing / Updating <b>{name}</b> ...");

            addRequest = Client.Add(url);
            EditorApplication.update += ProgressDependencyInstall;
        }

        private static void ProgressDependencyInstall()
        {
            if (addRequest == null || !addRequest.IsCompleted) return;

            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log(
                    $"<color=cyan>OSK Package Installer: Successfully installed/updated:</color> {addRequest.Result.packageId}");
            }
            else if (addRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError($"OSK Package Installer: Failed to install/update package!\n{addRequest.Error.message}");
            }

            EditorApplication.update -= ProgressDependencyInstall;
            InstallNextPackage();
        }
    }
}
#endif