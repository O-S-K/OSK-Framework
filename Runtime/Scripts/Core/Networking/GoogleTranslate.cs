/* Limitations */
// translate.googleapis.com is free, but it only allows about 100 requests per one hour.
// After that, you will receive 429 error response.
// You may consider using paid service like google translate v2(https://translation.googleapis.com/language/translate/v2) to remove the limitations.
// Check here if you want to use paid version : https://gist.github.com/IJEMIN/fdff6db1b1131b91033cbf204247816e

/* // https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes

English: en
Korean (Tiếng Hàn): ko
Vietnamese (Tiếng Việt): vi
Japanese (Tiếng Nhật): ja
Chinese (Tiếng Trung): zh
...
*/
	
using System;
using System.Collections;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

namespace OSK
{
	public class GoogleTranslate : MonoBehaviour
	{
		public static GoogleTranslate Instance
		{
			get
			{
				if (_instance == null)
				{
					var go = new GameObject("GoogleTranslate");
					_instance = go.AddComponent<GoogleTranslate>();
				}

				return _instance;
			}
		}
		private static GoogleTranslate _instance;
		
		
		//example: TranslateText("en","ko","I'm a real gangster.", (success, translatedText) => { if (success) { Debug.Log(translatedText); } });
		public void TranslateText(string sourceLanguage, string targetLanguage, string sourceText,
			Action<bool, string> callback)
		{
			Main.UI.
			StartCoroutine(TranslateTextRoutine(sourceLanguage, targetLanguage, sourceText, callback));
		}

		private static IEnumerator TranslateTextRoutine(string sourceLanguage, string targetLanguage, string sourceText,
			Action<bool, string> callback)
		{
			var url =
				$"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLanguage}&tl={targetLanguage}&dt=t&q={UnityWebRequest.EscapeURL(sourceText)}";

			var webRequest = UnityWebRequest.Get(url);
			yield return webRequest.SendWebRequest();

			if (webRequest.isHttpError || webRequest.isNetworkError)
			{
				OSKLogger.LogError(webRequest.error);
				callback.Invoke(false, string.Empty);
				yield break;
			}

			var parsedTexts = JSONNode.Parse(webRequest.downloadHandler.text);
			var translatedText = parsedTexts[0][0][0];
			callback.Invoke(true, translatedText);
		}
	}

}