using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace ToolServiceCmd
{
    class Program
    {
        static int Main(string[] args)
        {
            var commands = new ICommand[]
            {
                new GetReportCommand(),
                new ImportAnnotationsCommand(),
            };
            var parseResult = Parser.Default.ParseArguments(args, commands.Select(c=>c.OptionsType).ToArray());
            Parsed<object> parsed = parseResult as Parsed<object>;
            if (parsed == null)
                return 1;
            foreach (var command in commands)
            {
                if (parsed.Value.GetType().IsAssignableFrom(command.OptionsType))
                {
                    return command.PerformCommand(parsed.Value);
                }
            }
            return 1;
        }
    }
}
