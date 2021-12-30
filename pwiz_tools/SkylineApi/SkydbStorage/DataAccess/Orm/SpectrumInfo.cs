using System.Globalization;
using System.Linq;
using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal.Orm
{
    [Class(Lazy = false)]
    public class SpectrumInfo : Entity<SpectrumInfo>
    {
        [ManyToOne(ClassType = typeof(ExtractedFile))]
        public long File { get; set; }
        [Property]
        public int SpectrumIndex { get; set; }
        [Property]
        public int? IdPart1 { get; set; }
        [Property]
        public int? IdPart2 { get; set; }
        [Property]
        public int? IdPart3 { get; set; }
        [Property]
        public int? IdPart4 { get; set; }
        [Property]
        public string SpectrumIdentifierText { get; set; }
        [Property]
        public double RetentionTime { get; set; }

        public string GetSpectrumIdentifier()
        {
            if (null != SpectrumIdentifierText)
            {
                return SpectrumIdentifierText;
            }

            return string.Join(".",
                new[] {IdPart1, IdPart2, IdPart3, IdPart4}.Where(part => part.HasValue)
                    .Select(part => part.Value.ToString(CultureInfo.InvariantCulture)));
        }

        public void SetSpectrumIdentifier(string spectrumIdentifier)
        {
            IdPart1 = IdPart2 = IdPart3 = IdPart4 = null;
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
            IdPart1 = partInts[0];
            IdPart2 = partInts[1];
            IdPart3 = partInts[2];
            IdPart4 = partInts[3];
            return true;
        }
    }
}
