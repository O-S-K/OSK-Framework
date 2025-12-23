using UnityEngine;

namespace OSK
{
    public static class InputAPI
    {
        #region Set & Get 
        public static void Set(string id, bool state) =>  InputInjector.Inject(id, state);
        public static void Set(string id, float value) => Get(id).Axis = value;
        public static void Set(string id, Vector2 value) => Get(id).Axis2D = value;
        public static InputActionRuntime Get(string id) => Main.InputDevice.Get(id);
        #endregion

        #region Buffering 
        public static bool DownBuffered(string id, float time) => Get(id)?.DownBuffered(time) ?? false;
        public static void RemoveBuffer(string id) => Get(id)?.RemoveBuffer();
        #endregion


        #region State Check
        public static bool Down(string id) => Get(id)?.IsDown ?? false;
        public static bool Hold(string id) => Get(id)?.IsHold ?? false;
        public static bool Up(string id) => Get(id)?.IsUp ?? false;
        public static bool DoubleTap(string id) => Get(id)?.IsDoubleTap ?? false;
        #endregion

        #region Get Values
        public static Vector2 Axis(string id) => Get(id)?.Axis2D ?? Vector2.zero;
        public static Vector2 ScreenPos(string id) => Get(id)?.ScreenPosition ?? Vector2.zero; 
        public static Vector3 WorldPos(string id) => Get(id)?.WorldPosition ?? Vector3.zero;
        public static Vector2 Delta(string id) => Get(id)?.DeltaPosition ?? Vector2.zero;
        public static int Touches(string id) => Get(id)?.TouchCount ?? 0;
        #endregion
    }
}