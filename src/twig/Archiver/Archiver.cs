namespace twig
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Spectre.Console;
    using ZstdNet;

    public static class Archiver
    {
        public static event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public static event EventHandler<ProgressStartEventArgs> ProgressStart;
        public static event EventHandler<ProgressFinishEventArgs> ProgressFinish;

        public static IDisposable ReportProgress(ProgressTask task)
        {
            return new ProgressBarDisposable(task);
        }

        public static async Task CompressAsync(string path, int compressionLevel, bool overwrite, bool subfolder, bool replicate, bool verbose, string output, bool remove, bool automaticMode = false, long size = 0)
        {
            using var options = new CompressionOptions(compressionLevel);
            using var compressor = new Compressor(options);
            var attr = File.GetAttributes(path);

            if (attr.HasFlag(FileAttributes.Directory))
            {
                if (size == 0)
                {
                    size = FileHelper.GetDirectorySize(path, subfolder);
                    ProgressStart?.Invoke(null, new ProgressStartEventArgs(size));
                }

                var filePaths = Directory.GetFiles(path);

                foreach (var filePath in filePaths.Where(filePaths => !filePaths.EndsWith(".zs")))
                {
                    await WriteCompressedDataAsync(filePath, compressor, overwrite, verbose, output);
                    ProgressChanged?.Invoke(null, new ProgressChangedEventArgs(new FileInfo(filePath).Length));
                    RemoveOriginal(filePath, remove);
                }
                if (subfolder)
                {
                    var subfolders = new DirectoryInfo(path).GetDirectories();

                    foreach (var folder in subfolders)
                    {
                        var files = Directory.GetFiles(folder.ToString());
                        var outputPath = replicate ? Path.Combine(output, folder.Name) : output;
                        foreach (var file in files.Where(files => !files.EndsWith(".zs")))
                        {
                            await WriteCompressedDataAsync(file, compressor, overwrite, verbose, outputPath);
                            ProgressChanged?.Invoke(null, new ProgressChangedEventArgs(new FileInfo(file).Length));
                            RemoveOriginal(file, remove);
                        }
                    }
                }

                if (!automaticMode)
                {
                    ProgressFinish?.Invoke(null, new ProgressFinishEventArgs());
                }

                return;
            }

            if (!automaticMode)
            {
                ProgressStart?.Invoke(null, new ProgressStartEventArgs(new FileInfo(path).Length));
            }
            await WriteCompressedDataAsync(path, compressor, overwrite, verbose, output);
            ProgressChanged?.Invoke(null, new ProgressChangedEventArgs(new FileInfo(path).Length));
            if (!automaticMode)
            {
                ProgressFinish?.Invoke(null, new ProgressFinishEventArgs());
            }
            RemoveOriginal(path, remove);
        }

        private static async Task WriteCompressedDataAsync(string path, Compressor compressor, bool overwrite, bool verbose, string output)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            byte[] data = await File.ReadAllBytesAsync(path);
            var compressedBytes = compressor.Wrap(data);
            if (File.Exists($"{path}.zs") && !overwrite)
            {
                AnsiConsole.WriteLine($"A compressed file with the same name {Path.GetFileNameWithoutExtension(path)} already exists. Use the -f | --force parameter to force overwrite.");
                return;
            }
            var writer = await FileHelper.WriteFileAsync(compressedBytes, path, output, ".zs");
            watch.Stop();
            if (verbose)
            {
                VerboseLogger.ShowLog(path, writer, watch);
            }
        }

        public static async Task DecompressAsync(string path, bool overwrite, bool subfolder, bool replicate, string output, bool remove, bool automaticMode = false, long size = 0)
        {
            using var decompressor = new Decompressor();
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                string[] filePaths = Directory.GetFiles(path);
                if (size == 0)
                {
                    size = FileHelper.GetDirectorySize(path, subfolder, ".zs");
                    ProgressStart?.Invoke(null, new ProgressStartEventArgs(size));
                }

                foreach (var filePath in filePaths.Where(filePaths => filePaths.EndsWith(".zs")))
                {
                    await WriteDecompressedDataAsync(filePath, decompressor, overwrite, output);
                    ProgressChanged?.Invoke(null, new ProgressChangedEventArgs(new FileInfo(filePath).Length));
                    RemoveOriginal(filePath, remove);
                }
                if (subfolder)
                {
                    var subfolders = new DirectoryInfo(path).GetDirectories();
                    foreach (var folder in subfolders)
                    {
                        var files = Directory.GetFiles(folder.ToString());
                        var outputPath = replicate ? Path.Combine(output, folder.Name) : output;
                        foreach (var file in files.Where(files => files.EndsWith(".zs")))
                        {
                            await WriteDecompressedDataAsync(file, decompressor, overwrite, outputPath);
                            ProgressChanged?.Invoke(null, new ProgressChangedEventArgs(new FileInfo(file).Length));
                            RemoveOriginal(file, remove);
                        }
                    }
                }

                if (!automaticMode)
                {
                    ProgressFinish?.Invoke(null, new ProgressFinishEventArgs());
                }

                return;
            }

            if (!automaticMode)
            {
                ProgressStart?.Invoke(null, new ProgressStartEventArgs(new FileInfo(path).Length));
            }
            await WriteDecompressedDataAsync(path, decompressor, overwrite, output);
            ProgressChanged?.Invoke(null, new ProgressChangedEventArgs(new FileInfo(path).Length));
            if (!automaticMode)
            {
                ProgressFinish?.Invoke(null, new ProgressFinishEventArgs());
            }
            RemoveOriginal(path, remove);
        }
        private static async Task WriteDecompressedDataAsync(string path, Decompressor decompressor, bool overwrite, string output)
        {
            byte[] compressedData = await File.ReadAllBytesAsync($"{path}");
            var decompressedBytes = decompressor.Unwrap(compressedData);
            var unpackingPath = Path.ChangeExtension(path, "");

            if (File.Exists($"{unpackingPath}") && !overwrite)
            {
                AnsiConsole.WriteLine($"A decompressed file with the same name {Path.GetFileNameWithoutExtension(path)} already exists. Use the -o | --overwrite parameter to force overwrite.");
                return;
            }
            await FileHelper.WriteFileAsync(decompressedBytes, unpackingPath, output);
        }

        public static void RemoveOriginal(string path, bool remove)
        {
            if (remove)
            {
                File.Delete(path);
            }
        }

        public static async Task RunArchiver(string path, int compressionLevel, bool overwrite, bool subfolder, bool replicate, bool verbose, string output, bool remove, ProgressTask task)
        {
            if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory) && path.EndsWith(".zs"))
            {
                await DecompressAsync(path, overwrite, subfolder, replicate, output, remove);
                return;
            }
            if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory) && !path.EndsWith(".zs"))
            {
                await CompressAsync(path, compressionLevel, overwrite, subfolder, replicate, verbose, output, remove);
                return;
            }

            var size = FileHelper.GetTotalSize(path, subfolder);
            ProgressStart?.Invoke(null, new ProgressStartEventArgs(size));

            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                var paths = Directory.GetFiles(path);
                if (subfolder)
                {
                    paths = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                }
                foreach (var p in paths)
                {
                    var dir = Path.GetDirectoryName(p) == path ? "" : Path.GetFileName(Path.GetDirectoryName(p));
                    var outputPath = replicate ? Path.Combine(output, dir) : output;

                    if (p.EndsWith(".zs"))
                    {
                        await DecompressAsync(p, overwrite, subfolder, replicate, outputPath, remove, true);
                    }

                    if (!p.EndsWith(".zs"))
                    {
                        await CompressAsync(p, compressionLevel, overwrite, subfolder, replicate, verbose, outputPath, remove, true);
                    }
                }
                ProgressFinish?.Invoke(null, new ProgressFinishEventArgs());
            }
        }

    }
}
