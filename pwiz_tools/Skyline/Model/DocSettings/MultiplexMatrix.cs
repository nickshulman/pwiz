using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Models.Regression.Linear;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.DocSettings
{
    [XmlRoot("multiplex")]
    public class MultiplexMatrix : XmlNamedElement, IValidating
    {
        public static readonly MultiplexMatrix NONE =
            new MultiplexMatrix(NAME_INTERNAL, ImmutableList.Empty<Replicate>());
        public MultiplexMatrix(string name, IEnumerable<Replicate> replicates) : base(name)
        {
            Replicates = ImmutableList.ValueOf(replicates);
            Validate();
        }
        private MultiplexMatrix()
        {
        }
        enum EL
        {
            replicate,
            ion
        }

        enum ATTR
        {
            name,
            mz,
            weight
        }
        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);
            var el = (XElement)XNode.ReadFrom(reader);
            var replicates = new List<Replicate>();
            foreach (var elReplicate in el.Elements(EL.replicate.ToString()))
            {
                string replicateName = elReplicate.Attribute(ATTR.name)?.Value;
                var ionWeights = new List<Weighting>();
                foreach (var elIon in elReplicate.Elements(EL.ion))
                {
                    var mz = elIon.GetNullableDouble(ATTR.mz).GetValueOrDefault();
                    var weight = elIon.GetNullableDouble(ATTR.weight).GetValueOrDefault();
                    string name = elIon.Attribute(ATTR.name)?.Value ?? string.Empty;
                    if (weight != 0)
                    {
                        ionWeights.Add(new Weighting(name, mz, weight));
                    }
                }
                replicates.Add(new Replicate(replicateName, ionWeights));
            }

            Replicates = ImmutableList.ValueOf(replicates);
            Validate();
        }

        public override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);
            foreach (var replicate in Replicates)
            {
                writer.WriteStartElement(EL.replicate);
                writer.WriteAttribute(ATTR.name, replicate.Name);
                foreach (var weight in replicate.Weights)
                {
                    writer.WriteStartElement(EL.ion);
                    writer.WriteAttribute(ATTR.name, weight.Name);
                    writer.WriteAttribute(ATTR.mz, weight.Mz);
                    writer.WriteAttribute(ATTR.weight, weight.Weight);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        public ImmutableList<Replicate> Replicates { get; private set; }

        public int GetReplicateIndex(string name)
        {
            return Replicates.IndexOf(replicate => replicate.Name == name);
        }

        public List<TransitionDocNode> GetAssociatedTransitions(TransitionGroupDocNode transitionGroupDocNode)
        {
            return Replicates.Select(replicate => GetAssociatedTransition(replicate, transitionGroupDocNode)).ToList();
        }

        public TransitionDocNode GetAssociatedTransition(Replicate replicate,
            TransitionGroupDocNode transitionGroupDocNode)
        {
            foreach (var weight in replicate.Weights.OrderByDescending(weight => weight.Weight))
            {
                var transition = transitionGroupDocNode.Transitions.FirstOrDefault(t => t.CustomIon?.Name == weight.Name);
                if (transition != null)
                {
                    return transition;
                }
            }

            return null;
        }

        public class Replicate : Immutable
        {
            public Replicate(string name, IEnumerable<Weighting> weights)
            {
                Name = name;
                Weights = ImmutableList.ValueOf(weights);
            }
            public string Name { get; }
            public ImmutableList<Weighting> Weights { get; private set; }

            protected bool Equals(Replicate other)
            {
                return Name == other.Name && Weights.Equals(other.Weights);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Replicate)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Name.GetHashCode() * 397) ^ Weights.GetHashCode();
                }
            }

            public double GetWeight(ReporterIon reporterIon)
            {
                double weight = 0;
                foreach (var weighting in Weights)
                {
                    if (weighting.Overlaps(reporterIon))
                    {
                        weight += weighting.Weight;
                    }
                }

                return weight;
            }
        }

        public void Validate()
        {
            var replicateNames = new HashSet<string>();
            foreach (var replicate in Replicates)
            {
                if (string.IsNullOrEmpty(replicate.Name))
                {
                    string message = string.Format("Missing replicate name in multiplex matrix '{0}'", Name);
                    throw new InvalidDataException(message);
                }

                if (!replicateNames.Add(replicate.Name))
                {
                    throw new InvalidDataException(
                        string.Format("Duplicate replicate name '{0}' in multiplex matrix '{1}'", replicate.Name,
                            Name));
                }
                var weightNames = new HashSet<string>();
                foreach (var weighting in replicate.Weights)
                {
                    string ionName = weighting.Name;
                    if (string.IsNullOrEmpty(ionName))
                    {
                        string message = string.Format("Missing ion name in replicate '{0}' of multiplex matrix '{1}'",
                            replicate.Name, Name);
                        throw new InvalidDataException(message);
                    }

                    if (!weightNames.Add(ionName))
                    {
                        string message =
                            string.Format("Duplicate ion name '{0}' in replicate '{1}' of multiplex matrix '{2}'", ionName, replicate.Name, Name);
                        throw new InvalidDataException(message);
                    }
                }
            }
        }

        protected bool Equals(MultiplexMatrix other)
        {
            return base.Equals(other) && Equals(Replicates, other.Replicates);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MultiplexMatrix)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Replicates.GetHashCode();
            }
        }

        public static MultiplexMatrix Deserialize(XmlReader reader)
        {
            var matrix = new MultiplexMatrix();
            matrix.ReadXml(reader);
            return matrix;
        }

        public double[] GetMultiplexAreas(Dictionary<ReporterIon, double> observedAreas)
        {
            return GetMultiplexAreaLists(observedAreas.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => new[] { kvp.Value }), 1)
                .FirstOrDefault();
        }

        public Dictionary<string, TimeIntensities> GetMultiplexChromatograms(Dictionary<ReporterIon, TimeIntensities> reporterIonChromatograms)
        {
            var timeIntensitiesList = IsotopeDeconvoluter.MergeTimes(reporterIonChromatograms.Values);
            int iEntry = 0;
            var observedAreaLists = new Dictionary<ReporterIon, double[]>();
            var firstTimeIntensities = timeIntensitiesList[0];
            foreach (var entry in reporterIonChromatograms)
            {
                var timeIntensities = timeIntensitiesList[iEntry++];
                var intensityDoubles = timeIntensities.Intensities.Select(value => (double)value).ToArray();
                Assume.AreEqual(firstTimeIntensities.NumPoints, intensityDoubles.Length);
                observedAreaLists.Add(entry.Key, intensityDoubles);
            }
            var multiplexAreaLists = GetMultiplexAreaLists(observedAreaLists, firstTimeIntensities.NumPoints);
            var multiplexTimeIntensities = new Dictionary<string, TimeIntensities>();
            for (int iReplicate = 0; iReplicate < Replicates.Count; iReplicate++)
            {
                var timeIntensities = new TimeIntensities(firstTimeIntensities.Times,
                    multiplexAreaLists.Select(list => (float)list[iReplicate]).ToList(), null, firstTimeIntensities.ScanIds);
                multiplexTimeIntensities.Add(Replicates[iReplicate].Name, timeIntensities);
            }

            return multiplexTimeIntensities;
        }

        private IList<double[]> GetMultiplexAreaLists(Dictionary<ReporterIon, double[]> observedAreaLists, int observationCount)
        {
            var reporterIonIndexes = MakeIndexDictionary(observedAreaLists.Keys.Where(reporterIon =>
                Replicates.SelectMany(replicate => replicate.Weights).Any(reporterIon.Overlaps)));
            if (reporterIonIndexes.Count == 0)
            {
                return Array.Empty<double[]>();
            }

            var observedVectors = Enumerable.Range(0, observationCount)
                .Select(i => new double[reporterIonIndexes.Count]).ToList();
            foreach (var kvp in observedAreaLists)
            {
                if (reporterIonIndexes.TryGetValue(kvp.Key, out int index))
                {
                    int observationIndex = 0;
                    foreach (var value in kvp.Value)
                    {
                        observedVectors[observationIndex++][index] += value;
                    }
                }
            }

            var inputs = Enumerable.Range(0, reporterIonIndexes.Count)
                .Select(i => new double[Replicates.Count]).ToArray();
            for (int iReplicate = 0; iReplicate < Replicates.Count; iReplicate++)
            {
                foreach (var weight in Replicates[iReplicate].Weights)
                {
                    foreach (var reporterIonIndex in reporterIonIndexes)
                    {
                        if (reporterIonIndex.Key.Overlaps(weight))
                        {
                            inputs[reporterIonIndex.Value][iReplicate] += weight.Weight;
                        }
                    }
                }
            }

            var areaLists = new List<double[]>();
            for (int i = 0; i < observationCount; i++)
            {
                var nonNegativeLeastSquares = new NonNegativeLeastSquares
                {
                    MaxIterations = 100
                };
                MultipleLinearRegression regression = nonNegativeLeastSquares.Learn(inputs, observedVectors[i]);
                areaLists.Add(regression.Weights);
            }

            return areaLists;
        }

        private Dictionary<T, int> MakeIndexDictionary<T>(IEnumerable<T> names)
        {
            var dictionary = new Dictionary<T, int>();
            foreach (var name in names)
            {
                if (!dictionary.ContainsKey(name))
                {
                    dictionary.Add(name, dictionary.Count);
                }
            }
            return dictionary;
        }

        public class Weighting
        {
            public Weighting(string name, double mz, double weight)
            {
                Name = name;
                Mz = mz;
                Weight = weight;
            }
            public string Name { get; }
            public double? Mz { get; }
            public double Weight { get; }

            protected bool Equals(Weighting other)
            {
                return Name == other.Name && Mz.Equals(other.Mz) && Weight.Equals(other.Weight);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Weighting)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Name?.GetHashCode() ?? 0;
                    hashCode = (hashCode * 397) ^ Mz.GetHashCode();
                    hashCode = (hashCode * 397) ^ Weight.GetHashCode();
                    return hashCode;
                }
            }

            public bool Overlaps(ReporterIon reporterIon)
            {
                if (Name != null && Name == reporterIon.Name) 
                    return true;
                if (Mz.HasValue && reporterIon.MzMin <= Mz && reporterIon.MzMax >= Mz)
                    return true;
                return false;
            }
        }

        public class ReporterIon
        {
            public ReporterIon(string name, double? mzMin, double? mzMax)
            {
                Name = name;
                MzMin = mzMin;
                MzMax = mzMax;
            }

            public string Name { get; }
            public double? MzMin { get; }
            public double? MzMax { get; }

            protected bool Equals(ReporterIon other)
            {
                return Name == other.Name && Nullable.Equals(MzMin, other.MzMin) && Nullable.Equals(MzMax, other.MzMax);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((ReporterIon)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Name != null ? Name.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ MzMin.GetHashCode();
                    hashCode = (hashCode * 397) ^ MzMax.GetHashCode();
                    return hashCode;
                }
            }

            public bool Overlaps(Weighting weighting)
            {
                if (Name != null && Name == weighting.Name)
                    return true;
                if (weighting.Mz.HasValue && MzMin <= weighting.Mz && MzMax >= weighting.Mz)
                    return true;
                return false;
            }

        }

        public static ReporterIon GetReporterIon(SrmSettings settings, TransitionDocNode nodeTran)
        {
            var name = nodeTran.CustomIon?.Name;
            if (name == null)
            {
                return null;
            }
            double? mzMin = null, mzMax = null;
            var fullScan = settings.TransitionSettings.FullScan;
            if (fullScan.IsEnabled)
            {
                double filterWindow;
                if (nodeTran.IsMs1 && fullScan.IsEnabledMs)
                {
                    filterWindow = fullScan.GetPrecursorFilterWindow(nodeTran.Mz);
                }
                else
                {
                    filterWindow = fullScan.GetProductFilterWindow(nodeTran.Mz);
                }

                mzMin = nodeTran.Mz - filterWindow / 2;
                mzMax = nodeTran.Mz + filterWindow / 2;
            }

            return new ReporterIon(name, mzMin, mzMax);
        }
    }
}
