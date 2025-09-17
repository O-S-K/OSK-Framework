using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    public partial class UIManager
    {
        public void SetCanvas(int sortOrder = 0, string sortingLayerName = "Default",
            RenderMode renderMode = RenderMode.ScreenSpaceOverlay, bool pixelPerfect = false,
            UnityEngine.Camera camera = null)
        {
            RootUI.Canvas.renderMode = renderMode;
            RootUI.Canvas.sortingOrder = sortOrder;
            RootUI.Canvas.sortingLayerName = sortingLayerName;
            RootUI.Canvas.pixelPerfect = pixelPerfect;
            RootUI.Canvas.worldCamera = camera;
        }

        public void SetupCanvasScaleForRatio()
        { 
            if (RootUI?.CanvasScaler == null)
            {
                OSKLogger.LogWarning("UI","CanvasScaler  is not set up in the RootUI. Please ensure it is assigned.");
                return;
            }

            float ratio = (float)Screen.width / Screen.height;
            if (IsIpad())
            {
                // For iPad, use MatchWidthOrHeight = 0 to maintain aspect ratio
                RootUI.CanvasScaler.matchWidthOrHeight = 0f;
            }
            else
            {
                // For other devices, use MatchWidthOrHeight = 1 if the aspect ratio is wider than 0.65f
                RootUI.CanvasScaler.matchWidthOrHeight = ratio > 0.65f ? 1 : 0;
            }
            
            string log = Mathf.Approximately(RootUI.CanvasScaler.matchWidthOrHeight, 1f) ? "1 (Match Width)" : "0 (Match Height)";
            OSKLogger.Log("UI",$"Ratio: {ratio}. IsPad {IsIpad()} matchWidthOrHeight: {log}");
        }
         
        
        public  bool IsIpad()
        {
#if (UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
            if (UnityEngine.iOS.Device.generation.ToString().Contains("iPad"))
                return true;
#endif

            float w = Screen.width;
            float h = Screen.height;

            // Normalize to portrait
            if (w > h) (w, h) = (h, w);

            // Aspect ratio check (iPad thường ~ 4:3 → ~1.33)
            return (h / w) < 1.65f;
        }

        public void SetCanvasScaler(
            CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize,
            float scaleFactor = 1f,
            float referencePixelsPerUnit = 100f)
        {
            RootUI.CanvasScaler.uiScaleMode = scaleMode;
            RootUI.CanvasScaler.scaleFactor = scaleFactor;
            RootUI.CanvasScaler.referencePixelsPerUnit = referencePixelsPerUnit;
        }

        public void SetCanvasScaler(
            CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize,
            Vector2? referenceResolution = null,
            CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,
            float matchWidthOrHeight = 0f,
            float referencePixelsPerUnit = 100f)
        {
            RootUI.CanvasScaler.uiScaleMode = scaleMode;
            RootUI.CanvasScaler.referenceResolution = referenceResolution ?? new Vector2(1920, 1080);
            RootUI.CanvasScaler.screenMatchMode = screenMatchMode;
            RootUI.CanvasScaler.matchWidthOrHeight = matchWidthOrHeight;
            RootUI.CanvasScaler.referencePixelsPerUnit = referencePixelsPerUnit;
        }

        public void SetCanvasScaler(
            CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize,
            Vector2? referenceResolution = null,
            CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight,
            bool autoMatchForRatio = true,
            float referencePixelsPerUnit = 100f)
        {
            float newRatio = (float)Screen.width / Screen.height;
            SetCanvasScaler(scaleMode, referenceResolution, screenMatchMode, newRatio > 0.65f ? 1 : 0,
                referencePixelsPerUnit);
        }

        public void ShowRayCast()
        {
            var graphicRayCaster = Canvas.GetComponent<GraphicRaycaster>();
            if (graphicRayCaster != null)
                graphicRayCaster.ignoreReversedGraphics = true;
        }

        public void HideRayCast()
        {
            var graphicRayCaster = Canvas.GetComponent<GraphicRaycaster>();
            if (graphicRayCaster != null)
                graphicRayCaster.ignoreReversedGraphics = false;
        }
    }
}