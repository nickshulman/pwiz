﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Models.Regression.Linear;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings.AbsoluteQuantification;
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
                var ionWeights = new List<KeyValuePair<string, double>>();
                foreach (var elIon in elReplicate.Elements(EL.ion))
                {
                    var weight = elIon.GetNullableDouble(ATTR.weight);
                    if (weight != 0)
                    {
                        ionWeights.Add(new KeyValuePair<string, double>(elIon.Attribute(ATTR.name)?.Value, weight.Value));
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
                    writer.WriteAttribute(ATTR.name, weight.Key);
                    writer.WriteAttribute(ATTR.weight, weight.Value);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        public ImmutableList<Replicate> Replicates { get; private set; }
        
        public class Replicate : Immutable
        {
            public Replicate(string name, IEnumerable<KeyValuePair<string, double>> weights)
            {
                Name = name;
                Weights = ImmutableSortedList.FromValues(weights);
            }
            public string Name { get; private set; }
            public ImmutableSortedList<string, double> Weights { get; private set; }

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
                foreach (var weight in replicate.Weights)
                {
                    string ionName = weight.Key;
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

        public double[] GetReplicateQuantities(Dictionary<string, double> observedAreas)
        {
            var reporterIonIndexes = MakeIndexDictionary(observedAreas.Keys.Intersect(Replicates.SelectMany(replicate=>replicate.Weights.Keys)));
            
            var observedVector = new double[reporterIonIndexes.Count];
            foreach (var kvp in observedAreas)
            {
                if (reporterIonIndexes.TryGetValue(kvp.Key, out int index))
                {
                    observedVector[index] += kvp.Value;
                }
            }

            var inputs = Enumerable.Range(0, observedVector.Length)
                .Select(i => new double[Replicates.Count]).ToArray();
            for (int iReplicate = 0; iReplicate < Replicates.Count; iReplicate++)
            {
                foreach (var weight in Replicates[iReplicate].Weights)
                {
                    if (reporterIonIndexes.TryGetValue(weight.Key, out int index))
                    {
                        inputs[index][iReplicate] = weight.Value;
                    }
                }
            }

            var nonNegativeLeastSquares = new NonNegativeLeastSquares()
            {
                MaxIterations = 100
            };
            MultipleLinearRegression regression = nonNegativeLeastSquares.Learn(inputs, observedVector);
            double[] coefficients = regression.Weights;
            return coefficients;
        }


        private Dictionary<string, int> MakeIndexDictionary(IEnumerable<string> names)
        {
            var dictionary = new Dictionary<string, int>();
            foreach (var name in names)
            {
                if (!dictionary.ContainsKey(name))
                {
                    dictionary.Add(name, dictionary.Count);
                }
            }
            return dictionary;
        }
    }
}
