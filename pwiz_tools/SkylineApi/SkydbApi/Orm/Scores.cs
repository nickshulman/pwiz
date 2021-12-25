using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class]
    public class Scores : Entity<Scores>
    {
        private IDictionary<string, double> _scores;

        public virtual double? GetScore(string name)
        {
            if (_scores != null && _scores.TryGetValue(name, out double score))
            {
                return score;
            }

            return null;
        }

        public virtual void SetScore(string name, double score)
        {
            _scores = _scores ?? new Dictionary<string, double>();
            _scores[name] = score;
        }
    }
}
