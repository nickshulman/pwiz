using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    public class Transition : Immutable
    {
        public Transition()
        {
            
        }

        public Transition(ResultRow resultRow, IEnumerable<TransitionResult> results)
        {
            FragmentIon = resultRow.FragmentIon;
            TransitionLocator = resultRow.TransitionLocator;
            ProductCharge = resultRow.ProductCharge;
            ProductMz = resultRow.ProductMz;
            Results = ImmutableList.ValueOf(results);
        }

        public string FragmentIon { get; private set; }
        public int ProductCharge { get; private set; }
        public double ProductMz { get; private set; }
        public string TransitionLocator { get; private set; }
        public ImmutableList<TransitionResult> Results { get; private set; }

        public TransitionResult GetResult(Replicate replicate)
        {
            return Results.FirstOrDefault(r => Equals(r.Replicate, replicate));
        }
    }
}
