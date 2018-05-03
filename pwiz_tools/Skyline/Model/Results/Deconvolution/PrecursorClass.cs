using System;

namespace pwiz.Skyline.Model.Results.Deconvolution
{
    public class PrecursorClass : IComparable<PrecursorClass>
    {
        public PrecursorClass(double neutralMass)
        {
            NeutralMass = neutralMass;
        }

        public double NeutralMass { get; private set; }

        protected bool Equals(PrecursorClass other)
        {
            return NeutralMass == other.NeutralMass;
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
            return NeutralMass.GetHashCode();
        }

        public override string ToString()
        {
            return "Mass:" + NeutralMass;
        }

        public int CompareTo(PrecursorClass other)
        {
            if (other == null)
            {
                return 1;
            }
            return NeutralMass.CompareTo(other.NeutralMass);
        }
    }
}
