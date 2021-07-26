namespace wig
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Ecma335;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using ZstdNet;

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
            [CommandArgument(0, "path")]
            [Description("Path to the files")]
            public string Path { get; set; }

            [CommandOption("-c|--compress")]
            [Description("Compress files")]
            public bool IsCompressionMode { get; set; }

            [CommandOption("-d|--decompress")]
            [Description("Decompress files")]
            public bool IsDecompressionMode { get; set; }

            [CommandOption("-o|--overwrite")]
            [Description("Overwrite file / directory")]
            public bool Overwrite { get; set; }

            [CommandOption("-l|--level")]
            [Description("Compression level (1-22)")]
            public int CompressionLevel { get; set; }

            [CommandOption("-s|--subfolder")]
            [Description("Subfolder name")]
            public string Subfolder { get; set; }

            public override ValidationResult Validate()
            {
                if (IsCompressionMode && IsDecompressionMode)
                {
                    return ValidationResult.Error("Only one operation (compress or decompress) can be selected at the same time.");
                }

                if (!IsCompressionMode && !IsDecompressionMode)
                {
                    return ValidationResult.Error("At leat one operation should be specified");
                }

                return base.Validate();
            }
        }

        public DefaultCommand(IAnsiConsole console)
        {
            _console = console;
        }

        public async Task WriteFileAsync(byte[] data, string path, string subfolder, string extension = "")
        {
            var fileName = Path.GetFileName(path) + extension;
            var currentDirectory = Path.GetDirectoryName(path);
            var directory = currentDirectory;
            if (!string.IsNullOrEmpty(subfolder))
            {
                directory = Directory.CreateDirectory(Path.Combine(currentDirectory, subfolder)).ToString();
            }

            await using (FileStream fstream = new FileStream(Path.Combine(directory, fileName), FileMode.OpenOrCreate))
            {
                await fstream.WriteAsync(data, 0, data.Length);
            }
        }

        public async Task WriteCompressedDataAsync(string path, Compressor compressor, bool overwrite, string subfolder)
        {
            byte[] data = await File.ReadAllBytesAsync(path);
            var compressedBytes = compressor.Wrap(data);
            if (File.Exists($"{path}.zs") && !overwrite)
            {
                AnsiConsole.WriteLine("A compressed file with the same name already exists. Use the -o | --overwrite parameter to force overwrite.");
                return;
            }

            await WriteFileAsync(compressedBytes, path, subfolder, ".zs");
        }

        public async Task CompressAsync(string path, int compressionLevel, bool overwrite, string subfolder)
        {
            using var options = new CompressionOptions(compressionLevel);
            using var compressor = new Compressor(options);
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                string[] filePaths = Directory.GetFiles(path);

                foreach (var filePath in filePaths)
                {
                    await WriteCompressedDataAsync(filePath, compressor, overwrite, subfolder);
                }
            }

            await WriteCompressedDataAsync(path, compressor, overwrite, subfolder);
        }

        public async Task WriteDecompressedDataAsync(string path, Decompressor decompressor, bool overwrite, string subfolder)
        {
            if (!path.EndsWith(".zs"))
            {
                AnsiConsole.WriteLine("Only files with extension '.ZS' can be decompressed");
                return;
            }

            byte[] compressedData = await File.ReadAllBytesAsync($"{path}");
            var decompressedBytes = decompressor.Unwrap(compressedData);
            var unpackingPath = Path.ChangeExtension(path, "");

            if (File.Exists($"{path}") && !overwrite)
            {
                AnsiConsole.WriteLine("A decompressed file with the same name already exists. Use the -o | --overwrite parameter to force overwrite.");
                return;
            }

            await WriteFileAsync(decompressedBytes, unpackingPath, subfolder);
        }

        public async Task DecompressAsync(string path, bool overwrite, string subfolder)
        {
            using var decompressor = new Decompressor();

            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                string[] filePaths = Directory.GetFiles(path);
                foreach (var filePath in filePaths)
                {
                    await WriteDecompressedDataAsync(filePath, decompressor, overwrite, subfolder);
                }
            }

            await WriteDecompressedDataAsync(path, decompressor, overwrite, subfolder);
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
                await CompressAsync(settings.Path, settings.CompressionLevel, settings.Overwrite, settings.Subfolder);
            }

            if (settings.IsDecompressionMode)
            {
                await DecompressAsync(settings.Path, settings.Overwrite, settings.Subfolder);
            }

            _console.WriteLine("Task completed.");
            return 0;
        }
    }
}
