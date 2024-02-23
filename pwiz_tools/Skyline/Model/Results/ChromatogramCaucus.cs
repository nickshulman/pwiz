﻿using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Chemistry;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.Model.Crosslinking;

namespace pwiz.Skyline.Model.Results
{
    public class ChromatogramCaucus
    {
        public ChromatogramCaucus(SrmDocument document, int replicateIndex, MsDataFileUri dataFileUri)
        {
            Document = document;
            ReplicateIndex = replicateIndex;
            MsDataFileUri = dataFileUri.GetLocation();
        }

        public SrmDocument Document { get; }
        public int ReplicateIndex { get; }
        public MsDataFileUri MsDataFileUri { get; }

        public List<Entry> Entries { get; } = new List<Entry>();

        public ChromatogramGroupInfo LoadChromatogramGroupInfo(PeptideDocNode peptideDocNode, TransitionGroupDocNode transitionGroupDocNode)
        {
            if (!Document.Settings.MeasuredResults.TryLoadChromatogram(ReplicateIndex, peptideDocNode, transitionGroupDocNode, (float) Document.Settings.TransitionSettings.Instrument.MzMatchTolerance, out var chromatogramGroupInfos))
            {
                return null;
            }

            return chromatogramGroupInfos.FirstOrDefault(groupInfo => Equals(groupInfo.FilePath.GetLocation()));
        }

        public bool AddPrecursor(IdentityPath precursorIdentityPath)
        {
            if (!Equals(precursorIdentityPath.Depth, (int)SrmDocument.Level.TransitionGroups))
            {
                throw new ArgumentException(@"Invalid argument " + precursorIdentityPath);
            }

            var peptideDocNode =
                (PeptideDocNode)Document.FindNode(precursorIdentityPath.GetPathTo((int)SrmDocument.Level.Molecules));
            var transitionGroupDocNode = (TransitionGroupDocNode)peptideDocNode?.FindNode(precursorIdentityPath.Child);
            if (transitionGroupDocNode == null)
            {
                return false;
            }

            var chromatogramGroupInfo = LoadChromatogramGroupInfo(peptideDocNode, transitionGroupDocNode);
            var entry = new Entry(precursorIdentityPath, chromatogramGroupInfo, peptideDocNode, transitionGroupDocNode);
            Entries.Add(entry);
            return true;
        }

        public void AddMolecule(IdentityPath moleculeIdentityPath)
        {
            var peptideDocNode = (PeptideDocNode)Document.FindNode(moleculeIdentityPath);
            foreach (var transitionGroup in peptideDocNode.TransitionGroups)
            {
                AddPrecursor(new IdentityPath(moleculeIdentityPath, transitionGroup.TransitionGroup));
            }
        }

        public MassDistribution GetMassDistribution(PeptideDocNode peptideDocNode,
            TransitionGroupDocNode transitionGroupDocNode)
        {
            var moleculeMassOffset = GetPrecursorFormula(peptideDocNode, transitionGroupDocNode);
            var settings = Document.Settings;
            return settings.GetDefaultPrecursorCalc().GetMZDistribution(moleculeMassOffset,
                transitionGroupDocNode.PrecursorAdduct, settings.TransitionSettings.FullScan.IsotopeAbundances);
        }

        public MoleculeMassOffset GetPrecursorFormula(PeptideDocNode peptideDocNode,
            TransitionGroupDocNode transitionGroupDocNode)
        {
            if (transitionGroupDocNode.CustomMolecule != null)
            {
                return transitionGroupDocNode.CustomMolecule.ParsedMolecule.GetMoleculeMassOffset();
            }
            var crosslinkBuilder = new CrosslinkBuilder(Document.Settings, peptideDocNode.Peptide,
                peptideDocNode.ExplicitMods, transitionGroupDocNode.LabelType);
            return crosslinkBuilder.GetPrecursorFormula();
        }

        public IList<TimeIntensities> GetDeconvolutedChromatograms()
        {
            var deconvoluter = new IsotopeDeconvoluter(Entries.Select(entry =>
                GetMassDistribution(entry.PeptideDocNode, entry.TransitionGroupDocNode)));
            var chromatogramChannels = GetChromatogramChannels();
            return deconvoluter.Deconvolute(chromatogramChannels);
        }

        private Dictionary<MzRange, TimeIntensities> GetChromatogramChannels()
        {
            var dictionary = new Dictionary<MzRange, TimeIntensities>();
            foreach (var entry in Entries)
            {
                var chromatogramGroupInfo = entry.ChromatogramGroupInfo;
                if (chromatogramGroupInfo == null)
                {
                    continue;
                }
                for (int iTransition = 0; iTransition < chromatogramGroupInfo.NumTransitions; iTransition++)
                {
                    var chromTransition = chromatogramGroupInfo.GetChromTransitionLocal(iTransition);
                    if (chromTransition.ExtractionWidth == 0)
                    {
                        continue;
                    }

                    var mzRange = new MzRange(chromTransition.Product - chromTransition.ExtractionWidth / 2,
                        chromTransition.Product + chromTransition.ExtractionWidth / 2);
                    var chromatogramInfo = chromatogramGroupInfo.GetTransitionInfo(iTransition);
                    if (!dictionary.ContainsKey(mzRange))
                    {
                        dictionary.Add(mzRange, chromatogramInfo.TimeIntensities);
                    }
                }
            }

            return dictionary;
        }

        public class Entry
        {
            public Entry(IdentityPath identityPath, ChromatogramGroupInfo chromatogramGroupInfo, PeptideDocNode peptideDocNode, TransitionGroupDocNode transitionGroupDocNode)
            {
                IdentityPath = identityPath;
                ChromatogramGroupInfo = chromatogramGroupInfo;
                PeptideDocNode = peptideDocNode;
                TransitionGroupDocNode = transitionGroupDocNode;
            }

            public IdentityPath IdentityPath { get; }
            public ChromatogramGroupInfo ChromatogramGroupInfo { get; }
            public PeptideDocNode PeptideDocNode { get; }
            public TransitionGroupDocNode TransitionGroupDocNode { get; }
        }
    }
}
