﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2019 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Crosslinking;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Controls.SeqNode
{
    public class PeptideFormatter : Immutable
    {
        public static readonly Color COLOR_CROSSLINK = Color.Green;
        private SequenceInfo _lightSequenceInfo;

        private List<Tuple<IsotopeLabelType, SequenceInfo>> _heavySequenceInfos =
            new List<Tuple<IsotopeLabelType, SequenceInfo>>();

        public PeptideFormatter(SrmSettings srmSettings, ModifiedSequence lightModifiedSequence, IEnumerable<KeyValuePair<IsotopeLabelType, ModifiedSequence>> heavyModifiedSequences, ModFontHolder modFontHolder)
        {
            SrmSettings = srmSettings;
            _lightSequenceInfo = new SequenceInfo(lightModifiedSequence);
            if (heavyModifiedSequences != null)
            {
                _heavySequenceInfos.AddRange(heavyModifiedSequences.Select(entry=>Tuple.Create(entry.Key, new SequenceInfo(entry.Value))));
            }
            LightModifiedSequence = lightModifiedSequence;
            ModFontHolder = modFontHolder;
            LinkedPeptides = ImmutableSortedList<ModificationSite, PeptideFormatter>.EMPTY;
        }

        public static PeptideFormatter MakePeptideFormatter(SrmSettings srmSettings, PeptideDocNode peptideDocNode,
            ModFontHolder modFontHolder)
        {
            var heavyLabelTypes = srmSettings.PeptideSettings.Modifications.GetHeavyModificationTypes()
                .Where(peptideDocNode.HasChildType).ToHashSet();
            return MakePeptideFormatter(srmSettings, peptideDocNode.Peptide, peptideDocNode.ExplicitMods,
                heavyLabelTypes, modFontHolder);
        }

        public static PeptideFormatter MakePeptideFormatter(SrmSettings srmSettings, Peptide peptide,
            ExplicitMods explicitMods, ICollection<IsotopeLabelType> heavyLabelTypes,
            ModFontHolder modFontHolder)
        {
            var lightModifiedSequence =
                ModifiedSequence.GetModifiedSequence(srmSettings, peptide.Sequence, explicitMods, IsotopeLabelType.light);
            var heavyModifiedSequences = new List<KeyValuePair<IsotopeLabelType, ModifiedSequence>>();
            foreach (var labelType in srmSettings.PeptideSettings.Modifications.GetHeavyModificationTypes())
            {
                if (!heavyLabelTypes.Contains(labelType))
                {
                    continue;
                }

                heavyModifiedSequences.Add(new KeyValuePair<IsotopeLabelType, ModifiedSequence>(labelType,
                    ModifiedSequence.GetModifiedSequence(srmSettings, peptide.Sequence, explicitMods, labelType)));
            }

            var peptideFormatter = new PeptideFormatter(srmSettings, lightModifiedSequence, heavyModifiedSequences, modFontHolder);
            if (explicitMods != null && explicitMods.HasCrosslinks)
            {
                var linkedPeptides = explicitMods.Crosslinks.ReplaceValues(explicitMods.Crosslinks.Select(
                    entry => MakePeptideFormatter(srmSettings,
                            entry.Value.Peptide, entry.Value.ExplicitMods, heavyLabelTypes, modFontHolder)
                        .ChangeCrosslinkedIndexAa(entry.Value.IndexAa)));
                peptideFormatter = peptideFormatter.ChangeLinkedPeptides(linkedPeptides);
            }

            return peptideFormatter;
        }

        public int? CrosslinkedIndexAa { get; private set; }

        public PeptideFormatter ChangeCrosslinkedIndexAa(int? index)
        {
            return ChangeProp(ImClone(this), im => im.CrosslinkedIndexAa = index);
        }

        public ImmutableSortedList<ModificationSite, PeptideFormatter> LinkedPeptides { get; private set; }

        public PeptideFormatter ChangeLinkedPeptides(
            ImmutableSortedList<ModificationSite, PeptideFormatter> linkedPeptides)
        {
            return ChangeProp(ImClone(this), im => im.LinkedPeptides = linkedPeptides);
        }

        public ModifiedSequence LightModifiedSequence { get; private set; }

        public string UnmodifiedSequence
        {
            get { return LightModifiedSequence.GetUnmodifiedSequence(); }
        }

        public ImmutableList<KeyValuePair<IsotopeLabelType, ModifiedSequence>> HeavyModifiedSequences
        {
            get;
            private set;
        }

        public IEnumerable<Tuple<IsotopeLabelType, ImmutableList<ModifiedSequence.Modification>>>
            GetModificationsAtResidue(DisplayModificationOption displayModificationOption, int residue)
        {
            var mods = ImmutableList.ValueOf(_lightSequenceInfo.ModificationsByResidue[residue]);
            if (displayModificationOption.IgnoreZeroMassMods)
            {
                mods = ImmutableList.ValueOf(mods.Where(mod=>mod.MonoisotopicMass != 0 || mod.AverageMass != 0));
            }
            if (mods.Any())
            {
                yield return Tuple.Create(IsotopeLabelType.light, mods);
            }

            foreach (var entry in _heavySequenceInfos)
            {
                mods = ImmutableList.ValueOf(entry.Item2.ModificationsByResidue[residue]);
                if (mods.Any())
                {
                    yield return Tuple.Create(entry.Item1, mods);
                }
            }
        }

        public TextSequence GetTextSequenceAtAaIndex(DisplayModificationOption displayModificationOption, int residue)
        {
            Font font = ModFontHolder.Plain;
            Color color = Color.Black;
            String strAminoAcid = UnmodifiedSequence.Substring(residue, 1);

            var modsAtResidue = GetModificationsAtResidue(displayModificationOption, residue).ToArray();
            if (residue == CrosslinkedIndexAa || modsAtResidue.Any(labeledMod=> labeledMod.Item2.Any(mod=>mod.ExplicitMod.LinkedPeptide != null)))
            {
                return new TextSequence()
                {
                    Color = COLOR_CROSSLINK,
                    Font = ModFontHolder.LightAndHeavy,
                    Text = strAminoAcid
                };
            }
            if (modsAtResidue.Length == 0)
            {
                return new TextSequence
                {
                    Color = color,
                    Font = font,
                    Text = strAminoAcid
                };
            }

            var firstEntry = modsAtResidue[0];
            color = ModFontHolder.GetModColor(firstEntry.Item1);
            var firstMismatch = modsAtResidue.Skip(1).FirstOrDefault(entry => !entry.Item2.Equals(firstEntry.Item2));
            if (firstMismatch == null)
            {
                font = ModFontHolder.GetModFont(firstEntry.Item1);
                return new TextSequence
                {
                    Color = color,
                    Font = font,
                    Text = strAminoAcid + displayModificationOption.GetModificationText(SrmSettings, firstEntry.Item2)
                };
            }

            if (IsotopeLabelType.light.Equals(firstEntry.Item1))
            {
                font = ModFontHolder.LightAndHeavy;
                color = ModFontHolder.GetModColor(firstMismatch.Item1);
            }
            else
            {
                font = ModFontHolder.LightAndHeavy;
            }
            string modText;
            if (displayModificationOption == DisplayModificationOption.NOT_SHOWN)
            {
                modText = strAminoAcid;
            }
            else
            {
                modText = strAminoAcid + @"[*]";
            }

            return new TextSequence
            {
                Color = color,
                Font = font,
                Text = modText
            };
        }

        private const string STR_WIDE_DASH = "\u2014";

        public IEnumerable<TextSequence> GetTextSequencesForLinkedPeptides(DisplayModificationOption displayModificationOption)
        {
            var result = new List<TextSequence>();
            if (LinkedPeptides.Count == 0)
            {
                return result;
            }

            result.Add(new TextSequence
            {
                Color = Color.Black,
                Font = ModFontHolder.Plain,
                Text = STR_WIDE_DASH
            });
            if (LinkedPeptides.Count > 1)
            {
                result.Add(new TextSequence
                {
                    Color = Color.Black,
                    Font = ModFontHolder.Plain,
                    Text = @"["
                });
            }

            bool first = true;
            foreach (var linkedPeptide in LinkedPeptides.Values)
            {
                result.AddRange(linkedPeptide.GetTextSequencesForSelfAndChildren(displayModificationOption));
                if (!first)
                {
                    result.Add(new TextSequence
                    {
                        Color = Color.Black,
                        Font = ModFontHolder.Plain,
                        Text = @","
                    });
                }

                first = false;
            }

            if (LinkedPeptides.Count > 1)
            {
                result.Add(new TextSequence
                {
                    Color = Color.Black,
                    Font = ModFontHolder.Plain,
                    Text = @"]"
                });
            }

            return result;
        }

        private IEnumerable<TextSequence> GetTextSequencesForSelfAndChildren(
            DisplayModificationOption displayModificationOption)
        {
            return Enumerable.Range(0, UnmodifiedSequence.Length)
                .Select(i => GetTextSequenceAtAaIndex(displayModificationOption, i))
                .Concat(GetTextSequencesForLinkedPeptides(displayModificationOption));
        }


        public SrmSettings SrmSettings { get; private set; }
        public ModFontHolder ModFontHolder { get; private set; }

        private class SequenceInfo
        {
            public SequenceInfo(ModifiedSequence modifiedSequence)
            {
                ModifiedSequence = modifiedSequence;
                ModificationsByResidue = modifiedSequence.GetModifications().ToLookup(mod => mod.IndexAA);
            }

            public ModifiedSequence ModifiedSequence { get; private set; }
            public ILookup<int, ModifiedSequence.Modification> ModificationsByResidue { get; private set; }
        }
    }
}
