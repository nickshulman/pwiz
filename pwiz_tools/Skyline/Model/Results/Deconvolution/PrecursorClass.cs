namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class PrecursorClass
    {
        public PrecursorClass(int labelCount)
        {
            LabelCount = labelCount;
        }

        public int LabelCount { get; private set; }

        protected bool Equals(PrecursorClass other)
        {
            return LabelCount == other.LabelCount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PrecursorClass) obj);
        }

        public override int GetHashCode()
        {
            return LabelCount;
        }

        public override string ToString()
        {
            return "Label" + LabelCount;
        }
    }
}
