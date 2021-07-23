namespace Wig.Zstd
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
            [CommandArgument(0, "[path]")]
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
            public string Subfolder{ get; set; }
        }

        public DefaultCommand(IAnsiConsole console)
        {
            _console = console;
        }

        public async Task WriteFileAsync(byte[] data, string path, string subfolder, string extension)
        {
            var fileName = Path.GetFileName(path);
            var currentDirectory = Path.GetDirectoryName(path);

            var directory = Directory.CreateDirectory($"{currentDirectory}/{subfolder}");

            await using (FileStream fstream = new FileStream($"{directory}/{fileName}{extension}", FileMode.OpenOrCreate))
            {
                await fstream.WriteAsync(data, 0, data.Length);
            }
        }

        public async Task CompressingAsync(string path, int compressionLevel, bool overwrite, string subfolder)
        {
            using var options = new CompressionOptions(compressionLevel);
            using var compressor = new Compressor(options);
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                string[] filePaths = Directory.GetFiles(path);

                foreach (var filePath in filePaths)
                {
                    byte[] data = await File.ReadAllBytesAsync(filePath);

                    var compData = compressor.Wrap(data);

                    if (File.Exists($"{filePath}.zs") && !overwrite)
                    {
                        AnsiConsole.WriteLine("A compressed file with the same name already exists. Use the -o | --overwrite parameter to force overwrite.");
                        return;
                    }

                    await WriteFileAsync(compData, path, subfolder, "zs");
                }
            }

            byte[] sourceData = await File.ReadAllBytesAsync(path);

            var compressedData = compressor.Wrap(sourceData);

            if (File.Exists($"{path}.zs") && !overwrite)
            {
                AnsiConsole.WriteLine("A compressed file with the same name already exists. Use the -o | --overwrite parameter to force overwrite.");
                return;
            }

            await WriteFileAsync(compressedData, path, subfolder, ".zs");
        }

        public async Task DecompressingAsync(string path, bool overwrite, string subfolder)
        {
            using var decompressor = new Decompressor();

            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                string[] filePaths = Directory.GetFiles(path);
                foreach (var filePath in filePaths)
                {
                    if (filePath.Contains(".zs"))
                    {
                        byte[] compressedData = await File.ReadAllBytesAsync($"{filePath}");
                        var decompressedData = decompressor.Unwrap(compressedData);
                        var unpackingPath = Path.ChangeExtension(filePath, "");

                        if (File.Exists($"{path}") && !overwrite)
                        {
                            AnsiConsole.WriteLine("A decompressed file with the same name already exists. Use the -o | --overwrite parameter to force overwrite.");
                            return;
                        }

                        await WriteFileAsync(decompressedData, unpackingPath, subfolder, "");
                    }
                }
            }
            if (path.Contains(".zs"))
            {
                byte[] compressedData = await File.ReadAllBytesAsync($"{path}");
                var decompressedData = decompressor.Unwrap(compressedData);
                var unpackingPath = Path.ChangeExtension(path, "");

                if (File.Exists($"{path}") && !overwrite)
                {
                    AnsiConsole.WriteLine("A decompressed file with the same name already exists. Use the -o | --overwrite parameter to force overwrite.");
                    return;
                }

                await WriteFileAsync(decompressedData, unpackingPath, subfolder, "");
            }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (settings.IsCompressionMode)
            {
                await CompressingAsync(settings.Path, settings.CompressionLevel, settings.Overwrite, settings.Subfolder);

            }

            if (settings.IsDecompressionMode)
            {
                await DecompressingAsync(settings.Path, settings.Overwrite, settings.Subfolder);
            }

            _console.WriteLine("Task completed.");
            return 0;
        }
    }
}
