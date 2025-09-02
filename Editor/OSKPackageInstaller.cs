#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace OSK.Framework.Editor
{
    [InitializeOnLoad]
    public class OSKPackageInstaller : EditorWindow
    {
        private static readonly Dictionary<string, string> RequiredPackages = new Dictionary<string, string>
        {
            { "UIFeel", "https://github.com/O-S-K/UIFeel.git" },
            { "OSK-UIParticle", "https://github.com/O-S-K/OSK-UIParticle.git" },
            { "OSK-Observable", "https://github.com/O-S-K/OSK-Observable.git" },
            { "DOTween", "https://github.com/O-S-K/DOTween.git#1.2.765" },
            { "Newtonsoft JSON", "com.unity.nuget.newtonsoft-json@3.2.1" }
        };

        private static List<string> missingPackages = new List<string>();
        private static Dictionary<string, bool> selectedPackages = new Dictionary<string, bool>();
        private static Queue<(string name, string url)> installQueue = new Queue<(string, string)>();
        private static AddRequest addRequest;

        static OSKPackageInstaller()
        {
            // Khi mở project, kiểm tra package thiếu
            EditorApplication.update += CheckAndShowInstaller;
        }

        private static void CheckAndShowInstaller()
        {
            EditorApplication.update -= CheckAndShowInstaller;

            FindMissingPackages();

            if (missingPackages.Count > 0)
            {
                // Hiện popup tự động
                ShowWindow();
            }
        }

        [MenuItem("OSK/Package Installer")]
        public static void ShowWindow()
        {
            OSKPackageInstaller window = GetWindow<OSKPackageInstaller>("OSK Package Installer");
            window.minSize = new Vector2(420, 350);
            window.Show();
        }

        private static void FindMissingPackages()
        {
            missingPackages.Clear();
            selectedPackages.Clear();

            string manifestPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("OSK Package Installer: Không tìm thấy manifest.json!");
                return;
            }

            string manifestContent = File.ReadAllText(manifestPath);

            foreach (var kvp in RequiredPackages)
            {
                if (!manifestContent.Contains(kvp.Value.Split('@')[0]))
                {
                    missingPackages.Add(kvp.Key);
                    selectedPackages[kvp.Key] = true;
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("OSK Framework - Missing Dependencies", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (missingPackages.Count == 0)
            {
                EditorGUILayout.HelpBox("✅ Tất cả các package đã được cài đặt đầy đủ!", MessageType.Info);
                if (GUILayout.Button("Reload"))
                {
                    FindMissingPackages();
                }
                return;
            }

            EditorGUILayout.HelpBox("Chọn các package bạn muốn cài đặt:", MessageType.None);

            foreach (string pkg in missingPackages)
            {
                selectedPackages[pkg] = EditorGUILayout.ToggleLeft(
                    $"{pkg}   ({RequiredPackages[pkg]})",
                    selectedPackages[pkg]
                );
            }

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Chọn tất cả"))
            {
                foreach (var pkg in missingPackages) selectedPackages[pkg] = true;
            }
            if (GUILayout.Button("Bỏ chọn tất cả"))
            {
                foreach (var pkg in missingPackages) selectedPackages[pkg] = false;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            if (GUILayout.Button("🚀 Install Selected", GUILayout.Height(30)))
            {
                QueueSelectedPackages();
                InstallNextPackage();
            }
        }

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
                Debug.Log("<color=green>OSK Package Installer: Đã cài đặt tất cả package được chọn!</color>");
                FindMissingPackages();
                return;
            }

            var (name, url) = installQueue.Dequeue();
            Debug.Log($"OSK Package Installer: Đang cài <b>{name}</b> ...");

            addRequest = Client.Add(url);
            EditorApplication.update += ProgressDependencyInstall;
        }

        private static void ProgressDependencyInstall()
        {
            if (addRequest == null) return;
            if (!addRequest.IsCompleted) return;

            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"<color=cyan>OSK Package Installer: Cài thành công:</color> {addRequest.Result.packageId}");
            }
            else if (addRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError($"OSK Package Installer: Cài đặt thất bại!\n{addRequest.Error.message}");
            }

            EditorApplication.update -= ProgressDependencyInstall;
            InstallNextPackage();
        }
    }
}
#endif
