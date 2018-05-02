using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using TopographTool.Model.DataRows;

namespace TopographTool.Model
{
    public class Transition : Immutable
    {
        public Transition(TransitionRow resultRow, IEnumerable<TransitionResult> results)
        {
            FragmentIon = resultRow.FragmentIon;
            TransitionLocator = resultRow.TransitionLocator;
            ProductCharge = resultRow.ProductCharge;
            ProductMz = resultRow.ProductMz;
            Results = ImmutableList.ValueOf(results);
            MzDistribution = RowReader.ParseMassDistribution(resultRow.ProductIonMzs, resultRow.ProductIonAbundances);
            ComplementaryFragmentDistribution = RowReader.ParseMassDistribution(
                resultRow.ComplementaryProductMasses,
                resultRow.ComplementaryProductAbundances);
        }

        public string FragmentIon { get; private set; }
        public int ProductCharge { get; private set; }
        public double ProductMz { get; private set; }
        public string TransitionLocator { get; private set; }
        public MassDistribution MzDistribution { get; private set; }
        public MassDistribution ComplementaryFragmentDistribution { get; private set; }

        public ImmutableList<TransitionResult> Results { get; private set; }

        public TransitionResult GetResult(ResultFile resultFile)
        {
            return Results.FirstOrDefault(r => Equals(r.ResultFile, resultFile));
        }
    }
}
