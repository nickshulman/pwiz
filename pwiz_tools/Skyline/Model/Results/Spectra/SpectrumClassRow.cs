﻿using System.Collections.Generic;

namespace pwiz.Skyline.Model.Results.Spectra
{
    public class SpectrumClassRow
    {
        public SpectrumClassRow(SpectrumClass spectrumClass)
        {
            Properties = spectrumClass;
            Files = new Dictionary<string, FileSpectrumInfo>();
        }

        public SpectrumClass Properties { get; }
        public Dictionary<string, FileSpectrumInfo> Files { get; }

    }
}
