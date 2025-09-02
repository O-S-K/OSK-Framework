using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OSK
{
    public static class UIExtensions
    {
        public static void SetText(this Text text, string value)
        {
            text.text = value;
        }
        public static void SetText(this TMPro.TMP_Text text, string value)
        {
            text.text = value;
        }
        public static void BindButton(this Button button, Action action)
        {
            if(button != null)
                button.onClick.AddListener(() => action());
        } 
        public static void BindButton(this Button button, Text text, Action action)
        {
            if (button != null)
            {
                if(button.GetComponentInChildren<Text>() != null)
                    button.GetComponentInChildren<Text>().text = text.text;
                button.onClick.AddListener(() => action());
            }
        } 
        public static void BindButton(this Button button, TMPro.TMP_Text text, Action action)
        {
            if (button != null)
            {
                if(button.GetComponentInChildren<TMPro.TMP_Text>() != null)
                    button.GetComponentInChildren<TMPro.TMP_Text>().text = text.text;
                button.onClick.AddListener(() => action());
            }
        } 
        public static void BindEventTrigger(this EventTrigger trigger, EventTriggerType type,
            Action<BaseEventData> action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = type;
            entry.callback.AddListener((data) => action(data));
            trigger.triggers.Add(entry);
        }
    }
}