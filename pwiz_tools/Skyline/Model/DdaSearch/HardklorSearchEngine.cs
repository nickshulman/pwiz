/*
 * Original author: Brian Pratt <bspratt .at. uw.edu >
 *
 * Copyright 2022 University of Washington - Seattle, WA
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

using pwiz.Common.Chemistry;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using pwiz.Skyline.Util.Extensions;
using System.Text;

namespace pwiz.Skyline.Model.DdaSearch
{
    public class HardklorSearchEngine : AbstractDdaSearchEngine, IProgressMonitor
    {
        private ImportPeptideSearch _searchSettings;

        // Temp files we'll need to clean up at the end the end
        private Dictionary<MsDataFileUri, string> _inputsAndOutputs;
        private string _isotopesFilename;
        private string _tempDirectory;
        private string _paramsFilename;

        public HardklorSearchEngine(ImportPeptideSearch searchSettings)
        {
            _searchSettings = searchSettings;
        }


        private CancellationTokenSource _cancelToken;
        private IProgressStatus _progressStatus;
        private bool _success;

        public override string[] FragmentIons => Array.Empty<string>();
        public override string[] Ms2Analyzers => Array.Empty<string>();
        public override string EngineName => @"Hardklor";
        public override Bitmap SearchEngineLogo => Resources.HardklorLogo;

        public override event NotificationEventHandler SearchProgressChanged;

        public override bool Run(CancellationTokenSource cancelToken, IProgressStatus status)
        {
            _cancelToken = cancelToken;
            _progressStatus = status;
            _success = true;
            _isotopesFilename = null;
            _paramsFilename = null;

            try
            {
                // Hardklor is not L10N ready, so take care to run its process under InvariantCulture
                Func<string> RunHardklor = () =>
                {
                    var paramsFileText = GenerateHardklorConfigFile();
                    _paramsFilename = Path.GetTempFileName();
                    File.WriteAllText(_paramsFilename, paramsFileText.ToString());
                    var pr = new ProcessRunner();
                    var psi = new ProcessStartInfo(@"Hardklor", _paramsFilename)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = false,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };
                    pr.Run(psi, string.Empty, this, ref _progressStatus, ProcessPriorityClass.BelowNormal);
                    return _paramsFilename;
                };
                LocalizationHelper.CallWithCulture(CultureInfo.InvariantCulture, RunHardklor);
                _progressStatus = _progressStatus.NextSegment();
            }
            catch (Exception ex)
            {
                _progressStatus = _progressStatus.ChangeErrorException(ex).ChangeMessage(string.Format(Resources.DdaSearch_Search_failed__0, ex.Message));
                _success = false;
            }

            if (IsCanceled && !_progressStatus.IsCanceled)
            {
                _progressStatus = _progressStatus.Cancel().ChangeMessage(Resources.DDASearchControl_SearchProgress_Search_canceled);
                _success = false;
            }

            if (!_success)
            {
                _cancelToken.Cancel();
            }

            if (_success)
                _progressStatus = _progressStatus.Complete().ChangeMessage(Resources.DDASearchControl_SearchProgress_Search_done);
            UpdateProgress(_progressStatus);

            FileEx.SafeDelete(_paramsFilename, true);

            return _success;
        }


        public override void SetEnzyme(Enzyme enz, int mmc)
        {
            // Not applicable to Hardklor
        }

        public override void SetFragmentIonMassTolerance(MzTolerance mzTolerance)
        {
            // Not applicable to Hardklor
        }

        public override void SetFragmentIons(string ions)
        {
            // Not applicable to Hardklor
        }

        public override void SetMs2Analyzer(string ms2Analyzer)
        {
            // not used by Hardklor
        }

        public override void SetPrecursorMassTolerance(MzTolerance mzTolerance)
        {
            // Not applicable to Hardklor
        }

        public override string GetSearchResultFilepath(MsDataFileUri searchFilepath)
        {
            return _inputsAndOutputs[searchFilepath];
        }

        private string[] SupportedExtensions = { @".mzml", @".mzxml" }; // TODO - build Hardklor+MSToolkit to use pwiz so we don't have to convert to mzML
        public override bool GetSearchFileNeedsConversion(MsDataFileUri searchFilepath, out AbstractDdaConverter.MsdataFileFormat requiredFormat)
        {
            requiredFormat = AbstractDdaConverter.MsdataFileFormat.mzML;
            if (!SupportedExtensions.Contains(e => e == searchFilepath.GetExtension().ToLowerInvariant()))
                return true;
            return false;
        }

        public bool IsCanceled => _cancelToken.IsCancellationRequested;
        public UpdateProgressResponse UpdateProgress(IProgressStatus status)
        {
            SearchProgressChanged?.Invoke(this, status);
            return _cancelToken.IsCancellationRequested ? UpdateProgressResponse.cancel : UpdateProgressResponse.normal;
        }

        public bool HasUI => false;

        public override void SetModifications(IEnumerable<StaticMod> modifications, int maxVariableMods_)
        {
            // Not applicable for Hardklor
        }

        public override void Dispose()
        {
            FileEx.SafeDelete(_paramsFilename, true);
            FileEx.SafeDelete(_isotopesFilename, true);
            if (_inputsAndOutputs != null)
            {
                foreach (var output in _inputsAndOutputs.Values)
                {
                    FileEx.SafeDelete(output, true);
                }
            }
            DirectoryEx.SafeDelete(_tempDirectory);
        }

        [Localizable(false)]
        private void InitializeIsotopes()
        {
            // Make sure Hardklor is working with the same isotope information as Skyline
            _isotopesFilename = Path.GetTempFileName();
            var isotopeValues = new List<string>
            {
                // First few lines are particular to Hardklor
                "X  2", "1  0.9", "2  0.1", string.Empty
            };
            var abundances = IsotopeAbundances.Default; // A map of element -> [ mass,abundance, mass, abundance, ...]
            // These are the elements listed in  CMercury8::DefaultValues() in pwiz_tools\Skyline\Executables\Hardklor\Hardklor\CMercury8.cpp
            foreach (var element in new [] { 
                         // "X", ?? appears in standard file, no Skyline equivalent - see hardcoded values above
                         "H","He","Li","Be","B","C","N","O","F","Ne","Na","Mg","Al","Si","P","S","Cl","Ar",
                         "K","Ca","Sc","Ti","V","Cr","Mn","Fe","Co","Ni","Cu","Zn","Ga","Ge","As","Se","Br","Kr","Rb","Sr","Y","Zr",
                         "Nb","Mo","Tc","Ru","Rh","Pd","Ag","Cd","In","Sn","Sb","Te","I","Xe","Cs","Ba","La","Ce","Pr","Nd","Pm","Sm",
                         "Eu","Gd","Tb","Dy","Ho","Er","Tm","Yb","Lu","Hf","Ta","W","Re","Os","Ir","Pt","Au","Hg","Tl","Pb","Bi","Po",
                         "At","Rn","Fr","Ra","Ac","Th","Pa","U","Np","Pu","Am","Cm","Bk","Cf","Es","Fm","Md","No","Lr",
                         "Hx","Cx","Nx", "Ox","Sx"}) // These are just repeats of H, C, N, O, S in the standard file
            {
                var massDistribution = abundances[element.EndsWith("x")? element.Substring(0,1) : element];
                isotopeValues.Add($"{element}  {massDistribution.Values.Count}");
                for (var i = 0; i < massDistribution.Values.Count; i++)
                {
                    isotopeValues.Add($"{massDistribution.Keys[i]}  {massDistribution.Values[i]} ");
                }
                isotopeValues.Add(string.Empty);
            }

            File.AppendAllLines(_isotopesFilename, isotopeValues);
        }

        private string GenerateHardklorConfigFile()
        {
            _inputsAndOutputs = new Dictionary<MsDataFileUri, string>();
            // Create a temporary directory for output
            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);

            foreach (var input in SpectrumFileNames)
            {
                _inputsAndOutputs.Add(input, $@"{Path.Combine(_tempDirectory, input.GetFileName())}.hk");
                if (!_searchSettings.DataIsCentroided)
                {
                    // User didn't declare data as centroided, but that might only be because they don't know.
                    // Luckily mzML conversion makes that easy to discover for many raw formats, and we should
                    // find a clue within the first few hundred lines.
                    using var reader = new StreamReader(input.GetFilePath());
                    for (var lineNum = 0; lineNum < 500; lineNum++)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                        {
                            break; // EOF
                        }
                        if (line.Contains(@"MS:1000127") || line.Contains(@"centroid spectrum"))
                        {
                            _searchSettings.DataIsCentroided = true;
                            break;
                        }
                    }
                }
            }

            // Make sure Hardklor is working with the same isotope information as Skyline
            InitializeIsotopes();

            return TextUtil.LineSeparate(
                $@"# comments in ALL CAPS are from a discussion with Danielle Faivre about Skyline integration",
                $@"",
                $@"# Please see online documentation for detailed explanations: ",
                $@"# http://proteome.gs.washington.edu/software/hardklor",
                $@"",
                $@"# All parameters are separated from their values by an equals sign ('=')",
                $@"# Anything after a '#' will be ignored for the remainder of the line.",
                $@"# All data files (including paths if necessary) to be analyzed are discussed below.",
                $@"",
                $@"# Parameters used to described the data being input to Hardklor",
                $@"instrument	=	{_searchSettings.Instrument.Replace(@"-", string.Empty)}	#Values are: FTICR, Orbitrap, TOF, QIT #NEED UI",
                $@"resolution	=	{_searchSettings.ResolutionAt400mz}		#Resolution at 400 m/z #NEED UI",
                $@"centroided	=	{(_searchSettings.DataIsCentroided ? @"1" : @"0")}			#0=no, 1=yes #NEED UI",
                $@"",
                $@"# Parameters used in preprocessing spectra prior to analysis",
                $@"ms_level			=	1		#1=MS1, 2=MS2, 3=MS3, 0=all",
                $@"scan_range_min		=	0		#ignore any spectra lower than this number, 0=off",
                $@"scan_range_max		=	0		#ignore any spectra higher than this number, 0=off",
                $@"signal_to_noise		=	{_searchSettings.SignalToNoise}		#set signal-to-noise ratio, 0=off #NEED UI",
                $@"sn_window			=	250.0	#size in m/z for computing localized noise level in a spectrum.",
                $@"static_sn			=	0		#0=off, 1=on. Apply lowest localized noise level to entire spectrum.",
                $@"boxcar_averaging	=	0		#0=off, or specify number of scans to average together, use odd numbers only #MAY NEED UI IN FUTURE",
                $@"boxcar_filter		=	0		#0=off, when using boxcar_averaging, only keep peaks seen in this number of scans #MAY NEED UI IN FUTURE",
                $@"								#  currently being averaged together. When on, signal_to_noise is not used.",
                $@"boxcar_filter_ppm	=	5		#Tolerance in ppm for matching peaks across spectra in boxcar_filter #MAY NEED UI IN FUTURE",
                $@"mz_min				=	0		#Sets lower bound of spectrum m/z range to analyze, 0=off",
                $@"mz_max				=	0		#Sets upper bound of spectrum m/z range to analyze, 0=off",
                $@"smooth				=	0		#Peforms Savitzky-Golay smoothing of peaks data. 0=off",
                $@"								#  Not recommended for high resolution data.",
                $@"",
                $@"# Parameters used to customize the Hardklor analysis. Some of these parameters will drastically",
                $@"# affect the analysis speed and results. Please consult the documentation and choose carefully!",
                $@"algorithm			=	Version2	#Algorithms include: Basic, Version1, Version2",
                $@"charge_algorithm	=	Quick		#Preferred method for feature charge identification.",
                $@"									#  Values are: Quick, FFT, Patterson, Senko, None",
                $@"									#  If None is set, all charge states are assumed, slowing Hardklor",
                $@"charge_min			=	1			#Lowest charge state allowed in the analysis. #MAY NEED UI IN FUTURE",
                $@"charge_max			=	5			#Highest charge state allowed in the analysis. #MAY NEED UI IN FUTURE",
                $@"correlation			=	{_searchSettings.CutoffScore}		#Correlation threshold to accept a peptide feature. #NEED UI",
                $@"averagine_mod		=	0			#Formula containing modifications to the averagine model.",
                $@"									#  Read documentation carefully before using! 0=off",
                $@"mz_window			=	5.25		#Breaks spectrum into windows not larger than this value for Version1 algorithm.",
                $@"sensitivity			=	2			#Values are 0 (lowest) to 3 (highest). Increasing sensitivity",
                $@"									#  identifies more features near the noise where the isotope distribution",
                $@"									#  may not be fully visible. However, these features are also more",
                $@"									#  likely to be false.",
                $@"depth				=	2			#Depth of combinatorial analysis. This is the maximum number of overlapping",
                $@"									#  features allowed in any mz_window. Each increase requires exponential",
                $@"									#  computation. In other words, keep this as low as necessary!!!",
                $@"max_features		=	12			#Maximum number of potential features in an mz_window to combinatorially solve.",
                $@"									#  Setting this too high results in wasted computation time trying to mix-and-match",
                $@"									#  highly improbable features.",
                $@"molecule_max_mz 	= 	5000		#Maximum m/z of molecules to detect. Set this higher than largest expected molecule.",
                $@"",
                $@"# Parameters used to customize the Hardklor output",
                $@"distribution_area	=	1	#Report sum of distribution peaks instead of highest peak only. 0=off, 1=on",
                $@"xml					=	0	#Output results as XML. 0=off, 1=on #MAY NEED UI IN FUTURE",
                $@"",
                $@"isotope_data =  ""{_isotopesFilename}"" # Using Skyline's isotope abundance values",
                $@"",
                $@"# Below this point is where files to be analyzed should go. They should be listed contain ",
                $@"# both the input file name, and the output file name. Each file to be analyzed should begin ",
                $@"# on a new line. By convention Hardklor output should have this extension: .hk",
                $@"",
                TextUtil.LineSeparate(_inputsAndOutputs.Select(kvp => ($@"""{kvp.Key}"" ""{kvp.Value}""")))
            );
        }
    }
}
 