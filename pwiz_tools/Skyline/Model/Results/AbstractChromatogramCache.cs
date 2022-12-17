using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results
{
    public abstract class AbstractChromatogramCache : Immutable, IDisposable
    {
        LibKeyMap<int[]> _chromEntryIndex;
        protected void Init(FeatureNames scoreTypeIndices)
        {
            _chromEntryIndex = MakeChromEntryIndex();
            ScoreTypes = scoreTypeIndices;
        }

        public virtual void Dispose()
        {
        }

        public abstract IList<IChromGroupHeaderInfo> ChromGroupHeaderInfos { get; }

        protected virtual IList<IChromGroupHeaderInfo> HeaderInfosSortedByPrecursor
        {
            get { return ChromGroupHeaderInfos; }
        }
        public abstract TimeIntensitiesGroup ReadTimeIntensities(IChromGroupHeaderInfo header);

        public abstract IList<float> ReadScores(IChromGroupHeaderInfo header);

        public abstract IList<ChromPeak> ReadPeaks(IChromGroupHeaderInfo header);

        public virtual void ReadDataForAll(IList<IChromGroupHeaderInfo> chromGroupHeaderInfos, IList<ChromPeak>[] peaks, IList<float>[] scores)
        {
            for (int i = 0; i < chromGroupHeaderInfos.Count; i++)
            {
                peaks[i] = ReadPeaks(chromGroupHeaderInfos[i]);
                scores[i] = ReadScores(chromGroupHeaderInfos[i]);
            }
        }

        public abstract string GetTextId(IChromGroupHeaderInfo chromGroupHeaderInfo);

        public abstract IList<ChromCachedFile> CachedFiles { get; }
        public IEnumerable<MsDataFileUri> CachedFilePaths
        {
            get { return CachedFiles.Select(file => file.FilePath.GetLocation()); }
        }
        public virtual IPooledStream ReadStream
        {
            get;
            protected set;
        }

        public abstract CacheFormatVersion Version { get; }

        public string CachePath {get; protected set; }

        public FeatureNames ScoreTypes { get; private set; }
        public ImmutableList<MsDataFileUri> MsDataFilePaths
        {
            get { return ImmutableList.ValueOf(CachedFilePaths); }
        }
        public int ScoreTypesCount
        {
            get { return ScoreTypes.Count; }
        }


        private IEnumerable<ChromatogramGroupInfo> GetHeaderInfos(PeptideDocNode nodePep, SignedMz precursorMz, double? explicitRT, float tolerance, ChromatogramSet chromatograms)
        {
            foreach (int i in ChromatogramIndexesMatching(nodePep, precursorMz, tolerance, chromatograms))
            {
                var entry = ChromGroupHeaderInfos[i];
                // If explicit retention time info is available, use that to discard obvious mismatches
                if (!explicitRT.HasValue || // No explicit RT
                    !entry.StartTime.HasValue || // No time data loaded yet
                    (entry.StartTime <= explicitRT && explicitRT <= entry.EndTime))
                // No overlap
                {
                    yield return LoadChromatogramInfo(entry);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization|MethodImplOptions.NoInlining)]
        public IEnumerable<int> ChromatogramIndexesMatching(PeptideDocNode nodePep, SignedMz precursorMz,
            float tolerance, ChromatogramSet chromatograms)
        {
            if (nodePep != null && nodePep.IsProteomic && _chromEntryIndex != null)
            {
                bool anyFound = false;
                var key = new LibKey(nodePep.ModifiedTarget, Adduct.EMPTY).LibraryKey;
                IEnumerable<int> chromatogramIndexes =
                    _chromEntryIndex.ItemsMatching(key, false).SelectMany(list => list);
                foreach (var chromatogramIndex in chromatogramIndexes)
                {
                    var entry = ChromGroupHeaderInfos[chromatogramIndex];
                    if (!MatchMz(precursorMz, entry.Precursor, tolerance))
                    {
                        continue;
                    }
                    if (chromatograms != null &&
                        !chromatograms.ContainsFile(CachedFiles[entry.FileIndex]
                            .FilePath))
                    {
                        continue;
                    }
                    anyFound = true;
                    yield return chromatogramIndex;
                }
                if (anyFound)
                {
                    yield break;
                }
            }
            int i = FindEntry(precursorMz, tolerance);
            if (i < 0)
            {
                yield break;
            }
            for (; i < ChromGroupHeaderInfos.Count; i++)
            {
                var entry = ChromGroupHeaderInfos[i];
                if (!MatchMz(precursorMz, entry.Precursor, tolerance))
                    break;
                if (chromatograms != null &&
                    !chromatograms.ContainsFile(CachedFiles[entry.FileIndex]
                        .FilePath))
                {
                    continue;
                }

                if (nodePep != null && !TextIdEqual(entry, nodePep))
                    continue;
                yield return i;
            }
        }

        private bool TextIdEqual(IChromGroupHeaderInfo entry, PeptideDocNode nodePep)
        {
            string textId = GetTextId(entry);
            if (string.IsNullOrEmpty(textId))
            {
                return true;
            }
            if (nodePep.Peptide.IsCustomMolecule)
            {
                if (textId == nodePep.CustomMolecule.ToSerializableString())
                {
                    return true;
                }
                // Older .skyd files used just the name of the molecule as the TextId.
                // We can't rely on the FormatVersion in the .skyd, because of the way that .skyd files can get merged.
                if (textId == nodePep.CustomMolecule.InvariantName)
                {
                    return true;
                }
                return false;
            }
            else
            {
                var key1 = new PeptideLibraryKey(nodePep.ModifiedSequence, 0);
                var key2 = new PeptideLibraryKey(textId, 0);
                return LibKeyIndex.KeysMatch(key1, key2);
            }
        }

        private int FindEntry(SignedMz precursorMz, float tolerance)
        {
            return FindEntry(precursorMz, tolerance, 0, HeaderInfosSortedByPrecursor.Count - 1);
        }

        private int FindEntry(SignedMz precursorMz, float tolerance, int left, int right)
        {
            // Binary search for the right precursorMz
            if (left > right)
                return -1;
            int mid = (left + right) / 2;
            int compare = CompareMz(precursorMz, HeaderInfosSortedByPrecursor[mid].Precursor, tolerance);
            if (compare < 0)
                return FindEntry(precursorMz, tolerance, left, mid - 1);
            if (compare > 0)
                return FindEntry(precursorMz, tolerance, mid + 1, right);

            // Scan backward until the first matching element is found.
            while (mid > 0 && MatchMz(precursorMz, HeaderInfosSortedByPrecursor[mid - 1].Precursor, tolerance))
                mid--;

            return mid;
        }

        private static int CompareMz(SignedMz precursorMz1, SignedMz precursorMz2, float tolerance)
        {
            return precursorMz1.CompareTolerant(precursorMz2, tolerance);
        }

        private static bool MatchMz(SignedMz mz1, SignedMz mz2, float tolerance)
        {
            return CompareMz(mz1, mz2, tolerance) == 0;
        }

        /// <summary>
        /// Create a map of LibraryKey to the indexes into _chromatogramEntries that have that particular
        /// TextId.
        /// </summary>
        private LibKeyMap<int[]> MakeChromEntryIndex()
        {
            var libraryKeyIndexes = new Dictionary<string, int>();
            List<LibraryKey> libraryKeys = new List<LibraryKey>();
            List<List<int>> chromGroupIndexes = new List<List<int>>();

            var headers = ChromGroupHeaderInfos;
            for (int index = 0; index < headers.Count; index++)
            {
                string textId = GetTextId(headers[index]);
                if (string.IsNullOrEmpty(textId))
                {
                    continue;
                }
                int libraryKeyIndex;
                List<int> chromGroupIndexList;
                if (libraryKeyIndexes.TryGetValue(textId, out libraryKeyIndex))
                {
                    chromGroupIndexList = chromGroupIndexes[libraryKeyIndex];
                }
                else
                {
                    libraryKeyIndexes.Add(textId, libraryKeys.Count);
                    LibraryKey libraryKey;
                    if (textId[0] == '#')
                    {
                        var customMolecule = CustomMolecule.FromSerializableString(textId);
                        libraryKey = new MoleculeLibraryKey(customMolecule.GetSmallMoleculeLibraryAttributes(), Adduct.EMPTY);
                    }
                    else
                    {
                        libraryKey = new PeptideLibraryKey(textId, 0);
                    }
                    libraryKeys.Add(libraryKey);
                    chromGroupIndexList = new List<int>();
                    chromGroupIndexes.Add(chromGroupIndexList);
                }
                chromGroupIndexList.Add(index);
            }
            return new LibKeyMap<int[]>(
                ImmutableList.ValueOf(chromGroupIndexes.Select(indexes => indexes.ToArray())),
                libraryKeys);
        }
        public ChromatogramGroupInfo LoadChromatogramInfo(IChromGroupHeaderInfo chromGroupHeaderInfo)
        {
            return new ChromatogramGroupInfo(this, chromGroupHeaderInfo);
        }

        public IEnumerable<ChromatogramGroupInfo> LoadChromatogramInfos(PeptideDocNode nodePep, TransitionGroupDocNode nodeGroup,
            float tolerance, ChromatogramSet chromatograms)
        {
            var precursorMz = nodeGroup != null ? nodeGroup.PrecursorMz : SignedMz.ZERO;
            double? explicitRT = null;
            if (nodePep != null && nodePep.ExplicitRetentionTime != null)
            {
                explicitRT = nodePep.ExplicitRetentionTime.RetentionTime;
            }

            return GetHeaderInfos(nodePep, precursorMz, explicitRT, tolerance, chromatograms);
        }

        public IEnumerable<ChromatogramGroupInfo> LoadAllIonsChromatogramInfo(ChromExtractor extractor, ChromatogramSet chromatograms)
        {
            return LoadChromatogramInfos(null, null, 0, chromatograms)
                .Where(groupInfo => groupInfo.Header.Extractor == extractor);
        }

        public bool HasAllIonsChromatograms
        {
            get
            {
                return LoadChromatogramInfos(null, null, 0, null).Any();
            }
        }
        public abstract IEnumerable<ChromTransition> GetTransitions(IChromGroupHeaderInfo chromGroupHeaderInfo);
        public virtual bool IsReadStreamModified
        {
            get { return ReadStream?.IsModified ?? false; }
        }
        public virtual string ReadStreamModifiedExplanation
        {
            get { return ReadStream?.ModifiedExplanation; }
        }

        public abstract IMsDataFileScanIds LoadMSDataFileScanIds(int fileIndex);
        public virtual bool IsSupportedVersion
        {
            get { return true; }
        }
    }
}

