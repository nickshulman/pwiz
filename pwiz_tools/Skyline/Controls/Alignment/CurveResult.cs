using System.ComponentModel;

namespace pwiz.Skyline.Controls.Alignment
{
    public class CurveResult
    {
        public CurveResult(string message)
        {
            Message = message;
        }
        //[ReadOnly(true)]
        public string Message { get; set; }

        //[ReadOnly(true)]
        public int NumberOfPoints { get; set; }

        public AlignmentResult AlignmentResult { get; set; }

        public override string ToString()
        {
            return Message ?? string.Empty;
        }
    }

    public class AlignmentResult
    {
        [ReadOnly(true)]
        public int NumberOfPoints { get; set; }
    }
}
