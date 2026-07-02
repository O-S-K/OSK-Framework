using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OSK
{
    public static class UIExtensions
    {
        #region Example Usage

        // Text:
        // titleText.SetText("Ready");
        // tmpTitle.SetText("Ready");
        //
        // Button:
        // playButton.BindButton(OnPlayClicked);
        // closeButton.RebindButton(Hide);
        // buyButton.SetInteractable(canBuy);
        //
        // Toggle / Slider / Dropdown:
        // musicToggle.SetValue(false, notify: false);
        // volumeSlider.SetValue(0.75f, notify: true);
        // qualityDropdown.SetValue(2, notify: false);
        //
        // Event trigger:
        // trigger.BindEventTrigger(EventTriggerType.PointerEnter, OnPointerEnter);
        // trigger.ClearEventTriggers();
        //
        // Canvas group:
        // panelGroup.SetVisible(true);
        // panelGroup.SetInteractable(false);

        #endregion

        #region Text

        // Sets legacy UI Text value.
        public static void SetText(this Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        // Sets TextMeshPro text value.
        public static void SetText(this TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        // Sets legacy UI Text value with string.Format.
        public static void SetTextFormat(this Text text, string format, params object[] args)
        {
            if (text == null)
            {
                return;
            }

            text.text = Format(format, args);
        }

        // Sets TextMeshPro text value with string.Format.
        public static void SetTextFormat(this TMP_Text text, string format, params object[] args)
        {
            if (text == null)
            {
                return;
            }

            text.text = Format(format, args);
        }

        #endregion

        #region Button

        // Adds an onClick listener to a Button.
        public static void BindButton(this Button button, Action action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.AddListener(() => action());
        }

        // Copies legacy UI Text to the Button child text and adds an onClick listener.
        public static void BindButton(this Button button, Text text, Action action)
        {
            if (button == null)
            {
                return;
            }

            if (text != null)
            {
                Text childText = button.GetComponentInChildren<Text>();
                if (childText != null)
                {
                    childText.text = text.text;
                }
            }

            button.BindButton(action);
        }

        // Copies TextMeshPro text to the Button child text and adds an onClick listener.
        public static void BindButton(this Button button, TMP_Text text, Action action)
        {
            if (button == null)
            {
                return;
            }

            if (text != null)
            {
                TMP_Text childText = button.GetComponentInChildren<TMP_Text>();
                if (childText != null)
                {
                    childText.text = text.text;
                }
            }

            button.BindButton(action);
        }

        // Removes all onClick listeners and adds a new listener.
        public static void RebindButton(this Button button, Action action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.BindButton(action);
        }

        // Removes all onClick listeners from a Button.
        public static void ClearButton(this Button button)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        // Sets a Button interactable state.
        public static void SetInteractable(this Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        #endregion

        #region Selectable

        // Sets a Selectable interactable state.
        public static void SetInteractable(this Selectable selectable, bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }

        // Sets a Toggle value with optional notification.
        public static void SetValue(this Toggle toggle, bool value, bool notify = true)
        {
            if (toggle == null)
            {
                return;
            }

            if (notify)
            {
                toggle.isOn = value;
            }
            else
            {
                toggle.SetIsOnWithoutNotify(value);
            }
        }

        // Sets a Slider value with optional notification.
        public static void SetValue(this Slider slider, float value, bool notify = true)
        {
            if (slider == null)
            {
                return;
            }

            if (notify)
            {
                slider.value = value;
            }
            else
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        // Sets a Scrollbar value with optional notification.
        public static void SetValue(this Scrollbar scrollbar, float value, bool notify = true)
        {
            if (scrollbar == null)
            {
                return;
            }

            if (notify)
            {
                scrollbar.value = value;
            }
            else
            {
                scrollbar.SetValueWithoutNotify(value);
            }
        }

        // Sets a Dropdown value with optional notification.
        public static void SetValue(this Dropdown dropdown, int value, bool notify = true)
        {
            if (dropdown == null)
            {
                return;
            }

            if (notify)
            {
                dropdown.value = value;
            }
            else
            {
                dropdown.SetValueWithoutNotify(value);
            }
        }

        // Sets a TMP_Dropdown value with optional notification.
        public static void SetValue(this TMP_Dropdown dropdown, int value, bool notify = true)
        {
            if (dropdown == null)
            {
                return;
            }

            if (notify)
            {
                dropdown.value = value;
            }
            else
            {
                dropdown.SetValueWithoutNotify(value);
            }
        }

        // Sets an InputField value with optional notification.
        public static void SetText(this InputField inputField, string value, bool notify = true)
        {
            if (inputField == null)
            {
                return;
            }

            if (notify)
            {
                inputField.text = value ?? string.Empty;
            }
            else
            {
                inputField.SetTextWithoutNotify(value ?? string.Empty);
            }
        }

        // Sets a TMP_InputField value with optional notification.
        public static void SetText(this TMP_InputField inputField, string value, bool notify = true)
        {
            if (inputField == null)
            {
                return;
            }

            if (notify)
            {
                inputField.text = value ?? string.Empty;
            }
            else
            {
                inputField.SetTextWithoutNotify(value ?? string.Empty);
            }
        }

        #endregion

        #region Event Trigger

        // Adds an EventTrigger listener.
        public static void BindEventTrigger(this EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action)
        {
            if (trigger == null || action == null)
            {
                return;
            }

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = type;
            entry.callback.AddListener(data => action(data));
            if (trigger.triggers == null)
            {
                trigger.triggers = new List<EventTrigger.Entry>();
            }

            trigger.triggers.Add(entry);
        }

        // Removes all EventTrigger entries.
        public static void ClearEventTriggers(this EventTrigger trigger)
        {
            if (trigger != null && trigger.triggers != null)
            {
                trigger.triggers.Clear();
            }
        }

        #endregion

        #region Canvas Group

        // Sets CanvasGroup visible state by alpha.
        public static void SetVisible(this CanvasGroup canvasGroup, bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }
        }

        // Sets CanvasGroup interactable and blocksRaycasts state.
        public static void SetInteractable(this CanvasGroup canvasGroup, bool interactable)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        // Sets CanvasGroup alpha, interactable, and blocksRaycasts together.
        public static void SetState(this CanvasGroup canvasGroup, bool visible, bool interactable)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        #endregion

        #region GameObject

        // Sets GameObject active state when it is not null.
        public static void SetActiveSafe(this GameObject gameObject, bool active)
        {
            if (gameObject != null)
            {
                gameObject.SetActive(active);
            }
        }

        // Sets Component GameObject active state when it is not null.
        public static void SetActiveSafe(this Component component, bool active)
        {
            if (component != null)
            {
                component.gameObject.SetActive(active);
            }
        }

        #endregion

        #region Internals

        // Formats text safely.
        private static string Format(string format, object[] args)
        {
            if (string.IsNullOrEmpty(format))
            {
                return string.Empty;
            }

            return args == null || args.Length == 0 ? format : string.Format(format, args);
        }

        #endregion
    }
}
