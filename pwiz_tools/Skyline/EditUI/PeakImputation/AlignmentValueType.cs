using System;

namespace pwiz.Skyline.EditUI.PeakImputation
{
    public class AlignmentValueType
    {
        private Func<string> _getNameFunc;
        private AlignmentValueType(Func<string> getNameFunc)
        {
            _getNameFunc = getNameFunc;
        }

        public override string ToString()
        {
            return _getNameFunc();
        }

        public static readonly AlignmentValueType PEAK_APEXES = new AlignmentValueType(() => "Peak Apexes");

        public static readonly AlignmentValueType MS2_IDENTIFICATIONS =
            new AlignmentValueType(() => "MS/MS Identifications");
    }

}
