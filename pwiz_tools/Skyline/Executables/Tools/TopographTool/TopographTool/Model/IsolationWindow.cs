using System;

namespace TopographTool.Model
{
    public struct IsolationWindow : IComparable
    {
        public IsolationWindow(double start, double end) : this()
        {
            Start = start;
            End = end;
        }
        public double Start { get; private set; }
        public double End { get; private set; }

        public int CompareTo(object obj)
        {
            if (null == obj)
            {
                return 1;
            }
            var that = (IsolationWindow) obj;
            int result = Start.CompareTo(that.Start);
            if (result == 0)
            {
                result = End.CompareTo(that.End);
            }
            return result;
        }

        public bool Contains(double precursorMz)
        {
            return precursorMz >= Start && precursorMz < End;
        }

        public override string ToString()
        {
            return "[" + Start.ToString("0.0") + "," + End.ToString("0.0") + ")";
        }
    }
}