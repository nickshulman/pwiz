using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Skyline.Model.XCorr
{
    public struct MassTolerance
    {
        public static MassTolerance WithPpm(double ppm)
        {
            return new MassTolerance {_ppm = ppm};
        }

        public static MassTolerance WithAmu(double amu)
        {
            return new MassTolerance {_amu = amu};
        }
        private double? _ppm;
        private double? _amu;
        public double GetTolerance(double mass)
        {
            if (_ppm.HasValue)
            {
                return mass * _ppm.Value / 1000000;
            }

            return _amu.GetValueOrDefault();
        }
    }
}
