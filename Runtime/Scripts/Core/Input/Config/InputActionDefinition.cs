using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public enum InputActionType { Button, Axis, Axis2D, Vector3, Rotation }
    public enum MouseButtonID
    {
        Left = 0,
        Right = 1,
        Middle = 2,
        None = -1
    }
    public enum MouseScrollDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
    
    [System.Serializable]
    public class ButtonBinding
    {
        [HorizontalGroup("BindingRow")]
        [VerticalGroup("BindingRow/Left")]
        [LabelText("Keyboard")]
        public KeyCode key = KeyCode.None;

        [VerticalGroup("BindingRow/Right")]
        [LabelText("Mouse")]
        public MouseButtonID mouseButton = MouseButtonID.None;
        
        public void SetKey(KeyCode newKey) => key = newKey;
        public void SetMouseButton(MouseButtonID newMouseButton) => mouseButton = newMouseButton;

        public bool Read()
        {
            if (key != KeyCode.None && Input.GetKey(key)) return true;
            if (mouseButton >= 0 && Input.GetMouseButton((int)mouseButton)) return true;
            return false;
        }
    }
    
    [System.Serializable]
    public class AxisBinding
    {
        public string axis;

        public void SetAxis(string newAxis) => axis = newAxis;

        public float Read()
        {
            return string.IsNullOrEmpty(axis) ? 0f : Input.GetAxisRaw(axis);
        }
    }
    
    [System.Serializable]
    public class Axis2DBinding
    {
        public string axisX;
        public string axisY;
        
        public void SetAxisX(string newAxisX) => axisX = newAxisX;
        public void SetAxisY(string newAxisY) => axisY = newAxisY;

        public Vector2 Read()
        {
            Vector2 v = Vector2.zero;

            if (!string.IsNullOrEmpty(axisX))
                v.x = Input.GetAxisRaw(axisX);

            if (!string.IsNullOrEmpty(axisY))
                v.y = Input.GetAxisRaw(axisY);

            return v;
        }
    }
    
    [System.Serializable] 
    [HideReferenceObjectPicker]
    public class InputActionDefinition
    {
        [TitleGroup("$id", "Action Configuration", alignment: TitleAlignments.Left)]
        [HorizontalGroup("$id/Header")]
        [BoxGroup("$id/Header/L", LabelText = "ID")] [HideLabel] public string id;
        [BoxGroup("$id/Header/R", LabelText = "Type")] [HideLabel] public InputActionType actionType;

        [TabGroup("$id/Tabs", "Settings", Icon = SdfIconType.Keyboard)]
        [ShowIf(nameof(IsButton))] public List<ButtonBinding> buttonBindings = new();
        [ShowIf(nameof(IsAxis))] public AxisBinding axisBinding;
        [ShowIf(nameof(IsAxis2D))] public Axis2DBinding axis2DBinding;

        private bool IsButton() => actionType == InputActionType.Button;
        private bool IsAxis() => actionType == InputActionType.Axis;
        private bool IsAxis2D() => actionType == InputActionType.Axis2D;
        private void AddButtonBinding() => buttonBindings.Add(new ButtonBinding());
    }
}
