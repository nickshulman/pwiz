using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pwiz.Common.Chemistry;
using pwiz.Common.SystemUtil;

namespace TopographTool.Model.DataRows
{
    public class TransitionRow : Immutable
    {
        public string ProteinLocator { get; private set; }
        public string Protein { get; private set; }
        public string PeptideModifiedSequenceFullNames { get; private set; }
        public string ModifiedSequenceFullNames { get; private set; }
        public int PrecursorCharge { get; private set; }
        public double PrecursorMz {  get; private set; }
        public string PrecursorMzs { get; private set; }
        public string PrecursorAbundances { get; private set; }
        public string TransitionLocator { get; private set; }
        public int ProductCharge { get; private set; }
        public double ProductMz { get; private set; }
        public string ProductIonMzs { get; private set; }
        public string ProductIonAbundances { get; private set; }
        public string ComplementaryProductMasses { get; private set; }
        public string ComplementaryProductAbundances { get; private set; }
        public string FragmentIon { get; private set; }
    }
}
