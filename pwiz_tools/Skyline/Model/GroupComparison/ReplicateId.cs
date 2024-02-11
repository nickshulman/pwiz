using System.Collections.Generic;
using System.Linq;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.GroupComparison
{
    public class ReplicateId
    {
        public ReplicateId(int index, string multiplex)
        {
            ReplicateIndex = index;
            MultiplexName = multiplex ?? string.Empty;
        }
        public int ReplicateIndex { get; }
        public string MultiplexName { get; }

        protected bool Equals(ReplicateId other)
        {
            return ReplicateIndex == other.ReplicateIndex && MultiplexName == other.MultiplexName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ReplicateId)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ReplicateIndex * 397) ^ (MultiplexName != null ? MultiplexName.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            string str = ReplicateIndex.ToString();
            if (string.IsNullOrEmpty(MultiplexName))
            {
                return str;
            }

            return TextUtil.ColonSeparate(str, MultiplexName);
        }

        public static IEnumerable<ReplicateId> Enumerate(SrmSettings settings)
        {
            if (settings.MeasuredResults == null)
            {
                return Enumerable.Empty<ReplicateId>();
            }
            var measuredResults = settings.MeasuredResults;
            var multiplexMatrix = settings.PeptideSettings.Quantification.MultiplexMatrix;
            if (multiplexMatrix?.Replicates.Count > 0)
            {
                return Enumerable.Range(0, measuredResults.Chromatograms.Count).SelectMany(replicateIndex =>
                    multiplexMatrix.Replicates.Select(replicate => new ReplicateId(replicateIndex, replicate.Name)));
            }

            return Enumerable.Range(0, measuredResults.Chromatograms.Count)
                .Select(i => new ReplicateId(i, string.Empty));
        }
    }
}