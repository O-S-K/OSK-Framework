using UnityEngine;

namespace OSK
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class LocalizedAudio : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private bool updateOnStart = true;
        [SerializeField] private bool playOnUpdate = false;

        private AudioSource audioSource;
        private SystemLanguage lastLanguage;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (updateOnStart) UpdateLocalized();
            Main.Observer.Add(KEY_OBSERVER.KEY_UPDATE_LANGUAGE, OnLanguageUpdate);
        }

        private void OnDestroy()
        {
            Main.Observer.Remove(KEY_OBSERVER.KEY_UPDATE_LANGUAGE, OnLanguageUpdate);
        }

        private void OnEnable()
        {
            if (Main.Localization == null) return;
            if (!Main.Localization.IsSetDefaultLanguage) return;
            if (lastLanguage == Main.Localization.GetCurrentLanguage) return;
            lastLanguage = Main.Localization.GetCurrentLanguage;
            UpdateLocalized();
        }

        private void OnLanguageUpdate(object data = null) => UpdateLocalized();

        public void UpdateLocalized()
        {
            if (string.IsNullOrEmpty(key)) return;
            if (Main.Localization == null) return;

            var clip = Main.Localization.GetAudioClip(key);
            if (clip == null) return;

            audioSource.clip = clip;
            if (playOnUpdate)
            {
                audioSource.Play();
            }
        }

        public void PlayOnce()
        {
            if (audioSource != null && audioSource.clip != null)
                audioSource.PlayOneShot(audioSource.clip);
        }

        public void SetKey(string newKey)
        {
            key = newKey;
            UpdateLocalized();
        }

        public string GetKey() => key;
    }
}