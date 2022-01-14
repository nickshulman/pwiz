using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Skyline.Model.DocSettings;

namespace pwiz.Skyline.Model
{
    public class ProteinId : Identity
    {
        public ProteinId(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
        public override string ToString()
        {
            return Name;
        }

        protected bool Equals(ProteinId other)
        {
            return base.Equals(other) && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProteinId) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }

    public class ProteinNode : DocNode
    {
        public ProteinNode(ProteinId id) : base(id)
        {
        }

        public ProteinId ProteinId
        {
            get { return (ProteinId) Id; }
        }

        public override AnnotationDef.AnnotationTarget AnnotationTarget
        {
            get { throw new InvalidOperationException(); }
        }

        public IdentityList<PeptideGroup> PeptideGroups { get; private set; }

        public ProteinNode ChangePeptideGroups(IEnumerable<PeptideGroup> peptideGroups)
        {
            return ChangeProp(ImClone(this),
                im => im.PeptideGroups = IdentityList<PeptideGroup>.ValueOf(peptideGroups));
        }
        public string FastaSequence { get; private set; }

        protected bool Equals(ProteinNode other)
        {
            return base.Equals(other) && Equals(PeptideGroups, other.PeptideGroups) && FastaSequence == other.FastaSequence;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ProteinNode) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ PeptideGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ (FastaSequence != null ? FastaSequence.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class ProteinGroupMap
    {
        public static readonly ProteinGroupMap EMPTY =
            new ProteinGroupMap(ImmutableList.Empty<ProteinNode>());
        private static readonly IdentityEqualityComparer<PeptideGroup> _peptideGroupComparer =
            new IdentityEqualityComparer<PeptideGroup>();
        private DocNodeChildren _proteins;
        private ILookup<PeptideGroup, ProteinId> _peptideGroupProteins;

        public IEnumerable<ProteinNode> Proteins
        {
            get
            {
                return _proteins.Cast<ProteinNode>();
            }
        }

        public ProteinNode FindProtein(ProteinId proteinId)
        {
            int index = _proteins.IndexOf(proteinId);
            if (index < 0)
            {
                return null;
            }

            return (ProteinNode)_proteins[index];
        }

        public ProteinGroupMap(IEnumerable<ProteinNode> proteins)
        {
            _proteins = new DocNodeChildren(proteins, null);
            _peptideGroupProteins = Proteins.SelectMany(protein =>
                    protein.PeptideGroups.Select(peptideGroup => Tuple.Create(protein.ProteinId, peptideGroup)))
                .ToLookup(tuple => tuple.Item2, tuple => tuple.Item1, _peptideGroupComparer);
        }

        public IEnumerable<ProteinNode> GetProteins(PeptideGroup peptideGroup)
        {
            return _peptideGroupProteins[peptideGroup].Select(FindProtein);
        }

        public ProteinGroupMap ChangeProteins(IEnumerable<ProteinNode> proteins)
        {
            return new ProteinGroupMap(proteins);
        }

        protected bool Equals(ProteinGroupMap other)
        {
            return _proteins.Equals(other._proteins);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProteinGroupMap) obj);
        }

        public override int GetHashCode()
        {
            return _proteins.GetHashCode();
        }
    }
}
