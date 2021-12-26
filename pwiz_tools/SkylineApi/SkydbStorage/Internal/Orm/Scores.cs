using System.Collections.Generic;
using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class Scores : Entity<Scores>
    {
        private IDictionary<string, double> _scores;

        public double? GetScore(string name)
        {
            if (_scores != null && _scores.TryGetValue(name, out double score))
            {
                return score;
            }

            return null;
        }

        public void SetScore(string name, double score)
        {
            _scores = _scores ?? new Dictionary<string, double>();
            _scores[name] = score;
        }
    }
}
