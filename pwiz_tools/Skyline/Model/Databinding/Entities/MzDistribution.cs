using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    public abstract class AbstractDistribution : IFormattable
    {
        protected FormattableList<double> _mzsOrMasses;
        protected double _averageMzOrMass;
        protected double? _monoMzOrMass;
        protected AbstractDistribution(IEnumerable<KeyValuePair<double, double>> mzOrMassIntensities, double? monoMzOrMass)
        {
            var mzList = mzOrMassIntensities.OrderBy(mz => mz.Key).ToArray();
            _mzsOrMasses = new FormattableList<double>(ImmutableList.ValueOf(mzList.Select(mz => mz.Key)));
            Abundances = new FormattableList<double>(ImmutableList.ValueOf(mzList.Select(mz => mz.Value)));
            var totalAbundance = mzList.Sum(mz => mz.Value);
            if (totalAbundance != 0)
            {
                var totalMzIntensity = mzList.Sum(mz => mz.Key * mz.Value);
                _averageMzOrMass = totalMzIntensity / totalAbundance;
            }
            _monoMzOrMass = monoMzOrMass;
        }

        [Format(Formats.STANDARD_RATIO)]
        public FormattableList<double> Abundances { get; private set; }
        public override string ToString()
        {
            return ToString(null, CultureInfo.CurrentCulture);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (_monoMzOrMass.HasValue)
            {
                return TextUtil.SpaceSeparate("Mono:", _monoMzOrMass.Value.ToString(format, formatProvider),
                    "Average:", _averageMzOrMass.ToString(format, formatProvider));
            }
            return TextUtil.SpaceSeparate("Average:", _averageMzOrMass.ToString(format, formatProvider));
        }
        public class MzDistribution : AbstractDistribution
        {
            public MzDistribution(IEnumerable<KeyValuePair<double, double>> mzs, double? monoisotopicMz)
                : base(mzs,
                    monoisotopicMz)
            {
            }

            [Format(Formats.Mz)]
            public FormattableList<double> Mzs { get { return _mzsOrMasses; } }
            [Format(Formats.Mz)]
            public double AverageMz { get { return _averageMzOrMass; } }
            [Format(Formats.Mz)]
            public double? MonoisotopicMz { get { return _monoMzOrMass; } }
        }
        public class MassDistribution : AbstractDistribution
        {
            public MassDistribution(IEnumerable<KeyValuePair<double, double>> massAbundances, double? monoisotopicMass)
                : base(massAbundances, monoisotopicMass)
            {
            }
            [Format(Formats.Mz)]
            public FormattableList<double> Masses { get { return _mzsOrMasses; } }
            [Format(Formats.Mz)]
            public double AverageMass { get { return _averageMzOrMass; } }
            [Format(Formats.Mz)]
            public double? MonoisotopicMass { get { return _monoMzOrMass; } }
        }
    }
}
