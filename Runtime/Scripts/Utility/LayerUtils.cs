using UnityEngine;

namespace OSK
{
    public static class LayerUtils
    {
        #region Example Usage

        // Sorting order by Y:
        // int order = LayerUtils.GetSortingOrder(transform.position, 0);
        //
        // Set layer:
        // gameObject.SetLayer("UI");
        // gameObject.SetLayer("Enemy", true); // include children
        //
        // Layer lookup:
        // if (LayerUtils.TryGetLayer("Player", out int playerLayer)) { gameObject.layer = playerLayer; }
        //
        // Layer mask:
        // LayerMask mask = LayerUtils.CreateMask("Player", "Enemy");
        // bool contains = LayerUtils.ContainsLayer(mask, gameObject);

        #endregion

        #region Constants

        public const int SortingOrderDefault = 5000;
        public const int sortingOrderDefault = SortingOrderDefault;

        private const int MinLayer = 0;
        private const int MaxLayer = 31;

        #endregion

        #region Sorting Order

        // Gets sorting order from world position, where higher Y means lower order.
        public static int GetSortingOrder(Vector3 position, int offset, int baseSortingOrder = SortingOrderDefault)
        {
            return Mathf.RoundToInt(baseSortingOrder - position.y) + offset;
        }

        // Gets sorting order from world Y, where higher Y means lower order.
        public static int GetSortingOrder(float yPosition, int offset = 0, int baseSortingOrder = SortingOrderDefault)
        {
            return Mathf.RoundToInt(baseSortingOrder - yPosition) + offset;
        }

        #endregion

        #region Layer Lookup

        // Gets a layer index from name, returning fallback when the layer is missing.
        public static int GetLayer(string layerName, int fallbackLayer = 0)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return IsValidLayer(layer) ? layer : fallbackLayer;
        }

        // Tries to get a layer index from name.
        public static bool TryGetLayer(string layerName, out int layer)
        {
            layer = LayerMask.NameToLayer(layerName);
            return IsValidLayer(layer);
        }

        // Gets a layer name from index.
        public static string GetLayerName(int layer)
        {
            return IsValidLayer(layer) ? LayerMask.LayerToName(layer) : string.Empty;
        }

        // Checks if a layer index is valid for Unity.
        public static bool IsValidLayer(int layer)
        {
            return layer >= MinLayer && layer <= MaxLayer;
        }

        #endregion

        #region Set Layer

        // Sets layer on a GameObject, optionally including all children.
        public static void SetLayer(this GameObject gameObject, int layer, bool applyToChildren = false)
        {
            if (gameObject == null || !IsValidLayer(layer))
            {
                return;
            }

            gameObject.layer = layer;
            if (!applyToChildren)
            {
                return;
            }

            SetLayerRecursive(gameObject.transform, layer);
        }

        // Sets layer on a GameObject by layer name.
        public static void SetLayer(this GameObject gameObject, string nameLayer)
        {
            SetLayer(gameObject, nameLayer, false);
        }

        // Sets layer on a GameObject by layer name, optionally including all children.
        public static void SetLayer(this GameObject gameObject, string nameLayer, bool applyToChildren)
        {
            if (!TryGetLayer(nameLayer, out int layer))
            {
                Debug.LogWarning("LayerUtils.SetLayer failed: layer not found: " + nameLayer);
                return;
            }

            SetLayer(gameObject, layer, applyToChildren);
        }

        // Sets layer on all children including the root object.
        public static void SetLayerAllChildren(this GameObject gameObject, string nameLayer)
        {
            SetLayer(gameObject, nameLayer, true);
        }

        // Sets layer on all children including the root object.
        public static void SetLayerAllChildren(this GameObject gameObject, int layer)
        {
            SetLayer(gameObject, layer, true);
        }

        // Sets layer recursively on a transform tree.
        private static void SetLayerRecursive(Transform root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursive(root.GetChild(i), layer);
            }
        }

        #endregion

        #region Layer Mask

        // Creates a layer mask from layer indexes.
        public static LayerMask CreateMask(params int[] layers)
        {
            int mask = 0;
            if (layers == null)
            {
                return mask;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (IsValidLayer(layers[i]))
                {
                    mask |= 1 << layers[i];
                }
            }

            return mask;
        }

        // Creates a layer mask from layer names.
        public static LayerMask CreateMask(params string[] layerNames)
        {
            int mask = 0;
            if (layerNames == null)
            {
                return mask;
            }

            for (int i = 0; i < layerNames.Length; i++)
            {
                if (TryGetLayer(layerNames[i], out int layer))
                {
                    mask |= 1 << layer;
                }
            }

            return mask;
        }

        // Checks if a layer mask contains a layer index.
        public static bool ContainsLayer(LayerMask layerMask, int layer)
        {
            return IsValidLayer(layer) && (layerMask.value & (1 << layer)) != 0;
        }

        // Checks if a layer mask contains a GameObject layer.
        public static bool ContainsLayer(LayerMask layerMask, GameObject gameObject)
        {
            return gameObject != null && ContainsLayer(layerMask, gameObject.layer);
        }

        // Adds a layer index to a layer mask.
        public static LayerMask AddLayer(LayerMask layerMask, int layer)
        {
            if (!IsValidLayer(layer))
            {
                return layerMask;
            }

            layerMask.value |= 1 << layer;
            return layerMask;
        }

        // Removes a layer index from a layer mask.
        public static LayerMask RemoveLayer(LayerMask layerMask, int layer)
        {
            if (!IsValidLayer(layer))
            {
                return layerMask;
            }

            layerMask.value &= ~(1 << layer);
            return layerMask;
        }

        #endregion
    }
}
