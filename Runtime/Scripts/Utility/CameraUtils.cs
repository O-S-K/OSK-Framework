using UnityEngine;

namespace OSK
{
    public class CameraUtils : MonoBehaviour
    {
        #region Example Usage

        // Orthographic camera size:
        // Vector2 size = CameraUtils.WorldSize(Camera.main);
        // Rect visibleRect = CameraUtils.OrthographicVisibleRect(Camera.main);
        //
        // Main camera access:
        // if (CameraUtils.TryGetMainCamera(out Camera cam)) { Debug.Log(cam.name); }
        //
        // Screen/world conversion:
        // Vector3 world = CameraUtils.ScreenToWorldPointOnZ(Camera.main, Input.mousePosition, 0f);
        // Vector3 screen = CameraUtils.WorldToScreenPoint(Camera.main, transform.position);
        //
        // Visibility and focus:
        // bool visible = CameraUtils.IsPointVisible(Camera.main, transform.position);
        // CameraUtils.FocusOn2DWorld(Camera.main, player);

        #endregion

        #region Constants

        private const float Bounds2DDepth = 100000f;

        #endregion

        #region Screen Scale

        // Gets screen width scale against a 1080 reference width.
        public static float XScale => (float)Screen.width / 1080f;

        // Gets screen height scale against a 1920 reference height.
        public static float YScale => (float)Screen.height / 1920f;

        // Gets screen scale using the smaller axis for safe uniform scaling.
        public static float SafeScale => Mathf.Min(XScale, YScale);

        #endregion

        #region Orthographic Size

        // Gets the visible world width of an orthographic camera.
        public static float WorldWidth(Camera camera)
        {
            if (camera == null)
            {
                return 0f;
            }

            return 2f * camera.orthographicSize * camera.aspect;
        }

        // Gets the visible world height of an orthographic camera.
        public static float WorldHeight(Camera camera)
        {
            if (camera == null)
            {
                return 0f;
            }

            return 2f * camera.orthographicSize;
        }

        // Gets visible orthographic camera size as world width and height.
        public static Vector2 WorldSize(Camera camera)
        {
            return new Vector2(WorldWidth(camera), WorldHeight(camera));
        }

        // Gets an orthographic camera visible rect in world space.
        public static Rect OrthographicVisibleRect(Camera self)
        {
            if (self == null)
            {
                return Rect.zero;
            }

            Vector2 min = (Vector2)self.transform.position - new Vector2(self.aspect * self.orthographicSize, self.orthographicSize);
            Vector2 size = new Vector2(self.aspect * self.orthographicSize * 2.0f, self.orthographicSize * 2.0f);
            return new Rect(min, size);
        }

        // Gets an orthographic camera visible bounds in world space.
        public static Bounds OrthographicVisibleBounds(Camera camera)
        {
            if (camera == null)
            {
                return new Bounds();
            }

            Vector2 worldSize = WorldSize(camera);
            Vector3 size = new Vector3(worldSize.x, worldSize.y, Bounds2DDepth);
            Bounds bounds = new Bounds(camera.transform.position, size);
            bounds.center = new Vector3(bounds.center.x, bounds.center.y, 0f);
            return bounds;
        }

        #endregion

        #region Stored Camera Size

        private static Vector2 sizeCamera;

        public enum LimitType
        {
            MaxLimitX,
            MinLimitX,
            MaxLimitY,
            MinLimitY
        }

        // Stores custom camera size used by legacy bounds helpers.
        public static void SetSize(Vector2 size)
        {
            sizeCamera = size;
        }

        // Gets custom camera size used by legacy bounds helpers.
        public static Vector2 GetSize()
        {
            return sizeCamera;
        }

        // Gets one limit from custom camera size and camera position.
        public static float GetLimitCamera(Vector2 posCamera, LimitType limitType)
        {
            switch (limitType)
            {
                case LimitType.MaxLimitX:
                    return posCamera.x + sizeCamera.x / 2f;
                case LimitType.MinLimitX:
                    return posCamera.x - sizeCamera.x / 2f;
                case LimitType.MaxLimitY:
                    return posCamera.y + sizeCamera.y / 2f;
                case LimitType.MinLimitY:
                    return posCamera.y - sizeCamera.y / 2f;
                default:
                    return 0f;
            }
        }

        // Gets custom camera bounds from stored camera size.
        public static Bounds GetBounds(Vector2 posCamera)
        {
            return new Bounds(new Vector3(posCamera.x, posCamera.y, 0f), new Vector3(sizeCamera.x, sizeCamera.y, Bounds2DDepth));
        }

        #endregion

        #region Camera Access

        // Gets Camera.main and returns false when there is no main camera.
        public static bool TryGetMainCamera(out Camera camera)
        {
            camera = Camera.main;
            return camera != null;
        }

        // Gets Camera.main or logs a warning when missing.
        public static Camera GetMainCamera(bool logWarning = true)
        {
            Camera camera = Camera.main;
            if (camera == null && logWarning)
            {
                Debug.LogWarning("CameraUtils.GetMainCamera failed: no camera tagged MainCamera.");
            }

            return camera;
        }

        #endregion

        #region Screen And World Conversion

        // Converts a GUI position to a world position at the camera ray origin.
        public static Vector3 GUIPositionToWorldPosition(Camera self, Vector2 guiPosition) =>
            self != null ? self.ScreenPointToRay(guiPosition).GetPoint(0.0f) : Vector3.zero;

        // Converts a GUI delta to a world delta using the camera ray origin.
        public static Vector3 GUIDeltaToWorldDelta(Camera self, Vector2 guiDelta)
        {
            if (self == null)
            {
                return Vector3.zero;
            }

            Vector3 screenDelta = GUIUtility.GUIToScreenPoint(guiDelta);
            Ray worldRay = self.ScreenPointToRay(screenDelta);

            Vector3 worldDelta = worldRay.GetPoint(0.0f);
            worldDelta -= self.ScreenPointToRay(Vector3.zero).GetPoint(0.0f);

            return worldDelta;
        }

        // Converts a screen point to world space at a specific distance from the camera.
        public static Vector3 ScreenToWorldPoint(Camera camera, Vector3 screenPoint, float distanceFromCamera)
        {
            if (camera == null)
            {
                return Vector3.zero;
            }

            screenPoint.z = distanceFromCamera;
            return camera.ScreenToWorldPoint(screenPoint);
        }

        // Converts a screen point to world space on a Z plane.
        public static Vector3 ScreenToWorldPointOnZ(Camera camera, Vector2 screenPoint, float worldZ = 0f)
        {
            if (camera == null)
            {
                return Vector3.zero;
            }

            Ray ray = camera.ScreenPointToRay(screenPoint);
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, worldZ));
            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        // Converts a world point to screen space.
        public static Vector3 WorldToScreenPoint(Camera camera, Vector3 worldPoint)
        {
            return camera != null ? camera.WorldToScreenPoint(worldPoint) : Vector3.zero;
        }

        // Converts a world point to viewport space.
        public static Vector3 WorldToViewportPoint(Camera camera, Vector3 worldPoint)
        {
            return camera != null ? camera.WorldToViewportPoint(worldPoint) : Vector3.zero;
        }

        #endregion

        #region Visibility

        // Checks if a renderer bounds intersects the camera frustum.
        public static bool IsObjectVisible(Camera self, Renderer renderer) =>
            self != null && renderer != null &&
            GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(self), renderer.bounds);

        // Checks if a world point is inside the camera viewport.
        public static bool IsPointVisible(Camera self, Vector3 point)
        {
            if (self == null)
            {
                return false;
            }

            Vector3 p = self.WorldToViewportPoint(point);
            return p.z >= 0.0f && p.x >= 0.0f && p.x <= 1.0f && p.y >= 0.0f && p.y <= 1.0f;
        }

        // Checks if a viewport point is inside the visible viewport.
        public static bool IsViewportPointVisible(Vector3 viewportPoint, bool requireInFront = true)
        {
            bool inViewport = viewportPoint.x >= 0.0f && viewportPoint.x <= 1.0f &&
                              viewportPoint.y >= 0.0f && viewportPoint.y <= 1.0f;
            return requireInFront ? inViewport && viewportPoint.z >= 0.0f : inViewport;
        }

        // Checks if a world position is inside stored camera bounds.
        public static bool IsObjectVisibleOnCamera(Vector2 posCamera, Vector3 pos)
        {
            return GetBounds(posCamera).Contains(pos);
        }

        // Checks if bounds intersect stored camera bounds.
        public static bool IsObjectVisibleOnCamera(Vector2 posCamera, Bounds other)
        {
            other.center = new Vector3(other.center.x, other.center.y, 0f);
            return GetBounds(posCamera).Intersects(other);
        }

        // Checks if a target transform position is inside the camera frustum.
        public static bool IsTargetVisible(Transform go, Camera camera)
        {
            if (go == null || camera == null)
            {
                return false;
            }

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            Vector3 point = go.position;
            for (int i = 0; i < planes.Length; i++)
            {
                if (planes[i].GetDistanceToPoint(point) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Movement And Bounds

        // Moves a camera to focus on a 2D target while keeping camera Z.
        public static void FocusOn2D(Camera camera, GameObject target)
        {
            if (camera == null || target == null)
            {
                return;
            }

            Vector3 t = target.transform.localPosition;
            camera.transform.position = new Vector3(t.x, t.y, camera.transform.position.z);
        }

        // Moves a camera to focus on a world-space 2D target while keeping camera Z.
        public static void FocusOn2DWorld(Camera camera, GameObject target)
        {
            if (camera == null || target == null)
            {
                return;
            }

            Vector3 t = target.transform.position;
            camera.transform.position = new Vector3(t.x, t.y, camera.transform.position.z);
        }

        // Moves a camera to focus on a 2D position while keeping camera Z.
        public static void FocusOn2D(Camera camera, Vector2 position)
        {
            if (camera == null)
            {
                return;
            }

            camera.transform.position = new Vector3(position.x, position.y, camera.transform.position.z);
        }

        // Clamps a 2D position inside bounds.
        public static Vector2 ClampPosition(Vector2 position, Bounds bounds)
        {
            return new Vector2(
                Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(position.y, bounds.min.y, bounds.max.y));
        }

        // Clamps a camera position so its orthographic view stays inside world bounds.
        public static Vector3 ClampOrthographicCameraPosition(Camera camera, Bounds worldBounds)
        {
            if (camera == null)
            {
                return Vector3.zero;
            }

            Vector2 halfSize = WorldSize(camera) * 0.5f;
            Vector3 position = camera.transform.position;
            float minX = worldBounds.min.x + halfSize.x;
            float maxX = worldBounds.max.x - halfSize.x;
            float minY = worldBounds.min.y + halfSize.y;
            float maxY = worldBounds.max.y - halfSize.y;

            if (minX > maxX)
            {
                position.x = worldBounds.center.x;
            }
            else
            {
                position.x = Mathf.Clamp(position.x, minX, maxX);
            }

            if (minY > maxY)
            {
                position.y = worldBounds.center.y;
            }
            else
            {
                position.y = Mathf.Clamp(position.y, minY, maxY);
            }

            return position;
        }

        #endregion
    }
}
