using System;
using CommandLine;
using SkylineTool;

namespace ToolServiceCmd
{
    [Verb("GetReport", HelpText = "Outputs a report from Skyline. Defaults to reading the report definition from stdin.")]
    internal class GetReportCommand : BaseCommand
    {
        [Option(HelpText = "Use the named report instead of reading from stdin.")]
        public string ReportName { get; set; }

        public override int PerformCommand()
        {
            //System.Diagnostics.Debugger.Launch();

            using (var client = GetSkylineToolClient())
            {
                IReport report;
                if (!string.IsNullOrEmpty(ReportName))
                {
                    report = client.GetReport(ReportName);
                }
                else
                {
                    string reportDefinition = Console.In.ReadToEnd();
                    report = client.GetReportFromDefinition(reportDefinition);
                }
                char sep = ',';
                Console.Out.WriteLine(DsvWriter.ToDsvRow(sep, report.ColumnNames));
                for (int iRow = 0; iRow < report.Cells.Length; iRow++)
                {
                    Console.Out.WriteLine(DsvWriter.ToDsvRow(sep, report.Cells[iRow]));
                }
                return 0;
            }
        }
    }
}
