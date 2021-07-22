namespace Wig.Zstd
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Spectre.Console;
    using Spectre.Console.Cli;

    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var app = new CommandApp<DefaultCommand>();
            app.Configure(config =>
            {
                config.SetApplicationName("dotnet example");
            });

            return await app.RunAsync(args);
        }
    }

    public sealed class DefaultCommand : AsyncCommand<DefaultCommand.Settings>
    {
        private readonly IAnsiConsole _console;

        public sealed class Settings : CommandSettings
        {
            [CommandArgument(0, "[EXAMPLE]")]
            [Description("The example to run.\nIf none is specified, all examples will be listed")]
            public string Name { get; set; }

            [CommandOption("-l|--list")]
            [Description("Lists all available examples")]
            public bool List { get; set; }

            [CommandOption("-a|--all")]
            [Description("Runs all available examples")]
            public bool All { get; set; }

            [CommandOption("-s|--source")]
            [Description("Show example source code")]
            public bool Source { get; set; }

            [CommandOption("--select")]
            [Description("Show example source code")]
            public bool Select { get; set; }
        }

        public DefaultCommand(IAnsiConsole console)
        {
            _console = console;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            _console.WriteLine("Hello World!");

            return 0;
        }
    }
}
