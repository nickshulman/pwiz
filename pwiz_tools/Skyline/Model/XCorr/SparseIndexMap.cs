using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.XCorr
{
    public interface ISparseIndexMap
    {
        int Count { get; }
        void PutIfGreater(int key, double mass, double intensity);
        void AdjustOrPutValue(int key, double mass, double intensity);
        void MultiplyAllValues(double value);
        IEnumerable<KeyValuePair<int, Peak>> OrderedEnumerable { get; }
    }


    public class OrderedSparseIndexMap : ISparseIndexMap
    {
        private List<KeyValuePair<int, Peak>> _list;
        public OrderedSparseIndexMap()
        {
            _list = new List<KeyValuePair<int, Peak>>();
        }

        public OrderedSparseIndexMap(int capacity)
        {
            _list = new List<KeyValuePair<int, Peak>>(capacity);
        }

        public IEnumerable<KeyValuePair<int, Peak>> OrderedEnumerable
        {
            get { return _list.AsEnumerable(); }
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public void PutIfGreater(int key, double mass, double intensity)
        {
            int index = BinarySearch(key);
            if (index < 0)
            {
                _list.Insert(~index, new KeyValuePair<int, Peak>(key, new Peak(mass, intensity)));
                return;
            }

            if (_list[index].Value.Intensity >= intensity)
            {
                return;
            }

            _list[index] = new KeyValuePair<int, Peak>(key, new Peak(mass, intensity));
        }

        public void AdjustOrPutValue(int key, double mass, double intensity)
        {
            int index = BinarySearch(key);
            if (index < 0)
            {
                _list.Insert(~index, new KeyValuePair<int, Peak>(key, new Peak(mass, intensity)));
                return;
            }

            var oldPeak = _list[index].Value;
            _list[index] = new KeyValuePair<int, Peak>(key, new Peak(oldPeak.Mass, oldPeak.Intensity + intensity));
        }

        public void MultiplyAllValues(double value)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                var entry = _list[i];
                _list[i] = new KeyValuePair<int, Peak>(entry.Key,
                    new Peak(entry.Value.Mass, entry.Value.Intensity * value));
            }
        }

        private int BinarySearch(int key)
        {
            if (_list.Count == 0 || key > _list[_list.Count - 1].Key)
            {
                return ~_list.Count;
            }

            if (key == _list[_list.Count - 1].Key)
            {
                return _list.Count - 1;
            }

            if (_list.Count >= 2 && key == _list[_list.Count - 2].Key)
            {
                return _list.Count - 2;
            }

            var range = CollectionUtil.BinarySearch(_list, item => item.Key.CompareTo(key));
            if (range.Length == 0)
            {
                return ~range.Start;
            }
            return range.Start;
        }
    }

    public class RandomSparseIndexMap : ISparseIndexMap
    {
        private Dictionary<int, Peak> _dictionary;

        public RandomSparseIndexMap(int capacity)
        {
            _dictionary = new Dictionary<int, Peak>(capacity);
        }

        public RandomSparseIndexMap()
        {
            _dictionary = new Dictionary<int, Peak>();
        }

        public IEnumerable<KeyValuePair<int, Peak>> OrderedEnumerable
        {
            get { return _dictionary.OrderBy(kvp => kvp.Key); }
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public void PutIfGreater(int key, double mass, double intensity)
        {
            Peak existing;
            if (_dictionary.TryGetValue(key, out existing))
            {
                if (existing.Intensity >= intensity)
                {
                    return;
                }

                _dictionary[key] = new Peak(mass, intensity);
            }
        }

        public void AdjustOrPutValue(int key, double mass, double intensity)
        {
            Peak existing;
            if (_dictionary.TryGetValue(key, out existing))
            {
                _dictionary[key] = new Peak(existing.Mass, existing.Intensity + intensity);
            }
            else
            {
                _dictionary.Add(key, new Peak(mass, intensity));
            }
        }

        public void MultiplyAllValues(double value)
        {
            var newDict = new Dictionary<int, Peak>(_dictionary.Count);
            foreach (var entry in _dictionary)
            {
                newDict.Add(entry.Key, new Peak(entry.Value.Mass, entry.Value.Intensity * value));
            }

            _dictionary = newDict;
        }
    }
}
