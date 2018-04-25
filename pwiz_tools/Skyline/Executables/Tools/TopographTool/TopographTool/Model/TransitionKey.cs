using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    public class TransitionKey : Immutable
    {
        public TransitionKey(int precursorCharge, int productCharge, string fragmentIon)
        {
            PrecursorCharge = precursorCharge;
            ProductCharge = productCharge;
            FragmentIon = fragmentIon;
        }

        public int PrecursorCharge { get; private set; }
        public int ProductCharge { get; private set; }
        public string FragmentIon { get; private set; }

        public override string ToString()
        {
            return ChargeToString(PrecursorCharge) + "/" + FragmentIon + ChargeToString(ProductCharge);
        }

        private static string ChargeToString(int charge)
        {
            if (charge > 0)
            {
                return "+" + charge;
            }
            return charge.ToString();
        }

        protected bool Equals(TransitionKey other)
        {
            return PrecursorCharge == other.PrecursorCharge && ProductCharge == other.ProductCharge &&
                   string.Equals(FragmentIon, other.FragmentIon);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TransitionKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PrecursorCharge;
                hashCode = (hashCode * 397) ^ ProductCharge;
                hashCode = (hashCode * 397) ^ FragmentIon.GetHashCode();
                return hashCode;
            }
        }

        public bool Matches(Transition transition)
        {
            return Equals(ProductCharge, transition.ProductCharge) 
                && Equals(FragmentIon, transition.FragmentIon);
        }
    }
}
