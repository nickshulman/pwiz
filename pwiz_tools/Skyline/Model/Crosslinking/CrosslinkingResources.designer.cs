//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace pwiz.Skyline.Model.Crosslinking {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class CrosslinkingResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CrosslinkingResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("pwiz.Skyline.Model.Crosslinking.CrosslinkingResources", typeof(CrosslinkingResources).Assembly);
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
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected &apos;{0}&apos;.
        /// </summary>
        public static string CrosslinkSequenceParser_Expected_Expected___0__ {
            get {
                return ResourceManager.GetString("CrosslinkSequenceParser_Expected_Expected___0__", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to parse &apos;{0}&apos; as a number.
        /// </summary>
        public static string CrosslinkSequenceParser_ParseCrosslink_Unable_to_parse___0___as_a_number {
            get {
                return ResourceManager.GetString("CrosslinkSequenceParser_ParseCrosslink_Unable_to_parse___0___as_a_number", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid peptide sequence.
        /// </summary>
        public static string CrosslinkSequenceParser_ParseCrosslinkLibraryKey_Invalid_peptide_sequence {
            get {
                return ResourceManager.GetString("CrosslinkSequenceParser_ParseCrosslinkLibraryKey_Invalid_peptide_sequence", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} at position {1}.
        /// </summary>
        public static string ParseExceptionDetail_ToString__at_position__0_ {
            get {
                return ResourceManager.GetString("ParseExceptionDetail_ToString__at_position__0_", resourceCulture);
            }
        }
    }
}
