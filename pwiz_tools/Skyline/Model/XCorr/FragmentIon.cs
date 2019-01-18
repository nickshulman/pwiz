using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Skyline.Model.XCorr
{
    public class FragmentIon
    {
        public FragmentIon(double mass, int index, IonType type)
        {
            Mass = mass;
            Index = index;
            IonType = type;
        }

        public double Mass { get; }
        public int Index { get; }
        public IonType IonType { get; }
    }
}
