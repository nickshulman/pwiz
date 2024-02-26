using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.Model.Results
{
    public class ChromDisplaySubset : Immutable
    {
        public ChromDisplaySubset(DisplayTypeChrom displayTypeChrom)
        {
            DisplayTypeChrom = displayTypeChrom;
        }
        public DisplayTypeChrom DisplayTypeChrom { get; private set; }

        public ChromDisplaySubset ChangeDisplayTypeChrom(DisplayTypeChrom value)
        {
            return ChangeProp(ImClone(this), im => im.DisplayTypeChrom = value);
        }
        public MultiplexMatrix MultiplexMatrix { get; private set; }

        public ChromDisplaySubset ChangeMultiplexMatrix(MultiplexMatrix value)
        {
            return ChangeProp(ImClone(this), im => im.MultiplexMatrix = value);
        }

        public bool Deconvolute { get; private set; }

        public ChromDisplaySubset ChangeDeconvolute(bool value)
        {
            return ChangeProp(ImClone(this), im => im.Deconvolute = value);
        }

        public static ChromDisplaySubset FromSettings(SrmSettings settings, DisplayTypeChrom displayTypeChrom)
        {
            var result = new ChromDisplaySubset(displayTypeChrom);
            if (Settings.Default.DeconvoluteChromatograms)
            {
                result = result.ChangeDeconvolute(true);
                if (settings.PeptideSettings.Quantification.MultiplexMatrix?.Replicates.Count > 0)
                {
                    result = result.ChangeMultiplexMatrix(settings.PeptideSettings.Quantification.MultiplexMatrix);
                }
            }

            return result;
        }

        public static ChromDisplaySubset ForTransitionGroup(SrmDocument srmDocument,
            TransitionGroupDocNode transitionGroupDocNode)
        {
            return FromSettings(srmDocument.Settings,
                GraphChromatogram.GetDisplayType(srmDocument, transitionGroupDocNode));
        }

        public IEnumerable<TransitionDocNode> GetDisplayTransitions(TransitionGroupDocNode transitionGroupDocNode)
        {
            switch (DisplayTypeChrom)
            {
                case DisplayTypeChrom.precursors:
                    return transitionGroupDocNode.GetMsTransitions(true);
                case DisplayTypeChrom.products:
                    return transitionGroupDocNode.GetMsMsTransitions(true);
            }

            return transitionGroupDocNode.Transitions;
        }

        public IEnumerable<ChromatogramOwner> GetChromatogramsToDisplay(PeptideGroup peptideGroup, PeptideDocNode peptideDocNode, TransitionGroupDocNode transitionGroupDocNode)
        {
            if (transitionGroupDocNode == null)
            {
                return Array.Empty<ChromatogramOwner>();
            }
            var transitions = GetDisplayTransitions(transitionGroupDocNode).ToList();
            if (!Deconvolute)
            {
                return transitions.Select(transition =>
                    new ChromatogramOwner(peptideGroup, peptideDocNode, transitionGroupDocNode, transition));
            }
            var result = new List<ChromatogramOwner>();
            TransitionDocNode primaryMs1Transition = GetPrimaryMs1Transition(transitions);
            if (primaryMs1Transition != null)
            {
                result.Add(new ChromatogramOwner(peptideGroup, peptideDocNode, transitionGroupDocNode, primaryMs1Transition));
            }

            if (MultiplexMatrix == null)
            {
                result.AddRange(transitions.Where(transition=>!transition.IsMs1).Select(transition=>new ChromatogramOwner(peptideGroup, peptideDocNode, transitionGroupDocNode, transition)));
            }
            else
            {
                var reporterIons = MakeReporterIonDictionary(transitions);
                foreach (var replicate in MultiplexMatrix.Replicates)
                {
                    var reporterIon = GetReporterIonForMultiplexReplicate(replicate, reporterIons);
                    if (reporterIon != null)
                    {
                        result.Add(new ChromatogramOwner(peptideGroup, peptideDocNode, transitionGroupDocNode, reporterIon).ChangeMultiplexReplicate(replicate));
                    }
                }
            }

            return result;
        }

        public IEnumerable<ChromatogramOwner> GetChromatogramsToDisplay(SrmDocument document,
            IdentityPath transitionGroupPath)
        {
            var peptideGroup = (PeptideGroup)transitionGroupPath.GetIdentity(0);
            var peptideDocNode = (PeptideDocNode) document.FindNode(transitionGroupPath.GetPathTo((int)SrmDocument.Level.Molecules));
            var transitionGroupDocNode =
                (TransitionGroupDocNode)peptideDocNode?.FindNode(
                    transitionGroupPath.GetIdentity((int)SrmDocument.Level.TransitionGroups));
            return GetChromatogramsToDisplay(peptideGroup, peptideDocNode, transitionGroupDocNode);
        }

        private static TransitionDocNode GetPrimaryMs1Transition(IEnumerable<TransitionDocNode> transitions)
        {
            return transitions.Where(t => t.IsMs1).OrderBy(t => (uint)t.Transition.MassIndex).FirstOrDefault();
        }

        private static TransitionDocNode GetReporterIonForMultiplexReplicate(MultiplexMatrix.Replicate replicate, Dictionary<string, TransitionDocNode> reporterIons)
        {
            foreach (var weight in replicate.Weights.OrderByDescending(weight => weight.Key))
            {
                if (reporterIons.TryGetValue(weight.Key, out var reporterIon))
                {
                    return reporterIon;
                }
            }

            return null;
        }

        private static Dictionary<string, TransitionDocNode> MakeReporterIonDictionary(
            IEnumerable<TransitionDocNode> transitions)
        {
            var dictionary = new Dictionary<string, TransitionDocNode>();
            foreach (var transition in transitions.Reverse())
            {
                if (null != transition.CustomIon?.Name)
                {
                    dictionary.Add(transition.CustomIon.Name, transition);
                }
            }

            return dictionary;
        }
    }
}
