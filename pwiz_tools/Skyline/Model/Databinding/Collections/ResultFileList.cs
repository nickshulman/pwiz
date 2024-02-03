using System.Collections;
using pwiz.Skyline.Model.Databinding.Entities;

namespace pwiz.Skyline.Model.Databinding.Collections
{
    public class ResultFileList : SkylineObjectList<ResultFile>
    {
        public ResultFileList(SkylineDataSchema dataSchema) : base(dataSchema) { }
        public override IEnumerable GetItems()
        {
            if (!SrmDocument.Settings.HasResults)
            {
                yield break;
            }

            var measuredResults = SrmDocument.Settings.MeasuredResults;
            for (int iReplicate = 0; iReplicate < measuredResults.Chromatograms.Count; iReplicate++)
            {
                var replicate = new Replicate(DataSchema, iReplicate);
                var chromatogramSet = measuredResults.Chromatograms[iReplicate];
                foreach (var msDataFileInfo in chromatogramSet.MSDataFileInfos)
                {
                    yield return new ResultFile(replicate, msDataFileInfo.FileId, 0);
                }
            }
        }
    }
}
