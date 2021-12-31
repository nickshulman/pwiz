using System.Collections.Generic;
using System.Linq;
using SkydbStorage.DataAccess;
using SkydbStorage.Internal.Orm;
using SkylineApi;

namespace SkydbStorage.SkylineDocument
{
    public class ChromatogramDataImpl : IChromatogramData
    {
        private ChromatogramData _chromatogramData;
        private SpectrumList _spectrumList;

        public ChromatogramDataImpl(ChromatogramGroupDataImpl groupData, ChromatogramImpl chromatogram)
        {
            GroupData = groupData;
            Chromatogram = chromatogram;
        }

        public ChromatogramGroupDataImpl GroupData { get; }
        public ChromatogramImpl Chromatogram { get; }
        public SkylineDocumentImpl Document
        {
            get { return GroupData.ChromatogramGroup.Document; }
        }

        public int NumPoints
        {
            get
            {
                LoadChromatogramData();
                return _chromatogramData.PointCount;
            }
        }

        public IList<float> RetentionTimes
        {
            get
            {
                LoadChromatogramData();
                if (_spectrumList.RetentionTimeBlob != null)
                {
                    return DataUtil.PrimitivesFromByteArray<float>(
                        DataUtil.Uncompress(_spectrumList.RetentionTimeBlob));
                }

                return SpectrumInfos.Select(info => (float)info.RetentionTime).ToList();
            }
        }

        public IList<float> Intensities
        {
            get
            {
                LoadChromatogramData();
                return DataUtil.PrimitivesFromByteArray<float>(DataUtil.Uncompress(_chromatogramData.IntensitiesBlob));
            }
        }

        public IList<float> MassErrors
        {
            get
            {
                LoadChromatogramData();
                if (_chromatogramData.MassErrorsBlob == null)
                {
                    return null;
                }

                return DataUtil.PrimitivesFromByteArray<float>(DataUtil.Uncompress(_chromatogramData.MassErrorsBlob));
            }
        }

        public IEnumerable<SpectrumInfo> SpectrumInfos
        {
            get
            {
                LoadChromatogramData();
                if (_spectrumList.SpectrumIndexBlob == null)
                {
                    return null;
                }
                var file = GroupData.ChromatogramGroup.DataFile;
                var spectrumIndexes =
                    DataUtil.PrimitivesFromByteArray<int>(DataUtil.Uncompress(_spectrumList.SpectrumIndexBlob));
                return spectrumIndexes.Select(file.GetSpectrumInfo);

            }
        }

        public IList<string> SpectrumIdentifiers
        {
            get
            {
                return SpectrumInfos?.Select(info => info.GetSpectrumIdentifier()).ToList();
            }
        }

        private void LoadChromatogramData()
        {
            if (_chromatogramData == null)
            {
                _chromatogramData = Document
                    .SelectWhere<ChromatogramData>(nameof(Entity.Id), Chromatogram.Chromatogram.ChromatogramData).Single();
            }

            if (_spectrumList == null)
            {
                _spectrumList = Document.SelectWhere<SpectrumList>(nameof(Entity.Id), _chromatogramData.SpectrumList)
                    .Single();
            }
        }
    }
}
