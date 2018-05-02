using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.FileIO;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using TopographTool.Model.DataRows;

namespace TopographTool.Model
{
    public class TopographData : Immutable
    {
        public ImmutableList<Protein> Proteins { get; private set; }

        public static TopographData MakeTopographData(IEnumerable<TransitionRow> transitionRows, IEnumerable<ScanInfoRow> scanInfos, IEnumerable<TransitionResultRow> transitionResultRows)
        {
            var replicates = new Dictionary<string, Replicate>();
            var resultFiles = new List<ResultFile>();
            foreach (var scanInfo in scanInfos)
            {
                Replicate replicate;
                if (!replicates.TryGetValue(scanInfo.ReplicateLocator, out replicate))
                {
                    replicate = new Replicate(scanInfo);
                    replicates.Add(replicate.Locator, replicate);
                }
                var resultFile = new ResultFile(replicate, scanInfo);
                resultFiles.Add(resultFile);
            }
            var transitionResults = MakeTransitionResults(resultFiles, transitionResultRows);
            var proteins = new List<Protein>();
            TransitionRow lastRow = null;
            foreach (var protein in transitionRows.ToLookup(row => row.ProteinLocator))
            {
                var peptides = new List<Peptide>();
                foreach (var peptide in protein.ToLookup(row => row.PeptideModifiedSequenceFullNames))
                {
                    var precursors = new List<Precursor>();
                    foreach (var precursor in peptide.ToLookup(row =>
                        Tuple.Create(row.ModifiedSequenceFullNames, row.PrecursorCharge)))
                    {
                        var transitions = new List<Transition>();
                        foreach (var transition in precursor)
                        {
                            lastRow = transition;
                            IList<TransitionResult> results;
                            if (transitionResults.TryGetValue(transition.TransitionLocator, out results))
                            {
                                transitions.Add(new Transition(transition, results));
}
                        }
                        precursors.Add(new Precursor(lastRow, transitions));
                    }
                    peptides.Add(new Peptide(lastRow, precursors));
                }
                proteins.Add(new Protein(lastRow, peptides));
            }
            return new TopographData
            {
                Proteins = ImmutableList.ValueOf(proteins),
            };
        }

        private static IDictionary<string, IList<TransitionResult>> MakeTransitionResults(
            IEnumerable<ResultFile> resultFiles, IEnumerable<TransitionResultRow> transitionResultRows)
        {
            var resultFileDict = resultFiles.ToDictionary(file => file.ResultFileLocator);
            var transitionResultDict = new Dictionary<string, IList<TransitionResult>>();
            foreach (var transitionResultRow in transitionResultRows)
            {
                IList<TransitionResult> list;
                if (!transitionResultDict.TryGetValue(transitionResultRow.TransitionLocator, out list))
                {
                    list = new List<TransitionResult>();
                    transitionResultDict.Add(transitionResultRow.TransitionLocator, list);
                }
                var transitionResult = new TransitionResult(resultFileDict[transitionResultRow.ResultFileLocator], transitionResultRow);
                list.Add(transitionResult);
            }
            return transitionResultDict;
        }

        public static TopographData Read(string pathTransitions, string pathScanInfos, string pathTransitionResults)
        {
            using (var transitionParser = MakeTextFieldParser(pathTransitions))
            using (var scanInfoParser = MakeTextFieldParser(pathScanInfos))
            using (var transitionResultParser = MakeTextFieldParser(pathTransitionResults))
            {
                return MakeTopographData(RowReader.Read<TransitionRow>(transitionParser), 
                    RowReader.Read<ScanInfoRow>(scanInfoParser),
                    RowReader.Read<TransitionResultRow>(transitionResultParser));
            }
        }

        private static TextFieldParser MakeTextFieldParser(string path)
        {
            var textFieldParser = new TextFieldParser(path);
            textFieldParser.SetDelimiters(",");
            return textFieldParser;
        }
    }
}
