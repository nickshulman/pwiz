using System.Collections.Generic;
using System.Linq;
using SkydbStorage.DataAccess.Orm;
using SkydbStorage.Internal.Orm;
using SkylineApi;

namespace SkydbStorage.SkylineDocument
{
    public class ChromatogramGroupDataImpl : IChromatogramGroupData
    {
        private List<CandidatePeakGroupImpl> _candidatePeakGroups;
        public ChromatogramGroupDataImpl(ChromatogramGroupImpl chromatogramGroup)
        {
            ChromatogramGroup = chromatogramGroup;
        }

        public ChromatogramGroupImpl ChromatogramGroup
        {
            get;
        }

        public InterpolationParameters InterpolationParameters
        {
            get
            {
                return ChromatogramGroup.ChromatogramGroup.InterpolationParameters;
            }
        }

        public IEnumerable<ICandidatePeakGroup> CandidatePeakGroups
        {
            get
            {
                if (_candidatePeakGroups == null)
                {
                    var document = ChromatogramGroup.Document;
                    var candidatePeakGroupEntities = document.SelectWhere<CandidatePeakGroup>(
                        nameof(CandidatePeakGroup.ChromatogramGroup), ChromatogramGroup.ChromatogramGroup.Id);
                    var candidatePeakEntities = document.SelectWhereIn<CandidatePeak>(
                            nameof(CandidatePeak.CandidatePeakGroup),
                            candidatePeakGroupEntities.Select(peakGroup => peakGroup.Id))
                        .ToLookup(peak => peak.CandidatePeakGroup);
                    _candidatePeakGroups = candidatePeakGroupEntities.Select(entity =>
                        new CandidatePeakGroupImpl(this, entity, candidatePeakEntities[entity.Id.Value])).ToList();
                }

                return _candidatePeakGroups;
            }
        }

        public IList<IChromatogramData> ChromatogramDatas
        {
            get
            {
                return ChromatogramGroup.ChromatogramImpls.Select(c => (IChromatogramData) new ChromatogramDataImpl(this, c)).ToList();
            }
        }
    }
}
