using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    public class TopographData : Immutable
    {
        public ImmutableList<Replicate> Replicates { get; private set; }
        public ImmutableList<Protein> Proteins { get; private set; }

        public static TopographData MakeTopographData(IEnumerable<ResultRow> resultRows)
        {
            var replicates = new Dictionary<string, Replicate>();
            var proteins = new List<Protein>();
            ResultRow lastRow = null;
            foreach (var protein in resultRows.ToLookup(row => row.ProteinLocator))
            {
                var peptides = new List<Peptide>();
                foreach (var peptide in protein.ToLookup(row => row.PeptideModifiedSequenceFullNames))
                {
                    var precursors = new List<Precursor>();
                    foreach (var precursor in peptide.ToLookup(row =>
                        Tuple.Create(row.ModifiedSequenceFullNames, row.PrecursorCharge)))
                    {
                        var transitions = new List<Transition>();
                        foreach (var transition in precursor.ToLookup(row => row.TransitionLocator))
                        {
                            var transitionResults = new List<TransitionResult>();
                            foreach (var row in transition)
                            {
                                lastRow = row;
                                Replicate replicate;
                                if (!replicates.TryGetValue(row.ReplicateLocator, out replicate))
                                {
                                    replicate = new Replicate(row);
                                    replicates.Add(replicate.Locator, replicate);
                                }
                                transitionResults.Add(new TransitionResult(replicate, row.Area, row.Truncated));
                            }
                            transitions.Add(new Transition(lastRow, transitionResults));
                        }
                        precursors.Add(new Precursor(lastRow, transitions));
                    }
                    peptides.Add(new Peptide(lastRow, precursors));
                }
                proteins.Add(new Protein(lastRow, peptides));
            }
            return new TopographData()
            {
                Replicates = ImmutableList.ValueOf(replicates.Values),
                Proteins = ImmutableList.ValueOf(proteins),
            };
        }
    }
}
