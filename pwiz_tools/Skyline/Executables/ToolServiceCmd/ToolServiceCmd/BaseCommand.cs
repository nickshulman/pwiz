using System;
using CommandLine;

namespace ToolServiceCmd
{
    public class BaseOptions
    {
        [Option(Required = true, HelpText = "Connection name from Skyline")]
        public string ConnectionName { get; set; }
    }

    public interface ICommand
    {
        Type OptionsType { get; }
        int PerformCommand(object options);
    }
 
    public abstract class BaseCommand<TOptions> : ICommand
    {
        Type ICommand.OptionsType
        {
            get { return typeof(TOptions); }
        }

        int ICommand.PerformCommand(object options)
        {
            return PerformCommand((TOptions) options);
        }

        public abstract int PerformCommand(TOptions options);
    }
}
