using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    public class InputDeviceManager : GameFrameworkComponent
    {
        public static Action<string> OnActionDown;
        public static Action<string> OnActionUp;

        [SerializeField, ReadOnly]
        private InputConfigSO config;
        [SerializeField, ReadOnly]
        private string configPath = "InputConfigs/InputConfigSO";

        private readonly Dictionary<string, InputActionRuntime> runtime = new();
        private bool isInit = false;

        public override void OnInit()
        {
            config = config != null ? config : Resources.Load<InputConfigSO>(configPath);
            if(config == null)
            {
                MyLogger.LogError($"No input config found at path: {configPath}");
                return;
            }
            Input.multiTouchEnabled = config.enableMultiTouch;
            foreach (var def in config.Actions)
                if (!string.IsNullOrEmpty(def.id))
                    runtime[def.id] = new InputActionRuntime(def.id);
            
            isInit = true;
        }

        private void Update()
        {
            if (!isInit) return;
            
            foreach (var pair in runtime) if (InputContextManager.Allow(pair.Key)) pair.Value.ResetFrame();
            foreach (var def in config.Actions)
            {
                if (string.IsNullOrEmpty(def.id) || !runtime.ContainsKey(def.id)) continue;
                if (!InputContextManager.Allow(def.id))
                {
                    runtime[def.id].ForceCancel();
                    continue;
                }
                UpdateAction(def, runtime[def.id]);
            }
        }

        private void UpdateAction(InputActionDefinition def, InputActionRuntime rt)
        {
            bool pressed = InputInjector.Get(def.id);
            foreach (var b in def.buttonBindings) pressed |= b.Read();
            if (def.actionType == InputActionType.Axis)
            {
                rt.Axis = def.axisBinding.Read();
            }

            if (def.actionType == InputActionType.Axis2D)
            {
                rt.Axis2D = def.axis2DBinding.Read();
            }

            if (pressed)
            {
                if (!rt.IsHoldPhysical)
                {
                    rt.IsDown = true;
                    rt.PressTime = Time.time;
                    rt.RegisterPress();
                    OnActionDown?.Invoke(def.id);
                }
                else
                {
                    rt.IsHold = true;
                    OnActionUp?.Invoke(def.id);
                }

                rt.IsHoldPhysical = true;
            }
            else
            {
                if (rt.IsHoldPhysical) rt.IsUp = true;
                rt.IsHold = rt.IsHoldPhysical = false;
            }

            rt.ScreenPosition = Input.mousePosition;
            rt.ScrollDelta = Input.mouseScrollDelta;
            if (rt.ScrollDelta.y > 0) rt.ScrollDir = MouseScrollDirection.Up;
            else if (rt.ScrollDelta.y < 0) rt.ScrollDir = MouseScrollDirection.Down;

            var cam = config.cameraDetection ? config.cameraDetection : Camera.main;
            if (cam != null)
            {
                Vector3 mPos = Input.mousePosition;
                mPos.z = 10f;
                rt.WorldPosition = cam.ScreenToWorldPoint(mPos);
            }

            rt.TouchCount = Input.touchCount;
            rt.Acceleration = Input.acceleration;
            if (SystemInfo.supportsGyroscope)
            {
                rt.Rotation = Input.gyro.attitude;
                rt.Gravity = Input.gyro.gravity;
            }
        }

        public  InputActionRuntime Get(string id) => runtime.TryGetValue(id, out var rt) ? rt : null;
    }
}