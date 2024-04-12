using System.Collections.Generic;
using System.Linq;

namespace pwiz.Skyline.Model.Results.Spectra.Alignment
{
    public class SummaryValue
    {
        private double[] _values;
        public SummaryValue(IEnumerable<double> values)
        {
            _values = values.ToArray();
        }

        public double? Score(SummaryValue other)
        {
            return SpectrumSummary.CalculateSimilarityScore(_values, other._values);
        }
    }
}
