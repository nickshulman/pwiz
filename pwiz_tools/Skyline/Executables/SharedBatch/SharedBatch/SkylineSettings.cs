﻿using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using SharedBatch.Properties;

namespace SharedBatch
{
    public enum SkylineType
    {
        Skyline,
        SkylineDaily,
        Local,
        Custom
    }
    
    public class SkylineSettings
    {
        // The skyline installation to use when a configuration is run

        public SkylineSettings(SkylineType type, string folderPath = "")
        {
            Type = type;

            bool skylineAdminInstallation = !string.IsNullOrEmpty(Settings.Default.SkylineAdminCmdPath);
            bool skylineWebInstallation = !string.IsNullOrEmpty(Settings.Default.SkylineRunnerPath);
            bool skylineDailyAdminInstallation = !string.IsNullOrEmpty(Settings.Default.SkylineDailyAdminCmdPath);
            bool skylineDailyWebInstallation = !string.IsNullOrEmpty(Settings.Default.SkylineDailyRunnerPath);

            switch (type)
            {
                case SkylineType.Skyline:
                    if (skylineWebInstallation)
                        CmdPath = Settings.Default.SkylineRunnerPath;
                    else if (skylineAdminInstallation)
                        CmdPath = Settings.Default.SkylineAdminCmdPath;
                    break;
                case SkylineType.SkylineDaily:
                    if (skylineDailyWebInstallation)
                        CmdPath = Settings.Default.SkylineDailyRunnerPath;
                    else if (skylineDailyAdminInstallation)
                        CmdPath = Settings.Default.SkylineDailyAdminCmdPath;
                    break;
                case SkylineType.Local:
                    CmdPath = Settings.Default.SkylineLocalCommandPath;
                    break;
                case SkylineType.Custom:
                    CmdPath = Path.Combine(folderPath, SkylineInstallations.SkylineCmdExe);
                    break;
            }
        }

        public readonly SkylineType Type; // The type of skyline installation
        public readonly string CmdPath; // the path to a SkylineCmd or SkylineRunner

        public void Validate()
        {
            if (!File.Exists(CmdPath))
            {
                switch (Type)
                {
                    case SkylineType.Skyline:
                        throw new ArgumentException(Resources.SkylineSettings_Validate_Could_not_find_a_Skyline_installation_on_this_computer_ + Environment.NewLine +
                                                    Resources.SkylineSettings_Validate_Please_try_a_different_Skyline_option_);
                    case SkylineType.SkylineDaily:
                        throw new ArgumentException(Resources.SkylineSettings_Validate_Could_not_find_a_Skyline_daily_installation_on_this_computer_ + Environment.NewLine +
                              Resources.SkylineSettings_Validate_Please_try_a_different_Skyline_option_); 
                    case SkylineType.Local:
                        throw new ArgumentException(string.Format(Resources.SkylineSettings_Validate_Could_not_find__0__at_this_location___1_, SkylineInstallations.SkylineCmdExe, CmdPath));
                    case SkylineType.Custom:
                        throw new ArgumentException(string.Format(Resources.SkylineSettings_Validate_Could_not_find_a_Skyline_installation_at_this_location___0_, Path.GetDirectoryName(CmdPath)) + Environment.NewLine +
                                                    string.Format(Resources.SkylineSettings_Validate_Please_select_a_folder_containing__0__, SkylineInstallations.SkylineCmdExe));
                }
            }
        }
        
        private enum Attr
        {
            Type,
            CmdPath,
        }
        
        public static SkylineSettings ReadXml(XmlReader reader)
        {
            var type = Enum.Parse(typeof(SkylineType), reader.GetAttribute(Attr.Type), false);
            var cmdPath = Path.GetDirectoryName(reader.GetAttribute(Attr.CmdPath));
            return new SkylineSettings((SkylineType)type, cmdPath);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("config_skyline_settings");
            writer.WriteAttributeIfString(Attr.Type, Type.ToString());
            writer.WriteAttributeIfString(Attr.CmdPath, CmdPath);
            writer.WriteEndElement();
        }

        public int[] GetVersion()
        {
            var output = "";
            Process cmd = new Process();
            cmd.StartInfo.FileName = CmdPath;
            cmd.StartInfo.Arguments = "--version";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.EnableRaisingEvents = true;
            cmd.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null && string.IsNullOrEmpty(output))
                {
                    output += e.Data;
                }
            };
            cmd.Start();
            cmd.BeginOutputReadLine();
            cmd.WaitForExit();
            
            var versionString = output.Split(' ');
            int i = 0;
            while (i < versionString.Length && !Int32.TryParse(versionString[i].Substring(0,1), out _)) i++;
            if (i == versionString.Length) throw new Exception("No parsable Skyline version found.");
            return ParseVersionFromString(versionString[i]);
        }

        private int[] ParseVersionFromString(string stringVersion)
        {
            var versionArray = stringVersion.Split('.');
            if (versionArray.Length != 4) throw new Exception("Error parsing Skyline version.");
            var versionNumbers = new int[versionArray.Length];
            for (int i = 0; i < versionArray.Length; i++)
                versionNumbers[i] = Int32.Parse(versionArray[i]);
            return versionNumbers;
        }

        public bool HigherVersion(string versionCutoff)
        {
            var cutoff = ParseVersionFromString(versionCutoff);
            var version = GetVersion();
            for (int i = 0; i < cutoff.Length; i++)
            {
                if (version[i] < cutoff[i]) return false;
            }
            return true;
        }

        protected bool Equals(SkylineSettings other)
        {
            return Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SkylineSettings)obj);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }
    }
}
