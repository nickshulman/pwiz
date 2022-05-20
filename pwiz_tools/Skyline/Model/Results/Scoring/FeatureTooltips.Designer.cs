﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace pwiz.Skyline.Model.Results.Scoring {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class FeatureTooltips {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal FeatureTooltips() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("pwiz.Skyline.Model.Results.Scoring.FeatureTooltips", typeof(FeatureTooltips).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A score of 1 or 0 depending on whether the peak overlaps with the retention time of an MS/MS identification..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_LegacyIdentifiedCountCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.LegacyIdentifiedCountCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fraction of transition peaks across all precursors for which &quot;Coeluting&quot; is true.
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_LegacyUnforcedCountScoreCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.LegacyUnforcedCountScoreCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fraction of transition peaks for which &quot;Coeluting&quot; is true.
        ///Calculated using internal standard precursors if they exist, otherwise analyte precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_LegacyUnforcedCountScoreDefaultCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.LegacyUnforcedCountScoreDefaultCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fraction of transitions across internal standard precursors for which &quot;Coeluting&quot; is true..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_LegacyUnforcedCountScoreStandardCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.LegacyUnforcedCountScoreStandardCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Log10 of the sum of the transition peak areas.
        ///Calculated using internal standard precursors if they exist, otherwise analyte precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestDefaultIntensityCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestDefaultIntensityCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Normalized contrast angle of MS2 transition areas against library intensities, if that exist.
        ///Otherwise, dot product of MS1 transition areas against predicted isotope distribution.
        ///Calculated using internal standard precursors if they exist, otherwise analyte precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestDefaultIntensityCorrelationCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestDefaultIntensityCorrelationCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MQuest co-elution score weighted by the sum of the transition areas.
        ///Calculated using internal standard precursors if they exist, otherwise analyte precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestDefaultWeightedCoElutionCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestDefaultWeightedCoElutionCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MQuest shape score, weighted by the sum of the transition peak areas.
        ///Calculated using internal standard precursors if they exist, otherwise analyte precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestDefaultWeightedShapeCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestDefaultWeightedShapeCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Log10 of the sum of the transition peak areas across analyte precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestIntensityCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestIntensityCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Normalized contrast angle of MS2 transition areas against library intensities.
        ///Calculated using analyte precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestIntensityCorrelationCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestIntensityCorrelationCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Normalized contrast angle of the correlation between analyte and internal standard transition peak areas..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestReferenceCorrelationCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestReferenceCorrelationCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Difference between predicted retention time and peak apex time..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestRetentionTimePredictionCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestRetentionTimePredictionCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Square of difference between predicted retention time and peak apex time..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestRetentionTimeSquaredPredictionCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestRetentionTimeSquaredPredictionCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Log10 of the sum of the transition peak areas across internal standard precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestStandardIntensityCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestStandardIntensityCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Normalized contrast angle of MS2 transition areas against library intensities.
        ///Calculated using internal standard precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestStandardIntensityCorrelationCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestStandardIntensityCorrelationCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MQuest co elution score, weighted by the sum of the transition peak areas.
        ///Calculated using internal standard precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestStandardWeightedCoElutionCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestStandardWeightedCoElutionCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MQuest shape score, weighted by the sum of the transition peak areas.
        ///Calculated using internal standard precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestStandardWeightedShapeCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestStandardWeightedShapeCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MQuest co-elution score of analyte precursors weighted by the sum of the transition areas.
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestWeightedCoElutionCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestWeightedCoElutionCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MQuest co elution score, weighted by the sum of the transition peak areas..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestWeightedReferenceCoElutionCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestWeightedReferenceCoElutionCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MQuest shape score, weighted by the sum of the transition peak areas..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestWeightedReferenceShapeCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestWeightedReferenceShapeCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MQuest shape score, weighted by the sum of the transition peak areas.
        ///Calculated using analyte precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_MQuestWeightedShapeCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.MQuestWeightedShapeCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The shape correlation score between MS1 ions and MS2 ions.
        ///Calculated using internal standard precursors if they exist, otherwise analyte precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_NextGenCrossWeightedShapeCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.NextGenCrossWeightedShapeCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Normalized contrast angle of the MS1 transition areas against the predicted isotope distribution.
        ///Calculated using analyte precursor with the highest score..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_NextGenIsotopeDotProductCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.NextGenIsotopeDotProductCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Average mass error across analyte MS1 transitions weighted by transition area..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_NextGenPrecursorMassErrorCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.NextGenPrecursorMassErrorCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Average mass error across analyte MS2 transitions weighted by transition area..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_NextGenProductMassErrorCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.NextGenProductMassErrorCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Log of the ratio of the peak height to the median intensity beyond the bounds of the peak.
        ///Calculated using analyte precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_NextGenSignalNoiseCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.NextGenSignalNoiseCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Average mass error across internal standard MS2 transitions weighted by transition area..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_NextGenStandardProductMassErrorCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.NextGenStandardProductMassErrorCalc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Log of the ratio of the peak height to the median intensity beyond the bounds of the peak.
        ///Calculated using internal standard precursors..
        /// </summary>
        internal static string pwiz_Skyline_Model_Results_Scoring_NextGenStandardSignalNoiseCalc {
            get {
                return ResourceManager.GetString("pwiz.Skyline.Model.Results.Scoring.NextGenStandardSignalNoiseCalc", resourceCulture);
            }
        }
    }
}
