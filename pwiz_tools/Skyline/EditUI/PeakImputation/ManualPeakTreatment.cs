using System;

namespace pwiz.Skyline.EditUI.PeakImputation
{
    public class ManualPeakTreatment
    {
        private Func<string> _getLabelFunc;
        private ManualPeakTreatment(Func<string> getLabelFunc)
        {
            _getLabelFunc = getLabelFunc;
        }

        public override string ToString()
        {
            return _getLabelFunc();
        }

        public static readonly ManualPeakTreatment SKIP = new ManualPeakTreatment(() => "Skip");
        public static readonly ManualPeakTreatment ACCEPT = new ManualPeakTreatment(() => "Always Accept");
        public static readonly ManualPeakTreatment OVERWRITE = new ManualPeakTreatment(() => "Overwrite");
    }
}
