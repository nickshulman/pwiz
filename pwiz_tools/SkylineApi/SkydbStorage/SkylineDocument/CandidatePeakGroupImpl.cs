using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbStorage.DataAccess.Orm;
using SkydbStorage.Internal.Orm;
using SkylineApi;

namespace SkydbStorage.SkylineDocument
{
    public class CandidatePeakGroupImpl : ICandidatePeakGroup
    {
        public CandidatePeakGroupImpl(ChromatogramGroupDataImpl groupData, CandidatePeakGroup candidatePeakGroup,
            IEnumerable<CandidatePeak> candidatePeaks)
        {
            PeakGroup = candidatePeakGroup;
            var peakDict = candidatePeaks.ToDictionary(peak => peak.Chromatogram);
            var peaks = new List<ICandidatePeak>();
            foreach (var chromatogram in groupData.ChromatogramGroup.ChromatogramImpls)
            {
                CandidatePeakImpl peakImpl = null;
                if (peakDict.TryGetValue(chromatogram.Chromatogram.Id.Value, out CandidatePeak peak))
                {
                    peakImpl = new CandidatePeakImpl(this, peak);
                }
                peaks.Add(peakImpl);
            }

            CandidatePeaks = peaks;
        }

        public CandidatePeakGroup PeakGroup { get; }

        public double? GetScore(string name)
        {
            throw new NotImplementedException();
        }

        public bool IsBestPeak
        {
            get
            {
                return PeakGroup.IsBestPeak;
            }
        }

        public IList<ICandidatePeak> CandidatePeaks
        {
            get;
        }

        public PeakIdentified Identified {
            get
            {
                return (PeakIdentified) PeakGroup.Identified;
            }
        }
    }
}
