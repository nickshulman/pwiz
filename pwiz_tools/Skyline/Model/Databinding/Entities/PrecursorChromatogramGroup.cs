﻿using System;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    public class PrecursorChromatogramGroup : SkylineObject
    {
        private readonly Lazy<ChromatogramGroupInfo> _chromatogramGroupInfo;
        public PrecursorChromatogramGroup(PrecursorResult precursorResult) : base(precursorResult.DataSchema)
        {
            PrecursorResult = precursorResult;
            _chromatogramGroupInfo = new Lazy<ChromatogramGroupInfo>(()=>GetChromatogramGroup(false));
        }

        public PrecursorResult PrecursorResult { get; private set; }
        public ChromatogramGroupInfo ChromatogramGroupInfo { get { return _chromatogramGroupInfo.Value; } }

        public TimeIntensitiesGroup ReadTimeIntensitiesGroup()
        {
            var chromatogramGroupInfo = GetChromatogramGroup(true);
            if (chromatogramGroupInfo == null)
            {
                return null;
            }
            return chromatogramGroupInfo.TimeIntensitiesGroup;
        }

        private ChromatogramGroupInfo GetChromatogramGroup(bool loadPoints)
        {
            return DataSchema.ChromDataCache.GetChromatogramGroupInfo(DataSchema.Document,
                PrecursorResult.GetResultFile().Replicate.ChromatogramSet,
                PrecursorResult.GetResultFile().ChromFileInfo.FilePath, PrecursorResult.Precursor.IdentityPath,
                loadPoints);
        }

        public MsDataFileScanIds ReadMsDataFileScanIds()
        {
            return DataSchema.ChromDataCache.GetScanIds(DataSchema.Document,
                PrecursorResult.GetResultFile().ChromFileInfo.FilePath);
        }
    }
}
