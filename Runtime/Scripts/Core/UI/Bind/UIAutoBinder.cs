using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace OSK
{
    public static class UIAutoBinder
    {
        public static void Bind(object view, object viewModel)
        {
            if (view == null || viewModel == null) return;

            var viewFields = view.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => Attribute.IsDefined(f, typeof(BindToAttribute)));

            foreach (var viewField in viewFields)
            {
                BindField(view, viewField, viewModel);
            }
        }

        private static void BindField(object view, FieldInfo viewField, object viewModel)
        {
            var attr = viewField.GetCustomAttribute<BindToAttribute>();
            var propertyName = attr.PropertyName;
            var format = attr.Format;
            var targetName = attr.Target;

            var vmField = viewModel.GetType().GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (vmField == null)
            {
                Debug.LogError($"[UIAutoBinder] Không tìm thấy field '{propertyName}' trong {viewModel.GetType().Name}");
                return;
            }

            var observable = vmField.GetValue(viewModel);
            if (observable == null)
            {
                Debug.LogError($"[UIAutoBinder] Observable '{propertyName}' null trong ViewModel");
                return;
            }

            var valueProp = observable.GetType().GetProperty("Value");
            var onChangedEvent = observable.GetType().GetEvent("OnValueChanged");

            var mainComponent = viewField.GetValue(view);
            if (mainComponent == null) return;

            object targetComponent = null;
            if (!string.IsNullOrEmpty(targetName))
            {
                var targetField = view.GetType().GetField(targetName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (targetField != null)
                {
                    targetComponent = targetField.GetValue(view);
                    var initValue = valueProp.GetValue(observable);
                    SetFormattedValue(initValue, targetComponent);
                }
            }

            void SetFormattedValue(object val, object comp)
            {
                if (val == null || comp == null) return;
                string str = string.IsNullOrEmpty(format) ? val.ToString() : string.Format(format, val);

                if (comp is TMP_Text tmp) tmp.text = str;
                else if (comp is Text txt) txt.text = str;
            }

            // --- Bind theo component ---
            switch (mainComponent)
            {
                case TMP_Text tmpText:
                    tmpText.text = string.Format(format ?? "{0}", valueProp.GetValue(observable));
                    AddObserver(observable, onChangedEvent, v => tmpText.text = string.Format(format ?? "{0}", v));
                    break;

                case TMP_InputField input:
                    input.text = string.Format(format ?? "{0}", valueProp.GetValue(observable));
                    AddObserver(observable, onChangedEvent, v => input.text = string.Format(format ?? "{0}", v));
                    input.onSubmit.AddListener(newVal =>
                    {
                        valueProp.SetValue(observable, Convert.ChangeType(newVal, valueProp.PropertyType));
                        SetFormattedValue(newVal, targetComponent);
                    });
                     
                    break;

                case Slider slider:
                    slider.value = Convert.ToSingle(valueProp.GetValue(observable));
                    AddObserver(observable, onChangedEvent, v =>
                    {
                        slider.value = Convert.ToSingle(v);
                        SetFormattedValue(v, targetComponent);
                    });
                    slider.onValueChanged.AddListener(newVal =>
                    {
                        valueProp.SetValue(observable, Convert.ChangeType(newVal, valueProp.PropertyType));
                        SetFormattedValue(newVal, targetComponent);
                    });
                    break;

                case Toggle toggle:
                    toggle.isOn = Convert.ToBoolean(valueProp.GetValue(observable));
                    AddObserver(observable, onChangedEvent, v =>
                    {
                        toggle.isOn = Convert.ToBoolean(v);
                        SetFormattedValue(v, targetComponent);
                    });
                    toggle.onValueChanged.AddListener(newVal =>
                    {
                        valueProp.SetValue(observable, Convert.ChangeType(newVal, valueProp.PropertyType));
                        SetFormattedValue(newVal, targetComponent);
                    });
                    break;

                case TMP_Dropdown dropdown:
                    dropdown.value = Convert.ToInt32(valueProp.GetValue(observable));
                    AddObserver(observable, onChangedEvent, v => dropdown.value = Convert.ToInt32(v));
                    dropdown.onValueChanged.AddListener(newVal =>
                        valueProp.SetValue(observable, Convert.ChangeType(newVal, valueProp.PropertyType)));
                    break;

                case Scrollbar scrollbar:
                    scrollbar.value = Convert.ToSingle(valueProp.GetValue(observable));
                    AddObserver(observable, onChangedEvent, v => scrollbar.value = Convert.ToSingle(v));
                    scrollbar.onValueChanged.AddListener(newVal =>
                        valueProp.SetValue(observable, Convert.ChangeType(newVal, valueProp.PropertyType)));
                    break;

                case ScrollRect scrollRect:
                    scrollRect.verticalNormalizedPosition = Convert.ToSingle(valueProp.GetValue(observable));
                    AddObserver(observable, onChangedEvent, v => scrollRect.verticalNormalizedPosition = Convert.ToSingle(v));
                    scrollRect.onValueChanged.AddListener(newVal =>
                        valueProp.SetValue(observable, Convert.ChangeType(newVal.y, valueProp.PropertyType)));
                    break;

                case Image image:
                    image.sprite = valueProp.GetValue(observable) as Sprite;
                    AddObserver(observable, onChangedEvent, v => image.sprite = v as Sprite);
                    break;

                case RawImage rawImage:
                    rawImage.texture = valueProp.GetValue(observable) as Texture;
                    AddObserver(observable, onChangedEvent, v => rawImage.texture = v as Texture);
                    break;

                default:
                    Debug.LogWarning($"[UIAutoBinder] Component {mainComponent.GetType().Name} chưa được hỗ trợ");
                    break;
            }
        }

        private static void AddObserver(object observableObj, EventInfo eventInfo, Action<object> action)
        {
            if (eventInfo == null) return;
            var handlerType = eventInfo.EventHandlerType;
            var invokeMethod = handlerType.GetMethod("Invoke");
            var paramType = invokeMethod.GetParameters()[0].ParameterType;

            var handler = Delegate.CreateDelegate(handlerType, action.Target, action.Method);
            eventInfo.AddEventHandler(observableObj, handler);
        }
    }
}