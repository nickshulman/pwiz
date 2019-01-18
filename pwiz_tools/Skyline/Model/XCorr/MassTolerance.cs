using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Skyline.Model.XCorr
{
    public struct MassTolerance
    {
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
