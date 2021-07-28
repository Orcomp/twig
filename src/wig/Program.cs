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
            [CommandArgument(0, "<path>")]
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
            [DefaultValue(3)]
            [Description("Compression level (1-22)")]
            public int CompressionLevel { get; set; }

            [CommandOption("-s|--subfolder")]
            [Description("Look into subfolder")]
            public bool Subfolder { get; set; }

            [CommandOption("-f|--folder")]
            [Description("Write to specified destination folder")]
            public string DestinationFolder { get; set; }

            public override ValidationResult Validate()
            {
                if (IsCompressionMode && IsDecompressionMode)
                {
                    return ValidationResult.Error("Only one operation (compress or decompress) can be selected at the same time.");
                }

                if (!IsCompressionMode && !IsDecompressionMode)
                {
                    return ValidationResult.Error("At least one operation should be specified");
                }

                if (!String.IsNullOrEmpty(DestinationFolder) && DestinationFolder.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                {
                    return ValidationResult.Error("Destination folder contains invalid characters");
                }

                if ( CompressionLevel > 22 || CompressionLevel < 1)
                {
                    return ValidationResult.Error("Invalid compression level (must be between 1 and 22).");
                }

                return base.Validate();
            }
        }

        public DefaultCommand(IAnsiConsole console)
        {
            _console = console;
        }

        public async Task WriteFileAsync(byte[] data, string path, string destination = "", string extension = "")
        {
            var fileName = Path.GetFileName(path) + extension;
            var currentDirectory = Path.GetDirectoryName(path);
            var directory = currentDirectory;

            if (!String.IsNullOrEmpty(destination))
            {
                var folder = Path.Combine(currentDirectory, destination);
                if (!Directory.Exists(folder))
                {
                    directory = Directory.CreateDirectory(folder).ToString();
                }
                else
                {
                    directory = folder;
                }
            }

            await using (FileStream fstream = new FileStream(Path.Combine(directory, fileName), FileMode.OpenOrCreate))
            {
                await fstream.WriteAsync(data, 0, data.Length);
            }
        }

        public async Task WriteCompressedDataAsync(string path, Compressor compressor, bool overwrite, string destination)
        {
            byte[] data = await File.ReadAllBytesAsync(path);
            var compressedBytes = compressor.Wrap(data);
            if (File.Exists($"{path}.zs") && !overwrite)
            { 
                AnsiConsole.WriteLine($"A compressed file with the same name {Path.GetFileNameWithoutExtension(path)} already exists. Use the -o | --overwrite parameter to force overwrite.");
                return;
            }
            if (path.EndsWith(".zs"))
            {
                await WriteFileAsync(compressedBytes, path, destination);
                return;
            }
            await WriteFileAsync(compressedBytes, path, destination, ".zs");
        }

        public async Task CompressAsync(string path, int compressionLevel, bool overwrite, bool subfolder, string destination)
        {
            using var options = new CompressionOptions(compressionLevel);
            using var compressor = new Compressor(options);
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                string[] filePaths = Directory.GetFiles(path);
               
                foreach (var filePath in filePaths)
                {
                    await WriteCompressedDataAsync(filePath, compressor, overwrite, destination);
                }
                if (subfolder)
                {
                    var subfolders = new DirectoryInfo(path).GetDirectories();
                    foreach (var folder in subfolders)
                    {
                        var files = Directory.GetFiles(folder.ToString());
                        foreach (var file in files)
                        {
                            await WriteCompressedDataAsync(file, compressor, overwrite, destination);
                        }
                    }
                }
                return;
            }
            
            await WriteCompressedDataAsync(path, compressor, overwrite, destination);
        }

        public async Task WriteDecompressedDataAsync(string path, Decompressor decompressor, bool overwrite, string destination)
        {
            if (!path.EndsWith(".zs"))
            {
                AnsiConsole.WriteLine($"Can't decompress {path}. Only files with extension '.ZS' can be decompressed");
                return;
            }

            byte[] compressedData = await File.ReadAllBytesAsync($"{path}");
            var decompressedBytes = decompressor.Unwrap(compressedData);
            var unpackingPath = Path.ChangeExtension(path, "");

            if (File.Exists($"{path}") && !overwrite)
            {
                AnsiConsole.WriteLine($"A decompressed file with the same name {Path.GetFileNameWithoutExtension(path)} already exists. Use the -o | --overwrite parameter to force overwrite.");
                return;
            }

            await WriteFileAsync(decompressedBytes, unpackingPath, destination);
        }

        public async Task DecompressAsync(string path, bool overwrite, bool subfolder, string destination)
        {
            using var decompressor = new Decompressor();

            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                string[] filePaths = Directory.GetFiles(path);
                foreach (var filePath in filePaths)
                {
                    if (filePath.EndsWith(".zs"))
                    {
                        await WriteDecompressedDataAsync(filePath, decompressor, overwrite, destination);
                    }
                }
                if (subfolder)
                {
                    var subfolders = new DirectoryInfo(path).GetDirectories();
                    foreach (var folder in subfolders)
                    {
                        var files = Directory.GetFiles(folder.ToString());
                        foreach (var file in files)
                        {
                            if (file.EndsWith(".zs"))
                            {
                                await WriteDecompressedDataAsync(file, decompressor, overwrite, destination);
                            }
                        }
                    }
                }
                return;
            }

            await WriteDecompressedDataAsync(path, decompressor, overwrite, destination);
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
                await CompressAsync(settings.Path, settings.CompressionLevel, settings.Overwrite, settings.Subfolder, settings.DestinationFolder);
            }

            if (settings.IsDecompressionMode)
            {
                await DecompressAsync(settings.Path, settings.Overwrite, settings.Subfolder, settings.DestinationFolder);
            }


            _console.WriteLine("Task completed.");
            return 0;
        }
    }
}
