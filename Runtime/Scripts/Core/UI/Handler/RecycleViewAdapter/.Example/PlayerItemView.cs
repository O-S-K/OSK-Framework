using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


namespace OSK.Bindings.Example
{
    [System.Serializable]
    public struct PlayerDataa
    {
        public string Name;
        public int Score;
        public Sprite Avatar;
    }

    public class PlayerItemView : MonoBehaviour, IRecyclerItem<PlayerDataa>, IPointerClickHandler
    {
        public TMP_Text NameText;
        public TMP_Text ScoreText;
        public Image Avatar;
        public Image Background;

        int _index;
        PlayerDataa _model;
        public Color NormalColor = Color.white;
        public Color SelectedColor = new Color(0.8f, 0.9f, 1f);

        public void SetData(PlayerDataa model, int index)
        {
            _model = model;
            _index = index;
            if (NameText != null) NameText.text = model.Name;
            if (ScoreText != null) ScoreText.text = model.Score.ToString();
            gameObject.name = $"PlayerItem [{index}] {model.Name}";
            if (Background != null) Background.color = NormalColor;
        }

        public void Clear()
        {
            if (NameText != null) NameText.text = "";
            if (ScoreText != null) ScoreText.text = "";
            _model = default;
        }

        public void Bind()
        {
        }

        public void Unbind()
        {
        }

        public void OnPointerClick(PointerEventData eventData)
        {
        }

        // helper to unselect
        public void SetSelected(bool sel)
        {
            if (Background != null) Background.color = sel ? SelectedColor : NormalColor;
        }
    }

}