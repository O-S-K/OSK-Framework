using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public static class PhysicsUtils
    {
        #region Example Usage

        // Layer mask check:
        // if (!PhysicsUtils.IsInLayerMask(other.gameObject, damageMask)) { return; }
        //
        // Collider2D cast:
        // List<RaycastHit2D> hits = PhysicsUtils.CheckCollisions(myCollider, Vector2.right, 1f);
        // bool blocked = PhysicsUtils.HasCollision2D(myCollider, moveDirection, 0.2f, wallMask);
        //
        // Raycast:
        // if (PhysicsUtils.Raycast2D(transform, Vector2.down, 2f, groundMask, out RaycastHit2D hit2D)) { }
        // if (PhysicsUtils.ScreenRaycast3D(Camera.main, Input.mousePosition, 100f, interactMask, out RaycastHit hit3D)) { }
        //
        // Overlap:
        // Collider2D[] targets = PhysicsUtils.OverlapCircleAll2D(transform.position, 3f, enemyMask);

        #endregion

        #region Layer Mask

        // Checks if a GameObject layer is included in a LayerMask.
        public static bool IsInLayerMask(GameObject gameObject, LayerMask layerMask)
        {
            return gameObject != null && IsLayerInMask(gameObject.layer, layerMask);
        }

        // Checks if a Component GameObject layer is included in a LayerMask.
        public static bool IsInLayerMask(Component component, LayerMask layerMask)
        {
            return component != null && IsInLayerMask(component.gameObject, layerMask);
        }

        // Checks if a layer index is included in a LayerMask.
        public static bool IsLayerInMask(int layer, LayerMask layerMask)
        {
            return layer >= 0 && layer <= 31 && (layerMask.value & (1 << layer)) != 0;
        }

        // Adds a layer index to a LayerMask.
        public static LayerMask AddLayerToMask(LayerMask layerMask, int layer)
        {
            if (layer < 0 || layer > 31)
            {
                return layerMask;
            }

            layerMask.value |= 1 << layer;
            return layerMask;
        }

        // Removes a layer index from a LayerMask.
        public static LayerMask RemoveLayerFromMask(LayerMask layerMask, int layer)
        {
            if (layer < 0 || layer > 31)
            {
                return layerMask;
            }

            layerMask.value &= ~(1 << layer);
            return layerMask;
        }

        #endregion

        #region Contact Filters

        // Creates a 2D contact filter from layer mask and trigger settings.
        public static ContactFilter2D CreateContactFilter2D(LayerMask layerMask, bool useTriggers = false)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = useTriggers;
            filter.SetLayerMask(layerMask);
            filter.useLayerMask = true;
            return filter;
        }

        // Creates a 2D contact filter using the layer collision matrix for a collider layer.
        public static ContactFilter2D CreateCollisionFilter2D(Collider2D collider, bool useTriggers = false)
        {
            if (collider == null)
            {
                return CreateContactFilter2D(Physics2D.AllLayers, useTriggers);
            }

            return CreateContactFilter2D(Physics2D.GetLayerCollisionMask(collider.gameObject.layer), useTriggers);
        }

        #endregion

        #region Cast 2D

        // Casts a Collider2D and returns all hits using the collider layer collision matrix.
        public static List<RaycastHit2D> CheckCollisions(Collider2D collider, Vector2 direction, float distance)
        {
            return CastCollider2D(collider, direction, distance);
        }

        // Casts a Collider2D and returns all hits using the collider layer collision matrix.
        public static List<RaycastHit2D> CastCollider2D(
            Collider2D collider,
            Vector2 direction,
            float distance,
            int bufferSize = 10,
            bool useTriggers = false)
        {
            if (collider == null)
            {
                return new List<RaycastHit2D>();
            }

            ContactFilter2D filter = CreateCollisionFilter2D(collider, useTriggers);
            return CastCollider2D(collider, direction, distance, filter, bufferSize);
        }

        // Casts a Collider2D and returns all hits using a custom filter.
        public static List<RaycastHit2D> CastCollider2D(
            Collider2D collider,
            Vector2 direction,
            float distance,
            ContactFilter2D filter,
            int bufferSize = 10)
        {
            List<RaycastHit2D> hits = new List<RaycastHit2D>();
            if (collider == null || bufferSize <= 0)
            {
                return hits;
            }

            RaycastHit2D[] hitBuffer = new RaycastHit2D[bufferSize];
            int hitCount = collider.Cast(direction, filter, hitBuffer, distance);
            for (int i = 0; i < hitCount; i++)
            {
                hits.Add(hitBuffer[i]);
            }

            return hits;
        }

        // Casts a Collider2D and returns true when any hit is found.
        public static bool HasCollision2D(Collider2D collider, Vector2 direction, float distance, LayerMask layerMask)
        {
            if (collider == null)
            {
                return false;
            }

            ContactFilter2D filter = CreateContactFilter2D(layerMask);
            RaycastHit2D[] hitBuffer = new RaycastHit2D[1];
            return collider.Cast(direction, filter, hitBuffer, distance) > 0;
        }

        #endregion

        #region Raycast 2D

        // Raycasts in 2D and returns true when a hit is found.
        public static bool Raycast2D(Vector2 origin, Vector2 direction, float distance, LayerMask layerMask, out RaycastHit2D hit)
        {
            hit = Physics2D.Raycast(origin, direction, distance, layerMask);
            return hit.collider != null;
        }

        // Raycasts in 2D from a transform position.
        public static bool Raycast2D(Transform origin, Vector2 direction, float distance, LayerMask layerMask, out RaycastHit2D hit)
        {
            hit = default(RaycastHit2D);
            if (origin == null)
            {
                return false;
            }

            return Raycast2D(origin.position, direction, distance, layerMask, out hit);
        }

        // Raycasts in 2D from screen position through a camera.
        public static bool ScreenRaycast2D(Camera camera, Vector2 screenPosition, LayerMask layerMask, out RaycastHit2D hit)
        {
            hit = default(RaycastHit2D);
            if (camera == null)
            {
                return false;
            }

            Vector2 worldPosition = camera.ScreenToWorldPoint(screenPosition);
            hit = Physics2D.Linecast(worldPosition, worldPosition, layerMask);
            return hit.collider != null;
        }

        #endregion

        #region Overlap 2D

        // Checks if a point overlaps any 2D collider in a LayerMask.
        public static bool OverlapPoint2D(Vector2 point, LayerMask layerMask, out Collider2D collider)
        {
            collider = Physics2D.OverlapPoint(point, layerMask);
            return collider != null;
        }

        // Gets all 2D colliders inside a circle.
        public static Collider2D[] OverlapCircleAll2D(Vector2 point, float radius, LayerMask layerMask)
        {
            return Physics2D.OverlapCircleAll(point, Mathf.Max(0f, radius), layerMask);
        }

        // Gets all 2D colliders inside a box.
        public static Collider2D[] OverlapBoxAll2D(Vector2 point, Vector2 size, float angle, LayerMask layerMask)
        {
            return Physics2D.OverlapBoxAll(point, size, angle, layerMask);
        }

        #endregion

        #region Raycast 3D

        // Raycasts in 3D and returns true when a hit is found.
        public static bool Raycast3D(Vector3 origin, Vector3 direction, float distance, LayerMask layerMask, out RaycastHit hit)
        {
            return Physics.Raycast(origin, direction, out hit, distance, layerMask);
        }

        // Raycasts in 3D from a transform position.
        public static bool Raycast3D(Transform origin, Vector3 direction, float distance, LayerMask layerMask, out RaycastHit hit)
        {
            hit = default(RaycastHit);
            if (origin == null)
            {
                return false;
            }

            return Raycast3D(origin.position, direction, distance, layerMask, out hit);
        }

        // Raycasts in 3D from screen position through a camera.
        public static bool ScreenRaycast3D(Camera camera, Vector2 screenPosition, float distance, LayerMask layerMask, out RaycastHit hit)
        {
            hit = default(RaycastHit);
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray, out hit, distance, layerMask);
        }

        #endregion

        #region Overlap 3D

        // Gets all 3D colliders inside a sphere.
        public static Collider[] OverlapSphereAll3D(Vector3 point, float radius, LayerMask layerMask)
        {
            return Physics.OverlapSphere(point, Mathf.Max(0f, radius), layerMask);
        }

        // Gets all 3D colliders inside a box.
        public static Collider[] OverlapBoxAll3D(Vector3 center, Vector3 halfExtents, Quaternion orientation, LayerMask layerMask)
        {
            return Physics.OverlapBox(center, halfExtents, orientation, layerMask);
        }

        #endregion
    }
}
