﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.Attributes;

namespace SkydbApi.Orm
{
    [Class]
    public class SpectrumInfo : Entity<SpectrumInfo>
    {
        [ManyToOne(NotFound = NotFoundMode.Ignore)]
        public virtual MsDataFile MsDataFile { get; set; }
        [Property]
        public virtual int SpectrumIndex { get; set; }
        [Property]
        public virtual int? SpectrumId1 { get; set; }
        [Property]
        public virtual int? SpectrumId2 { get; set; }
        [Property]
        public virtual int? SpectrumId3 { get; set; }
        [Property]
        public virtual int? SpectrumId4 { get; set; }
        [Property]
        public virtual string SpectrumIdentifierText { get; set; }
        [Property]
        public virtual double RetentionTime { get; set; }

        public virtual string GetSpectrumIdentifier()
        {
            if (null != SpectrumIdentifierText)
            {
                return SpectrumIdentifierText;
            }

            return string.Join(".",
                new[] {SpectrumId1, SpectrumId2, SpectrumId3, SpectrumId4}.Where(part => part.HasValue)
                    .Select(part => part.Value.ToString(CultureInfo.InvariantCulture)));
        }

        public virtual void SetSpectrumIdentifier(string spectrumIdentifier)
        {
            SpectrumId1 = SpectrumId2 = SpectrumId3 = SpectrumId4 = null;
            SpectrumIdentifierText = null;
            if (spectrumIdentifier != null)
            {
                if (!TrySetSpectrumIdParts(spectrumIdentifier))
                {
                    SpectrumIdentifierText = spectrumIdentifier;
                }
            }
        }

        private bool TrySetSpectrumIdParts(string spectrumIdentifier)
        {
            var parts = spectrumIdentifier.Split('.');
            if (parts.Length > 4)
            {
                return false;
            }

            var partInts = new int?[4];
            for (int iPart = 0; iPart < parts.Length; iPart++)
            {
                string part = parts[iPart];
                if (!int.TryParse(part, out int partInt))
                {
                    return false;
                }

                if (partInt.ToString(CultureInfo.InvariantCulture) != part)
                {
                    return false;
                }

                partInts[iPart] = partInt;
            }
            SpectrumId1 = partInts[0];
            SpectrumId2 = partInts[1];
            SpectrumId3 = partInts[2];
            SpectrumId4 = partInts[3];
            return true;
        }
    }
}
