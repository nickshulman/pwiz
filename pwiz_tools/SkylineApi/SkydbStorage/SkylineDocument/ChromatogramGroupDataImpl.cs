using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkylineApi;

namespace SkydbStorage.SkylineDocument
{
    public class ChromatogramGroupDataImpl : IChromatogramGroupData
    {
        public ChromatogramGroupDataImpl(ChromatogramGroupImpl chromatogramGroup)
        {
            ChromatogramGroup = chromatogramGroup;
        }

        public ChromatogramGroupImpl ChromatogramGroup
        {
            get;
        }

        public InterpolationParameters InterpolationParameters => null;

        public IEnumerable<ICandidatePeakGroup> CandidatePeakGroups => new List<ICandidatePeakGroup>();

        public IList<IChromatogramData> ChromatogramDatas
        {
            get
            {
                return ChromatogramGroup.ChromatogramImpls.Select(c => (IChromatogramData) new ChromatogramDataImpl(this, c)).ToList();
            }
        }
    }
}
