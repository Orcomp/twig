namespace twig
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Spectre.Console;
    using Spectre.Console.Cli;

    public sealed class DefaultCommand : AsyncCommand<DefaultCommand.Settings>
    {
        private readonly IAnsiConsole _console;

        public sealed class Settings : CommandSettings
        {
            [CommandArgument(0, "<path>")]
            [Description("Path to the files")]
            public string Path { get; set; }

            [CommandOption("-c|--compress")]
            [Description("Compress files")]
            public bool IsCompressionMode { get; set; }

            [CommandOption("-d|--decompress")]
            [Description("Decompress files")]
            public bool IsDecompressionMode { get; set; }

            [CommandOption("-f|--force")]
            [Description("Force overwrite file or directory")]
            public bool Overwrite { get; set; }

            [CommandOption("-l|--level")]
            [DefaultValue(3)]
            [Description("Compression level (1-22)")]
            public int CompressionLevel { get; set; }

            [CommandOption("-r|--recursive")]
            [Description("Recursively look into subfolders")]
            public bool Subfolder { get; set; }

            [CommandOption("-o|--output")]
            [Description("Write to specified path")]
            public string OutputPath { get; set; }

            [CommandOption("--remove")]
            [Description("Remove the original file after successfully compressing")]
            public bool Remove { get; set; }

            [CommandOption("-v|--verbose")]
            [Description("Print information after compressing each file")]
            public bool Verbose { get; set; }

            [CommandOption("--advise")]
            [Description("Find the best compression level given a file and duration")]
            public int Duration { get; set; }

            public override ValidationResult Validate()
            {
                if (IsCompressionMode && IsDecompressionMode)
                {
                    return ValidationResult.Error("Only one operation (compress or decompress) can be selected at the same time.");
                }

                if (!String.IsNullOrEmpty(OutputPath) && OutputPath.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                {
                    return ValidationResult.Error("Destination folder contains invalid characters");
                }

                if (IsCompressionMode && !File.GetAttributes(Path).HasFlag(FileAttributes.Directory) && Path.EndsWith(".zs"))
                {
                    return ValidationResult.Error($"Can't compress {Path}. This file is already compressed.");
                }

                if (IsDecompressionMode && !File.GetAttributes(Path).HasFlag(FileAttributes.Directory) && !Path.EndsWith(".zs"))
                {
                    return ValidationResult.Error($"Can't decompress {Path}. Only files with extension '.zs' can be decompressed");
                }

                if (CompressionLevel > 22 || CompressionLevel < 1)
                {
                    return ValidationResult.Error("Invalid compression level (must be between 1 and 22).");
                }

                if (Duration > 0 && Path.EndsWith(".zs"))
                {
                    return ValidationResult.Error("Advise mode requires an uncompressed file.");
                }

                if (Duration > 0 && IsCompressionMode)
                {

                    return ValidationResult.Error("Cannot process advise and compress commands at the same time.");
                }

                //if (IsDecompressionMode && Directory.GetFiles(Path, "*.zs", SearchOption.AllDirectories).Length == 0)
                //{
                //    return ValidationResult.Error($"Nothing to decompress in {Path}");
                //}

                //if (IsCompressionMode && Directory.GetFiles(Path, "*.*", SearchOption.AllDirectories).Count(name => !name.EndsWith(".zs")) == 0)
                //{
                //    return ValidationResult.Error($"Nothing to compress in {Path}");
                //}

                return base.Validate();
            }
        }

        public DefaultCommand(IAnsiConsole console)
        {
            _console = console;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (string.IsNullOrEmpty(settings.Path) || settings.Path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
            {
                _console.WriteLine("Path is empty or contains invalid characters.");
                return 0;
            }

            if (settings.IsCompressionMode)
            {
                await AnsiConsole.Progress()
                    .StartExecuteAsync("Compressing...", async (task) => await Archiver.CompressAsync(
                            settings.Path,
                            settings.CompressionLevel,
                            settings.Overwrite,
                            settings.Subfolder,
                            settings.Verbose,
                            settings.OutputPath,
                            settings.Remove
                        )
                    );
            }

            if (settings.IsDecompressionMode)
            {
                await AnsiConsole.Progress()
                    .StartExecuteAsync("Decompressing...", async (task) => await Archiver.DecompressAsync(
                            settings.Path,
                            settings.Overwrite,
                            settings.Subfolder,
                            settings.OutputPath,
                            settings.Remove
                        )
                    );
            }

            if (!settings.IsCompressionMode && !settings.IsDecompressionMode && settings.Duration == 0)
            {
                await AnsiConsole.Progress()
                    .StartExecuteAsync("Processing...", async (task) => await Archiver.RunArchiver(
                            settings.Path,
                            settings.CompressionLevel,
                            settings.Overwrite,
                            settings.Subfolder,
                            settings.Verbose,
                            settings.OutputPath,
                            settings.Remove,
                            task
                        )
                    );
            }

            if (settings.Duration > 0 && !settings.IsCompressionMode && !settings.IsDecompressionMode)
            {
                AnsiConsole.WriteLine("Looking for the best compression level for given duration. Please wait...") ;
                await AdviseLogger.CheckForBestLevel(
                            settings.Duration,
                            settings.Path
                        );
            }

            _console.WriteLine("Task completed.");
            return 0;
        }
    }
}
