using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public static class ListUtils
    {
        #region Example Usage

        // Safe get:
        // int value = numbers.SafeGet(2, -1);
        // bool hasItem = numbers.TryGet(0, out int first);
        //
        // Add and random:
        // list.AddUnique(item);
        // Item random = list.GetRandom();
        //
        // Transform:
        // List<string> names = ListUtils.Map(players, p => p.name);
        // List<Player> alivePlayers = ListUtils.Filter(players, p => p.IsAlive);
        //
        // Modify:
        // list.Shuffle();
        // list.Swap(0, 1);
        // list.RemoveNulls();
        //
        // Compare:
        // bool sameItems = ListUtils.CompareListsUnordered(listA, listB);

        #endregion

        #region Delegates

        public delegate TResult MapFunc<out TResult, TArg>(TArg arg);
        public delegate bool FilterFunc<TArg>(TArg arg);

        #endregion

        #region State

        // Checks if a list is null or has no items.
        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        // Checks if an index is valid for a list.
        public static bool IsValidIndex<T>(this IList<T> list, int index)
        {
            return list != null && index >= 0 && index < list.Count;
        }

        // Clamps an index to a valid list range.
        public static int ClampIndex<T>(this IList<T> list, int index)
        {
            if (list == null || list.Count == 0)
            {
                return -1;
            }

            return Mathf.Clamp(index, 0, list.Count - 1);
        }

        #endregion

        #region Add

        // Adds a Vector3 to the first index and returns the same list.
        public static List<Vector3> AddFirstList(this List<Vector3> list, Vector3 obj)
        {
            return AddFirst(list, obj);
        }

        // Adds a Vector3 to the last index and returns the same list.
        public static List<Vector3> AddLastList(this List<Vector3> list, Vector3 obj)
        {
            return AddLast(list, obj);
        }

        // Adds an item to the first index and returns the same list.
        public static List<T> AddFirst<T>(this List<T> list, T item)
        {
            if (list == null)
            {
                return null;
            }

            list.Insert(0, item);
            return list;
        }

        // Adds an item to the last index and returns the same list.
        public static List<T> AddLast<T>(this List<T> list, T item)
        {
            if (list == null)
            {
                return null;
            }

            list.Add(item);
            return list;
        }

        // Adds an item only when it is not already in the list.
        public static bool AddUnique<T>(this IList<T> list, T item)
        {
            if (list == null || list.IsReadOnly || list.Contains(item))
            {
                return false;
            }

            list.Add(item);
            return true;
        }

        #endregion

        #region Get

        // Gets an item safely, returning fallback when index is invalid.
        public static T SafeGet<T>(this IList<T> list, int index, T fallback = default(T))
        {
            return list.IsValidIndex(index) ? list[index] : fallback;
        }

        // Tries to get an item by index.
        public static bool TryGet<T>(this IList<T> list, int index, out T value)
        {
            if (list.IsValidIndex(index))
            {
                value = list[index];
                return true;
            }

            value = default(T);
            return false;
        }

        // Gets the first item or fallback when the list is empty.
        public static T SafeFirst<T>(this IList<T> list, T fallback = default(T))
        {
            return list.IsNullOrEmpty() ? fallback : list[0];
        }

        // Gets the last item or fallback when the list is empty.
        public static T SafeLast<T>(this IList<T> list, T fallback = default(T))
        {
            return list.IsNullOrEmpty() ? fallback : list[list.Count - 1];
        }

        // Gets a random item from a list.
        public static T GetRandom<T>(this IList<T> list, T fallback = default(T))
        {
            if (list.IsNullOrEmpty())
            {
                return fallback;
            }

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        // Gets the closest component in an array to a world point.
        public static T GetClosest<T>(this T[] collection, Vector3 toPoint) where T : Component
        {
            if (collection == null || collection.Length == 0)
            {
                return null;
            }

            T closest = null;
            float closestDistance = Mathf.Infinity;
            for (int i = 0; i < collection.Length; i++)
            {
                T item = collection[i];
                if (item == null)
                {
                    continue;
                }

                float sqrDistance = (item.transform.position - toPoint).sqrMagnitude;
                if (sqrDistance < closestDistance)
                {
                    closestDistance = sqrDistance;
                    closest = item;
                }
            }

            return closest;
        }

        // Gets the closest component in a read-only list to a world point.
        public static T GetClosest<T>(this IReadOnlyList<T> collection, Vector3 toPoint) where T : Component
        {
            if (collection == null || collection.Count == 0)
            {
                return null;
            }

            T closest = null;
            float closestDistance = Mathf.Infinity;
            for (int i = 0; i < collection.Count; i++)
            {
                T item = collection[i];
                if (item == null)
                {
                    continue;
                }

                float sqrDistance = (item.transform.position - toPoint).sqrMagnitude;
                if (sqrDistance < closestDistance)
                {
                    closestDistance = sqrDistance;
                    closest = item;
                }
            }

            return closest;
        }

        #endregion

        #region Transform

        // Maps a list into a new list.
        public static List<TOut> Map<TIn, TOut>(List<TIn> list, MapFunc<TOut, TIn> func)
        {
            List<TOut> newList = new List<TOut>(list == null ? 0 : list.Count);
            if (list == null || func == null)
            {
                return newList;
            }

            for (int i = 0; i < list.Count; i++)
            {
                newList.Add(func(list[i]));
            }

            return newList;
        }

        // Filters a list into a new list.
        public static List<T> Filter<T>(List<T> list, FilterFunc<T> func)
        {
            List<T> newList = new List<T>();
            if (list == null || func == null)
            {
                return newList;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (func(list[i]))
                {
                    newList.Add(list[i]);
                }
            }

            return newList;
        }

        // Shuffles a list in place.
        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null)
            {
                return;
            }

            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                Swap(list, i, randomIndex);
            }
        }

        // Creates a shuffled copy of a list.
        public static List<T> Shuffled<T>(this IList<T> list)
        {
            List<T> copy = list == null ? new List<T>() : new List<T>(list);
            copy.Shuffle();
            return copy;
        }

        #endregion

        #region Compare

        // Compares two lists by content, ignoring order and preserving both input lists.
        public static bool CompareLists<T>(List<T> list1, List<T> list2)
        {
            return CompareListsUnordered(list1, list2);
        }

        // Compares two lists by content and order.
        public static bool CompareListsOrdered<T>(IList<T> list1, IList<T> list2)
        {
            if (list1 == null || list2 == null)
            {
                return list1 == list2;
            }

            if (list1.Count != list2.Count)
            {
                return false;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < list1.Count; i++)
            {
                if (!comparer.Equals(list1[i], list2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Compares two lists by content and ignores order.
        public static bool CompareListsUnordered<T>(IList<T> list1, IList<T> list2)
        {
            if (list1 == null || list2 == null)
            {
                return list1 == list2;
            }

            if (list1.Count != list2.Count)
            {
                return false;
            }

            bool[] matched = new bool[list2.Count];
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < list1.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < list2.Count; j++)
                {
                    if (matched[j] || !comparer.Equals(list1[i], list2[j]))
                    {
                        continue;
                    }

                    matched[j] = true;
                    found = true;
                    break;
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Modify

        // Removes null class references from a list.
        public static void RefreshList<T>(this List<T> list) where T : class
        {
            RemoveNulls(list);
        }

        // Removes null class references from a list and returns removed count.
        public static int RemoveNulls<T>(this IList<T> list) where T : class
        {
            if (list == null || list.IsReadOnly)
            {
                return 0;
            }

            int removedCount = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] != null)
                {
                    continue;
                }

                list.RemoveAt(i);
                removedCount++;
            }

            return removedCount;
        }

        // Swaps two items in a list.
        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            if (list == null || list.IsReadOnly || !list.IsValidIndex(indexA) || !list.IsValidIndex(indexB) || indexA == indexB)
            {
                return;
            }

            T temp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = temp;
        }

        // Moves an item from one index to another index.
        public static void Move<T>(this IList<T> list, int fromIndex, int toIndex)
        {
            if (list == null || list.IsReadOnly || !list.IsValidIndex(fromIndex))
            {
                return;
            }

            toIndex = Mathf.Clamp(toIndex, 0, list.Count - 1);
            if (fromIndex == toIndex)
            {
                return;
            }

            T item = list[fromIndex];
            list.RemoveAt(fromIndex);
            list.Insert(toIndex, item);
        }

        // Removes an item by swapping it with the last item first.
        public static bool RemoveAtSwapBack<T>(this IList<T> list, int index)
        {
            if (list == null || list.IsReadOnly || !list.IsValidIndex(index))
            {
                return false;
            }

            int lastIndex = list.Count - 1;
            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);
            return true;
        }

        #endregion
    }
}
