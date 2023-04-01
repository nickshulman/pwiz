using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.DocSettings.AbsoluteQuantification;
using static pwiz.Skyline.Model.GroupComparison.PeptideQuantifier;

namespace pwiz.Skyline.Model.GroupComparison
{
    public class PeptideListQuantifier
    {
        public PeptideListQuantifier(IEnumerable<PeptideQuantifier> peptideQuantifiers)
        {
            PeptideQuantifiers = ImmutableList.ValueOf(peptideQuantifiers);
            var normalizationMethods = PeptideQuantifiers.Select(q => q.NormalizationMethod).Distinct().ToList();
            if (normalizationMethods.Count == 1)
            {
                NormalizationMethod = normalizationMethods[0];
            }
            else
            {
                NormalizationMethod = PeptideQuantifiers.FirstOrDefault()?.QuantificationSettings.NormalizationMethod ?? NormalizationMethod.NONE;
            }

            var internalStandardConcentrations = PeptideQuantifiers
                .Select(q => q.PeptideDocNode.InternalStandardConcentration).OfType<double>().Distinct().ToList();
            if (internalStandardConcentrations.Count == 1)
            {
                InternalStandardConcentration = internalStandardConcentrations[0];
            }
        }

        public ImmutableList<PeptideQuantifier> PeptideQuantifiers { get; }

        public QuantificationSettings QuantificationSettings
        {
            get { return PeptideQuantifiers.FirstOrDefault()?.QuantificationSettings; }
        }

        public IDictionary<IdentityPath, Quantity> GetTransitionIntensities(SrmSettings srmSettings, int replicateIndex,
            bool treatMissingAsZero)
        {
            if (PeptideQuantifiers.Count == 1)
            {
                PeptideQuantifiers[0]
                    .GetTransitionIntensities(srmSettings, replicateIndex, treatMissingAsZero);
            }

            return CollectionUtil.SafeToDictionary(PeptideQuantifiers.SelectMany(q =>
                q.GetTransitionIntensities(srmSettings, replicateIndex, treatMissingAsZero)));
        }

        public NormalizationMethod NormalizationMethod { get; }

        public bool IsExcludeFromCalibration(int replicateIndex)
        {
            return PeptideQuantifiers.Any(q => q.PeptideDocNode.IsExcludeFromCalibration(replicateIndex));
        }

        public double? InternalStandardConcentration { get; }
    }
}
