using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

namespace OSK
{
    /* Example
        Main.Director
        .LoadScene( 
            new DataScene(){sceneName = "Game", loadMode = ELoadMode.Single}, 
            //new DataScene(){sceneName = "Game", loadMode = ELoadMode.Reload}, 
            new DataScene(){sceneName = "Additive", loadMode = ELoadMode.Additive})
        .Async(true)
        .FakeDuration(timeLoad)
        .OnStart(() =>
        {
            Main.UI.Open<TransitionUI>(new object[]{timeLoad});
        }).OnComplete(() =>
        {
            Main.UI.Get<TransitionUI>().Hide();
        }).Build();
    */

    public class DirectorManager : GameFrameworkComponent
    {
        [SerializeField, ReadOnly] private string _currentSceneName = "";
        [SerializeField, ReadOnly] private bool _isLoading = false;
        [SerializeField, ReadOnly] private float _loadingProgress = 0f;
        public HashSet<string> LoadedScenes { get; private set; } = new HashSet<string>();
        private float timer;

        public override void OnInit()
        {
        }

        public SceneLoadBuilder LoadScene(params DataScene[] sceneNames)
        {
            return new SceneLoadBuilder(this, sceneNames);
        }

        #region Public API

        public void UnloadScene(string sceneName, Action onComplete = null)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                Logg.LogWarning("Director",$"[DirectorManager] Scene '{sceneName}' chưa được load hoặc đã unload.");
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(UnloadSceneCoroutine(sceneName, onComplete));
        }

        #endregion

        #region Core Loading Logic

        internal void StartLoad(DataScene[] scenes, bool async, float fakeDuration, Action onStart, Action onComplete)
        {
            if (!ValidateScenes(scenes))
            {
                onComplete?.Invoke();
                return;
            }

            StopAllCoroutines();
            StartCoroutine(LoadScenesCoroutine(scenes, async, fakeDuration, fakeDuration > 0f, onStart, onComplete));
        }

        private IEnumerator LoadScenesCoroutine(DataScene[] scenes, bool async,
            float minLoadTime, bool fakeLoading, Action onStart, Action onComplete)
        {
            _isLoading = true;
            _loadingProgress = 0f;
            onStart?.Invoke();

            List<AsyncOperation> ops = new List<AsyncOperation>();
            yield return PrepareScenes(scenes, async, ops);

            if (async && ops.Count > 0)
                yield return HandleAsyncLoading(ops, fakeLoading, minLoadTime,
                    Array.ConvertAll(scenes, s => s.sceneName));

            _isLoading = false;
            _loadingProgress = 1f;
            Logg.Log("Director",$"[DirectorManager] Scenes loaded:" +
                                $" {string.Join(", ", Array.ConvertAll(scenes, s => s.sceneName))}");
            onComplete?.Invoke();
        }

        private IEnumerator PrepareScenes(DataScene[] scenes, bool async, List<AsyncOperation> ops)
        {
            // Nếu load mode là Single => clear những scene cũ nhưng giữ lại additive có autoRemove = false
            bool isSingleLoad = Array.Exists(scenes, s => s.loadMode == ELoadMode.Single);

            // Nếu scene hiện tại đang là Single và có scene mới load => unload scene hiện tại
            if (isSingleLoad)
            {
                List<string> scenesToRemove = new List<string>();

                foreach (var loaded in LoadedScenes)
                {
                    // Nếu scene này không nằm trong danh sách load mới và autoRemove = true => xoá
                    bool shouldRemove = scenes.All(s => s.sceneName != loaded || s.autoRemove != false);
                    if (shouldRemove)
                        scenesToRemove.Add(loaded);
                }

                // Thực hiện unload
                foreach (var sceneName in scenesToRemove)
                {
                    yield return SceneManager.UnloadSceneAsync(sceneName);
                    LoadedScenes.Remove(sceneName);
                }
            }

            // Load các scene mới
            foreach (var sceneData in scenes)
            {
                if (sceneData.loadMode == ELoadMode.Single)
                {
                    _currentSceneName = sceneData.sceneName;

                    if (async)
                    {
                        var op = SceneManager.LoadSceneAsync(sceneData.sceneName, LoadSceneMode.Single);
                        op.allowSceneActivation = false;
                        ops.Add(op);
                    }
                    else
                    {
                        SceneManager.LoadScene(sceneData.sceneName, LoadSceneMode.Single);
                        LoadedScenes.Add(sceneData.sceneName);
                    }
                }
                else if (sceneData.loadMode == ELoadMode.Additive)
                {
                    if (LoadedScenes.Contains(sceneData.sceneName)) continue;
                    if (async)
                    {
                        var op = SceneManager.LoadSceneAsync(sceneData.sceneName, LoadSceneMode.Additive);
                        op.allowSceneActivation = false;
                        ops.Add(op);
                    }
                    else
                    {
                        SceneManager.LoadScene(sceneData.sceneName, LoadSceneMode.Additive);
                        LoadedScenes.Add(sceneData.sceneName);
                    }
                }
                else if (sceneData.loadMode == ELoadMode.Reload)
                {
                    if (LoadedScenes.Contains(sceneData.sceneName))
                    {
                        yield return SceneManager.UnloadSceneAsync(sceneData.sceneName);
                        LoadedScenes.Remove(sceneData.sceneName);
                    }

                    if (async)
                    {
                        var op = SceneManager.LoadSceneAsync(sceneData.sceneName, LoadSceneMode.Additive);
                        op.allowSceneActivation = false;
                        ops.Add(op);
                    }
                    else
                    {
                        SceneManager.LoadScene(sceneData.sceneName, LoadSceneMode.Additive);
                        LoadedScenes.Add(sceneData.sceneName);
                    }
                }
            }
        }
         

        private bool ValidateScenes(DataScene[] scenes)
        {
            foreach (var s in scenes)
            {
                if (!SceneExists(s.sceneName))
                {
                    Logg.LogError("Director",$"[DirectorManager] Scene '{s.sceneName}' does not exist in Build Settings.");
                    return false;
                }
            }

            return true;
        }

        private IEnumerator HandleAsyncLoading(List<AsyncOperation> ops, bool fakeLoading,
            float minLoadTime, string[] sceneNames)
        {
            timer = 0f;
            while (true)
            {
                timer += Time.deltaTime;

                float totalProgress = 0f;
                foreach (var op in ops)
                    totalProgress += Mathf.Clamp01(op.progress / 0.9f);

                _loadingProgress = fakeLoading
                    ? Mathf.Clamp01(timer / minLoadTime)
                    : totalProgress / ops.Count;

                if ((!fakeLoading && _loadingProgress >= 1f && timer >= minLoadTime) ||
                    (fakeLoading && timer >= minLoadTime))
                {
                    foreach (var op in ops) op.allowSceneActivation = true;
                    break;
                }

                yield return null;
            }

            // Wait until all async load finished
            foreach (var op in ops)
                while (!op.isDone)
                    yield return null;

            foreach (var s in sceneNames)
                LoadedScenes.Add(s);
        }

        private IEnumerator UnloadSceneCoroutine(string sceneName, Action onComplete)
        {
            AsyncOperation asyncOp = SceneManager.UnloadSceneAsync(sceneName);
            if (asyncOp == null)
            {
                Logg.LogError("Director",$"[DirectorManager] Không thể unload scene '{sceneName}'");
                onComplete?.Invoke();
                yield break;
            }

            while (!asyncOp.isDone)
                yield return null;

            LoadedScenes.Remove(sceneName);
            Logg.Log("Director",$"[DirectorManager] Đã unload scene '{sceneName}' thành công.");
            onComplete?.Invoke();
        }

        private bool SceneExists(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName) return true;
            }

            return false;
        }

        public void ReloadSceneForce(string sceneName)
        {
            StartCoroutine(ReloadSceneForceRoutine(sceneName));
        }

        private IEnumerator ReloadSceneForceRoutine(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneName);
                if (unloadOp != null)
                {
                    while (!unloadOp.isDone)
                    {
                        yield return null;
                    }
                }
            }
            yield return null;
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOp != null)
            {
                while (!loadOp.isDone)
                {
                    yield return null;
                }
            }

            Logg.Log("Director",$"[DirectorManager] Reloaded scene: {sceneName}");
        }
        #endregion
    }
}