using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OSK
{
    public class WebRequestManager : GameFrameworkComponent
    {
        private readonly Dictionary<string, string> _defaultHeaders = new Dictionary<string, string>();

        public override void OnInit()
        {
        }

        public void AddDefaultHeader(string key, string value)
        {
            if (!_defaultHeaders.ContainsKey(key))
                _defaultHeaders.Add(key, value);
            else
                _defaultHeaders[key] = value;
        }

        public void RemoveDefaultHeader(string key)
        {
            if (_defaultHeaders.ContainsKey(key))
                _defaultHeaders.Remove(key);
        }
        
        public void DownloadCSV(string url, System.Action<string> callback)
        {
            DownloadCoroutine(url, callback).Run();
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
                    MyLogger.LogError($"⚠️ Error downloading CSV: {webRequest.error}");
                }
            }
        }

        // GET request
        public void Get(string url, System.Action<string> onSuccess, System.Action<string> onError)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            ApplyDefaultHeaders(webRequest);
            HandleRequest(webRequest, onSuccess, onError).Run();
        }

        // POST request with WWWForm
        public void Post(string url, WWWForm formData, System.Action<string> onSuccess, System.Action<string> onError)
        {
            UnityWebRequest webRequest = UnityWebRequest.Post(url, formData);
            ApplyDefaultHeaders(webRequest);
            HandleRequest(webRequest, onSuccess, onError).Run();
        }

        // POST request with JSON
        public void PostJson(string url, string jsonData, System.Action<string> onSuccess, System.Action<string> onError)
        {
            UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            ApplyDefaultHeaders(webRequest);
            HandleRequest(webRequest, onSuccess, onError).Run();
        }

        // PUT request with JSON
        public void Put(string url, string jsonData, System.Action<string> onSuccess, System.Action<string> onError)
        {
            UnityWebRequest webRequest = UnityWebRequest.Put(url, jsonData);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            ApplyDefaultHeaders(webRequest);
            HandleRequest(webRequest, onSuccess, onError).Run();
        }

        // DELETE request
        public void Delete(string url, System.Action<string> onSuccess, System.Action<string> onError)
        {
            UnityWebRequest webRequest = UnityWebRequest.Delete(url);
            ApplyDefaultHeaders(webRequest);
            HandleRequest(webRequest, onSuccess, onError).Run();
        }

        //  Apply default headers to request
        private void ApplyDefaultHeaders(UnityWebRequest webRequest)
        {
            foreach (var header in _defaultHeaders)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }
        }

        protected virtual void OnBeforeRequest(UnityWebRequest webRequest)
        {
            MyLogger.Log($"[WebRequestManager] Sending request to: {webRequest.url}");
        }

        protected virtual void OnAfterRequest(UnityWebRequest webRequest, bool isSuccess)
        {
            if (isSuccess)
                MyLogger.Log($"[WebRequestManager] Request successful: {webRequest.downloadHandler.text}");
            else
                MyLogger.LogError($"[WebRequestManager] Request failed: {webRequest.error}");
        }

        private IEnumerator HandleRequest(UnityWebRequest webRequest, System.Action<string> onSuccess,
            System.Action<string> onError)
        {
            OnBeforeRequest(webRequest);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                OnAfterRequest(webRequest, false);
                onError?.Invoke(webRequest.error);
            }
            else
            {
                OnAfterRequest(webRequest, true);
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
        }
    }
}