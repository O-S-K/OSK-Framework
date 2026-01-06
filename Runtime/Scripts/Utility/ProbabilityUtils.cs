using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace OSK
{
    #region Example Usage
    // var dropTable = new Probability<string>();
    // dropTable.Add("Rác", 50);
    // dropTable.Add("Vàng", 30);
    // dropTable.Add("Kiếm", 5);
    // string result = dropTable.Get();

    // Gacha pity system example:
    // var gacha = new Probability<string>();
    // gacha.Add("Common", 70);
    // gacha.Add("Rare", 25);
    // gacha.Add("Legendary", baseLegendRate); // e.g., baseLegendRate = 5  
    // float currentLegendRate = baseLegendRate;
    // float pityStep = 2; // Mỗi lần không ra Legendary thì tăng tỷ lệ thêm 2%
    // string result = gacha.Get();
    
    // if (result == "Legendary")
    // {
    //     currentLegendRate = baseLegendRate;
    //     gacha.ResetAllWeights(); 
    // }
    // else
    // {
    //     currentLegendRate += pityStep;
    //     gacha.ModifyWeight("Legendary", currentLegendRate);
    // }

    #endregion

    public class Probability<T> : IEnumerable
    {
        private class Entry
        {
            public T Item;
            public float Weight;
            public float OriginalWeight;
        }

        private readonly List<Entry> _elements = new();
        public float TotalWeight { get; private set; }

        public bool IsEmpty => _elements.Count == 0;

        public void Add(T item, float weight)
        {
            if (weight <= 0) return;
            _elements.Add(new Entry { Item = item, Weight = weight, OriginalWeight = weight });
            TotalWeight += weight;
        }

        public T Get()
        {
            if (IsEmpty) return default;
            if (TotalWeight <= 0) return _elements[0].Item;

            float randomPoint = Random.Range(0f, TotalWeight);
            foreach (var entry in _elements)
            {
                if ((randomPoint -= entry.Weight) <= 0)
                    return entry.Item;
            }

            return _elements[^1].Item;
        }

        public void ModifyWeight(T item, float newWeight)
        {
            foreach (var entry in _elements)
            {
                if (EqualityComparer<T>.Default.Equals(entry.Item, item))
                {
                    TotalWeight -= entry.Weight; // Trừ cái cũ
                    entry.Weight = newWeight; // Gán cái mới
                    TotalWeight += newWeight; // Cộng lại vào tổng
                    return;
                }
            }
        }

        public void ResetAllWeights()
        {
            TotalWeight = 0;
            foreach (var entry in _elements)
            {
                entry.Weight = entry.OriginalWeight;
                TotalWeight += entry.OriginalWeight;
            }
        }

        public T Get(int seed)
        {
            if (IsEmpty) return default;
            var sysRand = new System.Random(seed);
            float randomPoint = (float)(sysRand.NextDouble() * TotalWeight);
            foreach (var entry in _elements)
            {
                if ((randomPoint -= entry.Weight) <= 0) return entry.Item;
            }

            return _elements[^1].Item;
        }

        public IEnumerator GetEnumerator() => _elements.GetEnumerator();
    }
}