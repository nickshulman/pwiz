using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.Results
{
    public class ChromFileInfoIndex : IEnumerable<ChromFileInfo>
    {
        public static readonly ChromFileInfoIndex EMPTY = new ChromFileInfoIndex(ImmutableList.Empty<ChromFileInfo>());
        private readonly ImmutableList<ChromFileInfo> _infos;

        private readonly Dictionary<ReferenceValue<ChromFileInfoId>, int> _index;

        public ChromFileInfoIndex(IEnumerable<ChromFileInfo> infos)
        {
            var list = new List<ChromFileInfo>();
            var dictionary = new Dictionary<ReferenceValue<ChromFileInfoId>, int>();
            foreach (var info in infos)
            {
                if (!dictionary.ContainsKey(info.FileId))
                {
                    dictionary.Add(info.FileId, list.Count);
                    list.Add(info);
                }
            }

            _infos = ImmutableList.ValueOf(list);
            _index = dictionary;
        }

        public static ChromFileInfoIndex FromChromatogramSets(IEnumerable<ChromatogramSet> chromatogramSets)
        {
            return new ChromFileInfoIndex(chromatogramSets.SelectMany(set =>
                set.MSDataFileInfos));
        }

        public int Count
        {
            get { return _infos.Count; }
        }

        public int IndexOf(ChromFileInfoId id)
        {
            if (_index.TryGetValue(id, out int index))
            {
                return index;
            }

            return -1;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ChromFileInfo> GetEnumerator()
        {
            return _infos.GetEnumerator();
        }

        public IEnumerable<ChromFileInfoId> FileIds
        {
            get { return _infos.Select(info => info.FileId); }
        }

        public ChromFileInfo this[ChromFileInfoId fileId]
        {
            get
            {
                if (_index.TryGetValue(fileId, out int index))
                {
                    return _infos[index];
                }

                return null;
            }
        }

        public T[] MakeArray<T>(IDictionary<ReferenceValue<ChromFileInfoId>, T> items)
        {
            var array = new T[Count];
            foreach (var item in items)
            {
                int index = IndexOf(item.Key);
                if (index >= 0)
                {
                    array[index] = item.Value;
                }
            }

            return array;
        }
    }
}
