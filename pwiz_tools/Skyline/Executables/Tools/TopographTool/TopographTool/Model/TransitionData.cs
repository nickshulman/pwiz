
namespace TopographTool.Model
{
    public class TransitionData
    {
        public TransitionData(Peptide peptide, Precursor precursor, Transition transition)
        {
            Peptide = peptide;
            Precursor = precursor;
            Transition = transition;
        }

        public Peptide Peptide { get; private set; }
        public Precursor Precursor { get; private set; }
        public Transition Transition { get; private set; }

        public TransitionKey TransitionKey
        {
            get
            {
                return new TransitionKey(Precursor.PrecursorCharge, Transition.ProductCharge, Transition.FragmentIon);
            }
        }

        public int LabelCount { get { return Peptide.GetLabelCount(Precursor); } }

        protected bool Equals(TransitionData other)
        {
            return Peptide.Equals(other.Peptide) && Precursor.Equals(other.Precursor) && Transition.Equals(other.Transition);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TransitionData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Peptide.GetHashCode();
                hashCode = (hashCode * 397) ^ Precursor.GetHashCode();
                hashCode = (hashCode * 397) ^ Transition.GetHashCode();
                return hashCode;
            }
        }
    }
}
