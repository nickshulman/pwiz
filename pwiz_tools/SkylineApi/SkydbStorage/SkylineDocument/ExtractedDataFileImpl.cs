using System;
using System.Collections.Generic;
using SkydbStorage.Internal.Orm;
using SkylineApi;

namespace SkydbStorage.SkylineDocument
{
    public class ExtractedDataFileImpl : IExtractedDataFile
    {
        public ExtractedDataFileImpl(SkylineDocumentImpl document, ExtractedFile entity)
        {
            Document = document;
            Entity = entity;
        }

        public ExtractedFile Entity { get; }

        public SkylineDocumentImpl Document { get; }

        public string SourceFilePath => Entity.FilePath;

        public IEnumerable<IChromatogramGroup> ChromatogramGroups {
            get
            {
                throw new NotImplementedException();
                // return Document.CallWithConnection(connection =>
                // {
                //     return connection.SelectAll<ChromatogramGroup>().Select(group=>)
                // });
            }
        }

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

        public IEnumerable<SkylineApi.InstrumentInfo> InstrumentInfos => throw new NotImplementedException();
    }
}
