using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OSK
{
    #region Example Usage

    // Basic drop table:
    // var dropTable = new Probability<string>();
    // dropTable.Add("Trash", 50);
    // dropTable.Add("Gold", 30);
    // dropTable.Add("Sword", 5);
    // string result = dropTable.Get();
    //
    // Roll chance:
    // bool isCritical = Probability<string>.RollPercent(25f); // 25%
    // bool isLucky = Probability<string>.Roll01(0.1f); // 10%
    //
    // Deterministic result by seed:
    // int seed = 12345;
    // string seededResult = dropTable.Get(seed, "None");
    //
    // Static weighted pick:
    // var items = new List<string> { "Coin", "Gem", "Key" };
    // var weights = new List<float> { 80f, 15f, 5f };
    // string picked = Probability<string>.GetWeighted(items, weights, "None");
    //
    // Gacha pity example:
    // var gacha = new Probability<string>();
    // float baseLegendaryRate = 5f;
    // float currentLegendaryRate = baseLegendaryRate;
    // float pityStep = 2f;
    //
    // gacha.Add("Common", 70);
    // gacha.Add("Rare", 25);
    // gacha.Add("Legendary", baseLegendaryRate);
    //
    // string pullResult = gacha.Get();
    // if (pullResult == "Legendary")
    // {
    //     currentLegendaryRate = baseLegendaryRate;
    //     gacha.ResetAllWeights();
    // }
    // else
    // {
    //     currentLegendaryRate += pityStep;
    //     gacha.ModifyWeight("Legendary", currentLegendaryRate);
    // }
    //
    // float legendaryChance = gacha.GetChancePercent("Legendary");

    #endregion

    public class Probability<T> : IEnumerable<Probability<T>.Entry>
    {
        #region Entry

        public class Entry
        {
            public T Item;
            public float Weight;
            public float OriginalWeight;
        }

        #endregion

        #region Fields

        private readonly List<Entry> _elements = new List<Entry>();

        #endregion

        #region Properties

        public float TotalWeight { get; private set; }

        public int Count
        {
            get { return _elements.Count; }
        }

        public bool IsEmpty
        {
            get { return _elements.Count == 0; }
        }

        #endregion

        #region Add And Remove

        // Adds an item with a positive weight.
        public void Add(T item, float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            _elements.Add(new Entry { Item = item, Weight = weight, OriginalWeight = weight });
            TotalWeight += weight;
        }

        // Adds or updates an item weight.
        public void AddOrUpdate(T item, float weight)
        {
            if (Contains(item))
            {
                ModifyWeight(item, weight);
                return;
            }

            Add(item, weight);
        }

        // Removes an item from the probability table.
        public bool Remove(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _elements.Count; i++)
            {
                if (!comparer.Equals(_elements[i].Item, item))
                {
                    continue;
                }

                TotalWeight -= _elements[i].Weight;
                _elements.RemoveAt(i);
                return true;
            }

            return false;
        }

        // Clears all items and weights.
        public void Clear()
        {
            _elements.Clear();
            TotalWeight = 0f;
        }

        #endregion

        #region Get

        // Gets a random item using UnityEngine.Random.
        public T Get()
        {
            return Get(default(T));
        }

        // Gets a random item using UnityEngine.Random, returning fallback when empty.
        public T Get(T fallback)
        {
            if (IsEmpty || TotalWeight <= 0f)
            {
                return fallback;
            }

            float randomPoint = Random.Range(0f, TotalWeight);
            return GetByRandomPoint(randomPoint, fallback);
        }

        // Gets a deterministic random item using a seed.
        public T Get(int seed)
        {
            return Get(seed, default(T));
        }

        // Gets a deterministic random item using a seed, returning fallback when empty.
        public T Get(int seed, T fallback)
        {
            if (IsEmpty || TotalWeight <= 0f)
            {
                return fallback;
            }

            System.Random random = new System.Random(seed);
            float randomPoint = (float)(random.NextDouble() * TotalWeight);
            return GetByRandomPoint(randomPoint, fallback);
        }

        // Gets an item by index.
        public T GetItemAt(int index, T fallback = default(T))
        {
            if (index < 0 || index >= _elements.Count)
            {
                return fallback;
            }

            return _elements[index].Item;
        }

        // Gets current weight for an item.
        public float GetWeight(T item, float fallback = 0f)
        {
            Entry entry = FindEntry(item);
            return entry != null ? entry.Weight : fallback;
        }

        // Gets original weight for an item.
        public float GetOriginalWeight(T item, float fallback = 0f)
        {
            Entry entry = FindEntry(item);
            return entry != null ? entry.OriginalWeight : fallback;
        }

        #endregion

        #region Modify

        // Modifies current item weight and keeps total weight in sync.
        public bool ModifyWeight(T item, float newWeight)
        {
            Entry entry = FindEntry(item);
            if (entry == null)
            {
                return false;
            }

            TotalWeight -= entry.Weight;
            entry.Weight = Mathf.Max(0f, newWeight);
            TotalWeight += entry.Weight;
            return true;
        }

        // Modifies original item weight used by ResetAllWeights.
        public bool ModifyOriginalWeight(T item, float newOriginalWeight)
        {
            Entry entry = FindEntry(item);
            if (entry == null)
            {
                return false;
            }

            entry.OriginalWeight = Mathf.Max(0f, newOriginalWeight);
            return true;
        }

        // Resets all current weights back to original weights.
        public void ResetAllWeights()
        {
            TotalWeight = 0f;
            for (int i = 0; i < _elements.Count; i++)
            {
                _elements[i].Weight = Mathf.Max(0f, _elements[i].OriginalWeight);
                TotalWeight += _elements[i].Weight;
            }
        }

        // Multiplies an item current weight.
        public bool MultiplyWeight(T item, float multiplier)
        {
            Entry entry = FindEntry(item);
            if (entry == null)
            {
                return false;
            }

            return ModifyWeight(item, entry.Weight * multiplier);
        }

        #endregion

        #region Query

        // Checks if the table contains an item.
        public bool Contains(T item)
        {
            return FindEntry(item) != null;
        }

        // Gets chance from 0 to 1 for an item using current total weight.
        public float GetChance01(T item)
        {
            if (TotalWeight <= 0f)
            {
                return 0f;
            }

            return GetWeight(item) / TotalWeight;
        }

        // Gets chance from 0 to 100 for an item using current total weight.
        public float GetChancePercent(T item)
        {
            return GetChance01(item) * 100f;
        }

        #endregion

        #region Static Helpers

        // Rolls true by percent chance from 0 to 100.
        public static bool RollPercent(float percent)
        {
            return Random.value * 100f < Mathf.Max(0f, percent);
        }

        // Rolls true by chance from 0 to 1.
        public static bool Roll01(float chance)
        {
            return Random.value < Mathf.Clamp01(chance);
        }

        // Picks one item from items using matching weights.
        public static T GetWeighted(IList<T> items, IList<float> weights, T fallback = default(T))
        {
            if (items == null || weights == null || items.Count == 0 || items.Count != weights.Count)
            {
                return fallback;
            }

            Probability<T> probability = new Probability<T>();
            for (int i = 0; i < items.Count; i++)
            {
                probability.Add(items[i], weights[i]);
            }

            return probability.Get(fallback);
        }

        #endregion

        #region Internals

        // Returns a weighted item from a random point between 0 and TotalWeight.
        private T GetByRandomPoint(float randomPoint, T fallback)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                randomPoint -= _elements[i].Weight;
                if (randomPoint <= 0f)
                {
                    return _elements[i].Item;
                }
            }

            return _elements.Count > 0 ? _elements[_elements.Count - 1].Item : fallback;
        }

        // Finds an entry by item.
        private Entry FindEntry(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _elements.Count; i++)
            {
                if (comparer.Equals(_elements[i].Item, item))
                {
                    return _elements[i];
                }
            }

            return null;
        }

        #endregion

        #region IEnumerable

        // Gets typed enumerator over probability entries.
        public IEnumerator<Entry> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        // Gets non-generic enumerator over probability entries.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
