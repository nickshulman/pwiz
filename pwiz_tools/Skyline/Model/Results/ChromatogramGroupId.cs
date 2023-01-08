﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using MathNet.Numerics.LinearAlgebra.Complex.Solvers;
using pwiz.Common.DataBinding;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Results.Legacy;
using pwiz.Skyline.Model.Results.ProtoBuf;
using pwiz.Skyline.Model.Results.Spectra;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results
{
    public class ChromatogramGroupId : Immutable
    {
        private ChromatogramGroupId(Target target, string qcTraceName, SpectrumClassFilter spectrumClassFilter)
        {
            Target = target;
            QcTraceName = qcTraceName;
            SpectrumClassFilter = spectrumClassFilter;
        }

        public static ChromatogramGroupId ForTarget(Target target)
        {
            if (target == null)
            {
                return null;
            }

            return new ChromatogramGroupId(target, null, null);
        }

        public static ChromatogramGroupId ForQcTraceName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            return new ChromatogramGroupId(null, name, null);
        }

        public ChromatogramGroupId(Target target, SpectrumClassFilter spectrumClassFilter)
        {
            Target = target;
            SpectrumClassFilter = spectrumClassFilter;
        }

        public Target Target { get; }
        public string QcTraceName { get; }
        public SpectrumClassFilter SpectrumClassFilter { get; private set; }

        public ChromatogramGroupId ChangeSpectrumClassFilter(SpectrumClassFilter spectrumClassFilter)
        {
            if (false == spectrumClassFilter?.IsEmpty)
            {
                spectrumClassFilter = null;
            }
            if (ReferenceEquals(spectrumClassFilter, SpectrumClassFilter))
            {
                return this;
            }

            if (Target == null && spectrumClassFilter != null)
            {
                throw new InvalidOperationException();
            }

            return ChangeProp(ImClone(this), im => im.SpectrumClassFilter = spectrumClassFilter);
        }

        protected bool Equals(ChromatogramGroupId other)
        {
            return Equals(Target, other.Target) && QcTraceName == other.QcTraceName && Equals(SpectrumClassFilter, other.SpectrumClassFilter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ChromatogramGroupId) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Target != null ? Target.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (QcTraceName != null ? QcTraceName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SpectrumClassFilter != null ? SpectrumClassFilter.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static ChromatogramGroupIdsProto ToProto(IEnumerable<ChromatogramGroupId> ids)
        {
            var targets = new DistinctList<Target> {null};
            var filters = new DistinctList<SpectrumClassFilter> {null};
            var idsProto = new ChromatogramGroupIdsProto();
            foreach (var id in ids)
            {
                idsProto.ChromatogramGroupIds.Add(new ChromatogramGroupIdsProto.Types.ChromatogramGroupId()
                {
                    TargetIndex = targets.Add(id.Target),
                    FilterIndex = filters.Add(id.SpectrumClassFilter)
                });
            }

            foreach (var target in targets.Skip(1))
            {
                var targetProto = new ChromatogramGroupIdsProto.Types.Target();
                if (target.IsProteomic)
                {
                    targetProto.ModifiedPeptideSequence = target.Sequence;
                }
                else
                {
                    var molecule = target.Molecule;
                    targetProto.Name = molecule.Name;
                    targetProto.Formula = molecule.Formula;
                    targetProto.MonoMass = molecule.MonoisotopicMass;
                    targetProto.AverageMass = molecule.AverageMass;
                    // TODO: Accession numbers
                }

                idsProto.Targets.Add(targetProto);
            }

            foreach (var filter in filters.Skip(1))
            {
                var filterProto = new ChromatogramGroupIdsProto.Types.SpectrumFilter();
                foreach (var filterSpec in filter.FilterSpecs)
                {
                    filterProto.Predicates.Add(new ChromatogramGroupIdsProto.Types.SpectrumFilter.Types.Predicate()
                    {
                        PropertyPath = filterSpec.Column,
                        Operation = _filterOperationReverseMap[filterSpec.Operation],
                        Operand = new Value{StringValue = filterSpec.Predicate.InvariantOperandText}
                    });
                }
                idsProto.Filters.Add(filterProto);
            }

            return idsProto;
        }

        public static IEnumerable<ChromatogramGroupId> FromProto(ChromatogramGroupIdsProto proto)
        {
            var targets = new List<Target>() {null};
            foreach (var targetProto in proto.Targets)
            {
                if (!string.IsNullOrEmpty(targetProto.ModifiedPeptideSequence))
                {
                    targets.Add(new Target(targetProto.ModifiedPeptideSequence));
                }
                else
                {
                    MoleculeAccessionNumbers moleculeAccessionNumbers = MoleculeAccessionNumbers.EMPTY; // TODO
                    targets.Add(new Target(new CustomMolecule(targetProto.Formula, new TypedMass(targetProto.MonoMass, MassType.Monoisotopic),
                        new TypedMass(targetProto.AverageMass, MassType.Average),
                        targetProto.Name, moleculeAccessionNumbers)));
                }
            }

            foreach (var id in proto.ChromatogramGroupIds)
            {
                yield return new ChromatogramGroupId(targets[id.TargetIndex], id.QcTraceName, null);
            }
        }

        private static Dictionary<ChromatogramGroupIdsProto.Types.FilterOperation, IFilterOperation>
            _filterOperationMap = new Dictionary<ChromatogramGroupIdsProto.Types.FilterOperation, IFilterOperation>
            {
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpHasAnyValue, FilterOperations.OP_HAS_ANY_VALUE},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpEquals, FilterOperations.OP_EQUALS},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpNotEquals, FilterOperations.OP_NOT_EQUALS},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpIsBlank, FilterOperations.OP_IS_BLANK},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpIsNotBlank, FilterOperations.OP_IS_NOT_BLANK},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpIsGreaterThan, FilterOperations.OP_IS_GREATER_THAN},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpIsLessThan, FilterOperations.OP_IS_LESS_THAN},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpIsGreaterThanOrEqualTo, FilterOperations.OP_IS_GREATER_THAN_OR_EQUAL},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpIsLessThanOrEqualTo, FilterOperations.OP_IS_GREATER_THAN_OR_EQUAL},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpContains, FilterOperations.OP_CONTAINS},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpNotContains, FilterOperations.OP_NOT_CONTAINS},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FitlerOpStartsWith, FilterOperations.OP_STARTS_WITH},
                {ChromatogramGroupIdsProto.Types.FilterOperation.FilterOpNotStartsWith, FilterOperations.OP_NOT_STARTS_WITH}
            };

        private static readonly Dictionary<IFilterOperation, ChromatogramGroupIdsProto.Types.FilterOperation>
            _filterOperationReverseMap = _filterOperationMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }

    public class ChromatogramGroupIds : IEnumerable<ChromatogramGroupId>
    {
        private List<ChromatogramGroupId> _ids = new List<ChromatogramGroupId>();
        private Dictionary<ChromatogramGroupId, int> _idIndexes = new Dictionary<ChromatogramGroupId, int>();

        public ChromatogramGroupId GetId(int index)
        {
            if (index < 0)
            {
                return null;
            }

            return _ids[index];
        }

        public ChromatogramGroupId GetId(ChromGroupHeaderInfo chromGroupHeaderInfo)
        {
            return GetId(chromGroupHeaderInfo.TextIdIndex);
        }

        public int AddId(ChromatogramGroupId groupId)
        {
            if (groupId == null)
            {
                return -1;
            }
            if (!_idIndexes.TryGetValue(groupId, out int index))
            {
                index = _ids.Count;
                _ids.Add(groupId);
                _idIndexes.Add(groupId, index);
                return index;
            }

            return index;
        }

        public IEnumerable<ChromGroupHeaderInfo> ConvertFromTextIdBytes(byte[] textIdBytes,
            IEnumerable<ChromGroupHeaderInfo16> chromGroupHeaderInfos)
        {
            foreach (var chromGroupHeaderInfo in chromGroupHeaderInfos)
            {
                if (chromGroupHeaderInfo._textIdIndex == -1)
                {
                    yield return new ChromGroupHeaderInfo(chromGroupHeaderInfo, -1);
                    continue;
                }

                var textId = Encoding.UTF8.GetString(textIdBytes,
                    chromGroupHeaderInfo._textIdIndex, chromGroupHeaderInfo._textIdLen);
                ChromatogramGroupId chromatogramGroupId;
                if (0 != (chromGroupHeaderInfo._flagBits & ChromGroupHeaderInfo16.FlagValues.extracted_qc_trace))
                {
                    chromatogramGroupId = ChromatogramGroupId.ForQcTraceName(textId);
                }
                else
                {
                    chromatogramGroupId = ChromatogramGroupId.ForTarget(Target.FromSerializableString(textId));
                }
                int index = AddId(chromatogramGroupId);
                yield return new ChromGroupHeaderInfo(chromGroupHeaderInfo, index);
            }
        }

        public ChromGroupHeaderInfo SetId(ChromGroupHeaderInfo chromGroupHeaderInfo, ChromatogramGroupId id)
        {
            return chromGroupHeaderInfo.ChangeTextIdIndex(AddId(id));
        }

        public ChromGroupHeaderInfo16 ConvertToTextId(List<byte> textIdBytes, Dictionary<Target, TextIdLocation> map,
            ChromGroupHeaderInfo chromGroupHeaderInfo)
        {
            var target = GetId(chromGroupHeaderInfo)?.Target;
            if (target == null)
            {
                return new ChromGroupHeaderInfo16(chromGroupHeaderInfo, -1, 0);
            }
            if (!map.TryGetValue(target, out var textIdLocation))
            {
                int textIdIndex = textIdBytes.Count;
                textIdBytes.AddRange(Encoding.UTF8.GetBytes(target.ToSerializableString()));
                textIdLocation = new TextIdLocation(textIdIndex, textIdBytes.Count - textIdIndex);
                map.Add(target, textIdLocation);
            }

            return new ChromGroupHeaderInfo16(chromGroupHeaderInfo, textIdLocation.Index,
                (ushort) textIdLocation.Length);
        }

        public int Count
        {
            get
            {
                return _ids.Count;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ChromatogramGroupId> GetEnumerator()
        {
            return _ids.GetEnumerator();
        }

        public ChromatogramGroupIdsProto ToProtoMessage()
        {
            var targets = new DistinctList<Target> {null};
            var filters = new DistinctList<SpectrumClassFilter> {null};
            var idsProto = new ChromatogramGroupIdsProto();
            foreach (var id in this)
            {
                idsProto.ChromatogramGroupIds.Add(new ChromatogramGroupIdsProto.Types.ChromatogramGroupId()
                {
                    TargetIndex = targets.Add(id.Target),
                    FilterIndex = filters.Add(id.SpectrumClassFilter)
                });
            }

            foreach (var target in targets.Skip(1))
            {
                var targetProto = new ChromatogramGroupIdsProto.Types.Target();
                if (target.IsProteomic)
                {
                    targetProto.ModifiedPeptideSequence = target.Sequence;
                }
                else
                {
                    var molecule = target.Molecule;
                    targetProto.Name = molecule.Name;
                    targetProto.Formula = molecule.Formula;
                    targetProto.MonoMass = molecule.MonoisotopicMass;
                    targetProto.AverageMass = molecule.AverageMass;
                    // TODO: Accession numbers
                }

                idsProto.Targets.Add(targetProto);
            }

            return idsProto;
        }
    }

    public class TextIdLocation
    {
        public TextIdLocation(int index, int length)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (length < 0 || length > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            Index = index;
            Length = length;
        }

        public int Index { get; }
        public int Length { get; }
    }
}