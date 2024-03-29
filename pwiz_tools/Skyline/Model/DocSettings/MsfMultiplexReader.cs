using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using EnvDTE;
using pwiz.Common.Collections;
using pwiz.Common.Database;
using pwiz.Common.Database.NHibernate;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.DocSettings
{
    public class MsfMultiplexReader
    {
        public MsfMultiplexReader(ICollection<MeasuredIon> customIons)
        {
            CustomIons = ImmutableList.ValueOf(customIons);
        }
        
        public ImmutableList<MeasuredIon> CustomIons { get; private set; }

        public MultiplexMatrix ReadMultiplexMatrix(string msfFilePath)
        {
            using var connection = new SQLiteConnection(SessionFactoryFactory.SQLiteConnectionStringBuilderFromFilePath(msfFilePath).ToString()).OpenAndReturn();
            if (!SqliteOperations.TableExists(connection, @"AnalysisDefinition"))
            {
                return null;
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT AnalysisDefinitionXML FROM AnalysisDefinition";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string xml = reader.GetString(0);
                var matrix = ParseAnalysisDefinition(xml);
                if (matrix != null)
                {
                    return matrix;
                }
            }

            return null;
        }

        public MultiplexMatrix ParseAnalysisDefinition(string analysisDefinitionXML)
        {
            var xDocument = XDocument.Load(new StringReader(analysisDefinitionXML));
            if (xDocument.Root == null)
            {
                return null;
            }

            foreach (var elQuantitationMethod in xDocument.Root.Elements(@"StudyDefinition")
                         .Elements(@"StudyDefinitionExtensions").Elements(@"StudyDefinitionExtension")
                         .Elements(@"QuantitationMethods").Elements(@"QuantitationMethod"))
            {
                var innerText = elQuantitationMethod.Value;
                var matrix = ParseQuantitationMethod(XDocument.Load(new StringReader(innerText)));
                if (matrix != null)
                {
                    return matrix;
                }
            }
            
            Console.Out.WriteLine(xDocument);
            return null;
        }

        public MultiplexMatrix ParseQuantitationMethod(XDocument xDocument)
        {
            if (xDocument.Root == null)
            {
                return null;
            }

            var tagElements = new List<Tuple<XElement, MeasuredIon, double>>();

            foreach (var elTag in xDocument.Root.Elements(@"MethodPart").Elements(@"MethodPart"))
            {
                var strMonoisotopicMz = elTag.Elements(@"Parameter")
                    .FirstOrDefault(el => @"MonoisotopicMZ".Equals(el.Attribute(@"name")?.Value))?.Value;
                if (strMonoisotopicMz == null)
                {
                    continue;
                }

                var monoisotopicMz = double.Parse(strMonoisotopicMz, CultureInfo.InvariantCulture);
                var closestMatch = FindMeasuredIon(monoisotopicMz);
                tagElements.Add(Tuple.Create(elTag, closestMatch, monoisotopicMz));
            }

            var replicates = new List<MultiplexMatrix.Replicate>();
            foreach (var (elTag, measuredIon, mz) in tagElements)
            {
                var weights = new List<MultiplexMatrix.Weighting>();
                var elCorrectionFactors = elTag.Elements(@"MethodPart")
                    .FirstOrDefault(el => @"CorrectionFactors" == el.Attribute(@"name")?.Value);
                if (elCorrectionFactors == null)
                {
                    continue;
                }
                foreach (var elCorrectionFactor in elCorrectionFactors.Elements(@"MethodPart"))
                {
                    var elAffects = elCorrectionFactor.Elements(@"Parameter")
                        .FirstOrDefault(el => @"Affects" == el.Attribute(@"name")?.Value);
                    if (elAffects == null)
                    {
                        continue;
                    }

                    var affectsValue = int.Parse(elAffects.Value);
                    if (affectsValue < 0)
                    {
                        continue;
                    }

                    if (affectsValue == 0 || affectsValue > tagElements.Count)
                    {
                        throw new InvalidDataException(string.Format("Expected 'Affects' value {0} to be between 1 and {1}", affectsValue, tagElements.Count));
                    }

                    var elFactor = elCorrectionFactor.Elements(@"Parameter")
                        .FirstOrDefault(el => @"Factor" == el.Attribute(@"name")?.Value);
                    if (elFactor == null)
                    {
                        continue;
                    }

                    var factorValue = double.Parse(elFactor.Value, CultureInfo.InvariantCulture);
                    var tagElement = tagElements[affectsValue - 1];
                    weights.Add(new MultiplexMatrix.Weighting(tagElement.Item2?.Name, tagElement.Item3, factorValue));
                }
                replicates.Add(new MultiplexMatrix.Replicate(elTag.Attribute(@"name")?.Value, weights));
            }

            return new MultiplexMatrix(xDocument.Root.Attribute(@"name")?.Value, replicates);
        }

        private MeasuredIon FindMeasuredIon(double monoMz)
        {
            MeasuredIon closestMatch = null;
            double closestDelta = double.MaxValue;
            foreach (var measuredIon in CustomIons)
            {
                var delta = Math.Abs(monoMz - measuredIon.SettingsCustomIon.MonoisotopicMass);
                if (closestMatch == null || delta < closestDelta)
                {
                    closestMatch = measuredIon;
                    closestDelta = delta;
                }
            }

            return closestMatch;
        }
    }
}
