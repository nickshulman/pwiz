using System;
using System.Collections.Generic;
using System.Linq;
using SkydbStorage.DataAccess;
using SkydbStorage.DataAccess.Orm;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.SkylineDocument
{
    public class Precursor
    {
        private List<long> _chromatogramGroupIds;
        private Dictionary<long, ChromatogramGroup> _chromatogramGroups;
        private Dictionary<long, IList<Chromatogram>> _chromatograms;
        private Precursor(SkylineDocumentImpl document, string textId, double precursorMz, IEnumerable<long> groupIds)
        {
            Document = document;
            TextId = textId;
            PrecursorMz = precursorMz;
            _chromatogramGroupIds = groupIds.ToList();
        }

        public static void CreatePrecursor(SkylineDocumentImpl document, string textId, double precursorMz,
            ICollection<Tuple<ExtractedDataFileImpl, long>> chromatogramGroupIds)
        {
            var precursor = new Precursor(document, textId, precursorMz,
                chromatogramGroupIds.Select(tuple => tuple.Item2));
            foreach (var tuple in chromatogramGroupIds)
            {
                tuple.Item1.ChromatogramGroups.Add(new ChromatogramGroupImpl(tuple.Item1, precursor, tuple.Item2));
            }
        }

        public SkylineDocumentImpl Document { get; }
        public string TextId { get; }
        public double PrecursorMz { get; }

        public ChromatogramGroup GetChromatogramGroup(long id)
        {
            LoadChromatogramGroups();
            return _chromatogramGroups[id];
        }

        public IList<Chromatogram> GetChromatograms(long id)
        {
            LoadChromatogramGroups();
            IList<Chromatogram> list;
            _chromatograms.TryGetValue(id, out list);
            return list ?? Array.Empty<Chromatogram>();
        }

        private void LoadChromatogramGroups()
        {
            if (_chromatogramGroups != null && _chromatograms != null)
            {
                return;
            }

            using (var connection = SkydbConnection.OpenFile(Document.Path))
            {
                if (_chromatogramGroups == null)
                {
                    using (var statement = new SelectStatement<ChromatogramGroup>(connection))
                    {
                        _chromatogramGroups = statement.SelectWhereIn(nameof(Entity.Id), _chromatogramGroupIds)
                            .ToDictionary(group => group.Id.Value);
                    }
                }

                if (_chromatograms == null)
                {
                    _chromatograms = new Dictionary<long, IList<Chromatogram>>();
                    using (var statement = new SelectStatement<Chromatogram>(connection))
                    {
                        foreach (var grouping in statement.SelectWhereIn(nameof(Chromatogram.ChromatogramGroup),
                            _chromatogramGroupIds).GroupBy(chrom=>chrom.ChromatogramGroup))
                        {
                            _chromatograms.Add(grouping.Key, grouping.OrderBy(c=>c.ProductMz).ToList());
                        }
                    }
                }
            }
        }
    }
}
