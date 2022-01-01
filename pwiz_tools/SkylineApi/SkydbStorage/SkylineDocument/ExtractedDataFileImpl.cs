using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using SkydbStorage.DataAccess;
using SkydbStorage.DataAccess.Orm;
using SkydbStorage.Internal.Orm;
using SkylineApi;
using InstrumentInfo = SkydbStorage.Internal.Orm.InstrumentInfo;

namespace SkydbStorage.SkylineDocument
{
    public class ExtractedDataFileImpl : IExtractedDataFile
    {
        private IDictionary<int, SpectrumInfo> _spectrumInfos;
        public ExtractedDataFileImpl(SkylineDocumentImpl document, ExtractedFile entity)
        {
            Document = document;
            Entity = entity;
            ChromatogramGroups = new List<ChromatogramGroupImpl>();
            //     groups.Select(group => new ChromatogramGroupImpl(this, group)).ToList();
            // _spectrumInfos = spectrumInfos.ToDictionary(spectrumInfo => spectrumInfo.SpectrumIndex);
        }

        public ExtractedFile Entity { get; }

        public SkylineDocumentImpl Document { get; }

        public string SourceFilePath => Entity.FilePath;

        IEnumerable<IChromatogramGroup> IExtractedDataFile.ChromatogramGroups
        {
            get { return ChromatogramGroups; }
        }

        public List<ChromatogramGroupImpl> ChromatogramGroups { get; }

        public IEnumerable<string> ScoreNames => Document.SkydbSchema.ScoreNames;

        public DateTime? LastWriteTime => Entity.LastWriteTime;

        public bool HasCombinedIonMobility => Entity.HasCombinedIonMobility;

        public bool Ms1Centroid => Entity.Ms1Centroid;

        public bool Ms2Centroid => Entity.Ms2Centroid;

        public DateTime? RunStartTime => Entity.RunStartTime;

        public double? MaxRetentionTime => Entity.MaxRetentionTime;

        public double? MaxIntensity => Entity.MaxIntensity;

        public double? TotalIonCurrentArea => Entity.TotalIonCurrentArea;

        public string SampleId => Entity.SampleId;

        public string InstrumentSerialNumber => Entity.InstrumentSerialNumber;

        public IEnumerable<SkylineApi.InstrumentInfo> InstrumentInfos => new SkylineApi.InstrumentInfo[0];

        public SpectrumInfo GetSpectrumInfo(int index)
        {
            if (_spectrumInfos == null)
            {
                _spectrumInfos = Document.SelectWhere<SpectrumInfo>(nameof(SpectrumInfo.File), Entity.Id)
                    .ToDictionary(spectrum => spectrum.SpectrumIndex);
            }

            return _spectrumInfos[index];
        }
    }
}
