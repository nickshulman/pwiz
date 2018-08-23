using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using CommandLine;
using CommandLine.Text;
using SkylineTool;

namespace ToolServiceCmd
{
    class GetReportCommand : BaseCommand<GetReportCommand.Options>
    {
        public override int PerformCommand(Options options)
        {
            var client = new SkylineToolClient(options.ConnectionName, "foo");
            IReport report;
            if (!string.IsNullOrEmpty(options.ReportName))
            {
                report = client.GetReport(options.ReportName);
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

        [Verb("GetReport", HelpText = "Outputs a report from Skyline. Defaults to reading the report definition from stdin.")]
        public class Options : BaseOptions
        {
            [Option(HelpText = "Use the named report instead of reading from stdin.")]
            public string ReportName { get; set; }
        }

    }
}
