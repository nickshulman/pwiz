using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Skyline.Model.XCorr
{
    public class SparseIndexMap : Dictionary<int, Peak>
    {
        public SparseIndexMap()
        {

        }

        public SparseIndexMap(int capacity)
        {

        }
        public void PutIfGreater(int key, double mass, double intensity)
        {
            Peak existing;
            if (TryGetValue(key, out existing))
            {
                if (intensity < existing.Intensity)
                {
                    return;
                }
            }

            this[key] = new Peak(mass, intensity);
        }

        public void AdjustOrPutValue(int key, double mass, double intensity)
        {
            this[key] = new Peak(mass, intensity);
        }

        public void multiplyAllValues(double value)
        {
            foreach (var entry in this.ToArray())
            {
                this[entry.Key] = new Peak(entry.Value.Mass, entry.Value.Intensity * value);
            }
        }
    }
}
