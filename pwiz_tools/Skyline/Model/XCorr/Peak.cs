namespace pwiz.Skyline.Model.XCorr
{
    public class Peak
    {
        public Peak(double mass, double intensity)
        {
            Mass = mass;
            Intensity = intensity;
        }
        public double Mass { get; private set; }
        public double Intensity { get; private set; }
    }
}
