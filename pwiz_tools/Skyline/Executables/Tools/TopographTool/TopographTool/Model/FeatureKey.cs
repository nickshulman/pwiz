namespace TopographTool.Model
{
    public class FeatureKey
    {
        public FeatureKey(IsolationWindow? window, double mz)
        {
            Window = window;
            Mz = mz;
        }

        public IsolationWindow? Window { get; private set; }
        public double Mz { get; private set; }

        protected bool Equals(FeatureKey other)
        {
            return Window.Equals(other.Window) &&
                   Mz.Equals(other.Mz);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FeatureKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Mz.GetHashCode();
                hashCode = (hashCode * 397) ^ Window.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            string str = Mz.ToString("0.0000");
            if (Window.HasValue)
            {
                return Window + "/" + str;
            }
            return str;
        }
    }
}
