using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace TopographTool.Model
{
    public class LabelContribution
    {
        private readonly ImmutableSortedList<int, double> _amounts;
        public LabelContribution(IEnumerable<KeyValuePair<int, double>> amounts)
        {
            var values = new List<KeyValuePair<int, double>>();
            foreach (var group in amounts.ToLookup(a => a.Key))
            {
                values.Add(new KeyValuePair<int, double>(group.Key, group.Sum(kvp=>kvp.Value)));
            }
            _amounts = ImmutableSortedList.FromValues(values);
        }

        public double GetContribution(int labelCount)
        {
            double amount;
            if (_amounts.TryGetValue(labelCount, out amount))
            {
                return amount;
            }
            return 0;
        }

        public IEnumerable<int> LabelCounts { get { return _amounts.Keys; } }

        public override string ToString()
        {
            return string.Join(",", _amounts.Select(entry => entry.Key + ":" + entry.Value.ToString(".####")));
        }
    }
}
