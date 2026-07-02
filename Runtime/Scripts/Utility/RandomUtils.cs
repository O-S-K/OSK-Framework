using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public static class RandomUtils
    {
        #region Example Usage

        // Basic random values:
        // int number = RandomUtils.RandomInt(1, 10); // inclusive
        // float amount = RandomUtils.RandomFloat(0f, 1f);
        // bool coinFlip = RandomUtils.RandomBool;
        //
        // Random item:
        // string item = RandomUtils.GetRandom("Sword", "Shield", "Potion");
        // string listItem = RandomUtils.GetRandom(items, "None");
        //
        // Random transform data:
        // Vector2 point2D = RandomUtils.RandomUnitCircle(3f);
        // Vector3 point3D = RandomUtils.RandomInsideSphere(5f);
        // transform.rotation = RandomUtils.RandomRotation();
        //
        // Shuffle:
        // RandomUtils.Shuffle(cards);
        // RandomUtils.RandomRange(cards, 2, 6);
        //
        // Deterministic random:
        // RandomUtils.InitSeed(12345);
        // int seeded = RandomUtils.RandomSystem(1, 100);

        #endregion

        #region Fields

        private static System.Random _random = new System.Random();

        #endregion

        #region Items

        // Gets a random item from params array.
        public static T GetRandom<T>(params T[] arr)
        {
            if (arr == null || arr.Length == 0)
            {
                return default(T);
            }

            return arr[UnityEngine.Random.Range(0, arr.Length)];
        }

        // Gets a random item from a list.
        public static T GetRandom<T>(IList<T> list, T fallback = default(T))
        {
            if (list == null || list.Count == 0)
            {
                return fallback;
            }

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        // Gets a random item from a list and removes it.
        public static T PopRandom<T>(IList<T> list, T fallback = default(T))
        {
            if (list == null || list.Count == 0 || list.IsReadOnly)
            {
                return fallback;
            }

            int index = UnityEngine.Random.Range(0, list.Count);
            T item = list[index];
            list.RemoveAt(index);
            return item;
        }

        #endregion

        #region Numbers

        // Gets a random integer between min and max, inclusive.
        public static int RandomInt(int min, int max)
        {
            if (min > max)
            {
                Swap(ref min, ref max);
            }

            return UnityEngine.Random.Range(min, max + 1);
        }

        // Gets a random float between min and max.
        public static float RandomFloat(float min, float max)
        {
            if (min > max)
            {
                Swap(ref min, ref max);
            }

            return UnityEngine.Random.Range(min, max);
        }

        // Gets a random float between minimum and maximum using System.Random.
        public static float GetRandomNumber(float minimum, float maximum)
        {
            if (minimum > maximum)
            {
                Swap(ref minimum, ref maximum);
            }

            return (float)_random.NextDouble() * (maximum - minimum) + minimum;
        }

        // Gets a random integer that is not in usedValues.
        public static int UniqueRandomInt(int min, int max, IList<int> usedValues = null, int fallback = 0)
        {
            if (min > max)
            {
                Swap(ref min, ref max);
            }

            int count = max - min + 1;
            if (count <= 0)
            {
                return fallback;
            }

            if (usedValues == null || usedValues.Count == 0)
            {
                return RandomInt(min, max);
            }

            List<int> availableValues = new List<int>(count);
            for (int i = min; i <= max; i++)
            {
                if (!usedValues.Contains(i))
                {
                    availableValues.Add(i);
                }
            }

            return availableValues.Count == 0 ? fallback : GetRandom(availableValues, fallback);
        }

        // Rolls true by percent chance from 0 to 100.
        public static bool ChancePercent(float percent)
        {
            return UnityEngine.Random.value * 100f < Mathf.Max(0f, percent);
        }

        // Rolls true by chance from 0 to 1.
        public static bool Chance01(float chance)
        {
            return UnityEngine.Random.value < Mathf.Clamp01(chance);
        }

        #endregion

        #region System Random

        // Initializes deterministic System.Random seed.
        public static void InitSeed(int seed)
        {
            _random = new System.Random(seed);
        }

        // Gets a deterministic random integer between min and max, inclusive.
        public static int RandomSystem(int min, int max)
        {
            if (min > max)
            {
                Swap(ref min, ref max);
            }

            return _random.Next(min, max + 1);
        }

        // Gets a deterministic random float between min and max.
        public static float RandomSystem(float min, float max)
        {
            if (min > max)
            {
                Swap(ref min, ref max);
            }

            return (float)_random.NextDouble() * (max - min) + min;
        }

        #endregion

        #region Vectors And Rotation

        // Gets a random uniform rotation.
        public static Quaternion RandomRotation()
        {
            return UnityEngine.Random.rotationUniform;
        }

        // Gets a random point inside a 2D circle.
        public static Vector2 RandomUnitCircle(float radius)
        {
            return UnityEngine.Random.insideUnitCircle * Mathf.Max(0f, radius);
        }

        // Gets a random point on the surface of a 3D sphere.
        public static Vector3 RandomUnitSphere(float radius)
        {
            return UnityEngine.Random.onUnitSphere * Mathf.Max(0f, radius);
        }

        // Gets a random point inside a 3D sphere.
        public static Vector3 RandomInsideSphere(float radius)
        {
            return UnityEngine.Random.insideUnitSphere * Mathf.Max(0f, radius);
        }

        // Gets a random Vector2 between min and max.
        public static Vector2 RandomVector2(Vector2 min, Vector2 max)
        {
            return new Vector2(RandomFloat(min.x, max.x), RandomFloat(min.y, max.y));
        }

        // Gets a random Vector3 between min and max.
        public static Vector3 RandomVector3(Vector3 min, Vector3 max)
        {
            return new Vector3(RandomFloat(min.x, max.x), RandomFloat(min.y, max.y), RandomFloat(min.z, max.z));
        }

        #endregion

        #region Color

        // Gets a random RGB color.
        public static Color RandomColor()
        {
            return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
        }

        // Gets a random color using Unity HSV range.
        public static Color RandomColorHSV()
        {
            return UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f);
        }

        // Gets a random color with custom alpha.
        public static Color RandomColor(float alpha)
        {
            Color color = RandomColor();
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        #endregion

        #region Properties

        // Gets a random bool.
        public static bool RandomBool
        {
            get { return UnityEngine.Random.value > 0.5f; }
        }

        // Gets either -1 or 1.
        public static int RandomSign
        {
            get { return RandomBool ? 1 : -1; }
        }

        #endregion

        #region Shuffle

        // Shuffles a range inside a list.
        public static void RandomRange<T>(List<T> rangeToRandom, int startIndex = 0, int endIndex = -1)
        {
            if (rangeToRandom == null || rangeToRandom.Count == 0)
            {
                return;
            }

            startIndex = Mathf.Clamp(startIndex, 0, rangeToRandom.Count - 1);
            if (endIndex < startIndex || endIndex >= rangeToRandom.Count)
            {
                endIndex = rangeToRandom.Count - 1;
            }

            for (int i = endIndex; i > startIndex; i--)
            {
                int randomIndex = UnityEngine.Random.Range(startIndex, i + 1);
                Swap(rangeToRandom, i, randomIndex);
            }
        }

        // Shuffles a list in place using System.Random.
        public static void Shuffle<T>(List<T> list)
        {
            if (list == null)
            {
                return;
            }

            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = _random.Next(0, i + 1);
                Swap(list, i, randomIndex);
            }
        }

        // Returns a shuffled copy of a list.
        public static List<T> Shuffled<T>(IList<T> list)
        {
            List<T> copy = list == null ? new List<T>() : new List<T>(list);
            Shuffle(copy);
            return copy;
        }

        #endregion

        #region Helpers

        // Swaps two list items.
        private static void Swap<T>(IList<T> list, int indexA, int indexB)
        {
            if (list == null || list.IsReadOnly || indexA < 0 || indexB < 0 || indexA >= list.Count || indexB >= list.Count || indexA == indexB)
            {
                return;
            }

            T temp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = temp;
        }

        // Swaps two integer values.
        private static void Swap(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }

        // Swaps two float values.
        private static void Swap(ref float a, ref float b)
        {
            float temp = a;
            a = b;
            b = temp;
        }

        #endregion
    }
}
