using UnityEngine;

namespace OSK
{
    public class InputActionRuntime
    {
        public readonly string Id;

        // --- DIGITAL STATES ---
        public bool IsDown { get; set; }
        public bool IsHold { get; set; }
        public bool IsUp { get; set; }
        public bool IsTap { get; set; }
        public bool IsDoubleTap { get; set; }
        public int ClickCount { get; set; }

        // --- ANALOG & MOTION DATA ---
        public float Axis { get; set; }
        public Vector2 Axis2D { get; set; }
        public Vector3 ScreenPosition { get; set; }
        public Vector2 DeltaPosition { get;  set; }
        public Vector3 WorldPosition { get; set; }
        public Vector3 Acceleration { get; set; }
        public Vector3 Gravity { get; set; }
        public Quaternion Rotation { get; set; }
        public MouseScrollDirection ScrollDir { get; set; }
        public Vector2 ScrollDelta { get; set; }
        
        // --- MOBILE & MULTI-TOUCH ---
        public int TouchCount { get; set; }
        public bool IsMultiTouch => TouchCount > 1;
        public TouchPhase TouchPhase { get; set; }

        // --- INTERNAL LOGIC ---
        internal float PressTime, LastPressTime = -100f;
        internal bool IsHoldPhysical, IsConsumed;
        private const float MultiTapWindow = 0.3f;
        public bool IsLongPress { get; internal set; } 
        public float HoldDuration => IsHoldPhysical ? (Time.time - PressTime) : 0f; 

        public InputActionRuntime(string id) => Id = id;

        internal void ResetFrame()
        {
            IsDown = IsUp = IsTap = IsDoubleTap = false;
            DeltaPosition = Vector2.zero;
            ScrollDir = MouseScrollDirection.None;
        }

        public void ForceCancel()
        {
            IsDown = IsHold = IsHoldPhysical = IsUp = IsConsumed = IsDoubleTap = false;
            ClickCount = 0;
            Axis = 0;
            Axis2D = ScreenPosition = DeltaPosition = WorldPosition = Vector3.zero;
        }

        public bool DownBuffered(float time) => !IsConsumed && (Time.time - LastPressTime <= time);
        public void RemoveBuffer() => IsConsumed = true;

        internal void RegisterPress()
        {
            if (Time.time - LastPressTime <= MultiTapWindow)
            {
                ClickCount++;
                IsDoubleTap = (ClickCount == 2);
            }
            else
            {
                ClickCount = 1;
                IsDoubleTap = false;
            }

            LastPressTime = Time.time;
            IsConsumed = false;
        }
    }
}