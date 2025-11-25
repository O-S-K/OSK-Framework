using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    [DisallowMultipleComponent]
    public class LocalizedSprite : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private bool updateOnStart = true;
        [SerializeField] private bool setImage = true; // if false, will try SpriteRenderer

        private Image uiImage;
        private SpriteRenderer spriteRenderer;
        private SystemLanguage lastLanguage;

        private void Awake()
        {
            uiImage = GetComponent<Image>();
            spriteRenderer = GetComponent<SpriteRenderer>();
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

            var sprite = Main.Localization.GetSprite(key);
            if (sprite == null) return;

            if (setImage && uiImage != null)
            {
                uiImage.sprite = sprite;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
            else if (uiImage != null) // fallback
            {
                uiImage.sprite = sprite;
            }
        }

        public void SetKey(string newKey)
        {
            key = newKey;
            UpdateLocalized();
        }

        public string GetKey() => key;
    }
}