namespace twig
{
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Spectre.Console;
    using ZstdNet;

    public static class Archiver
    {
        public static async Task CompressAsync(string path, int compressionLevel, bool overwrite, bool subfolder, bool verbose, string output, bool remove, ProgressTask task, long size = 0)
        {
            using var options = new CompressionOptions(compressionLevel);
            using var compressor = new Compressor(options);
            var attr = File.GetAttributes(path);

            if (attr.HasFlag(FileAttributes.Directory))
            {
                if (size == 0)
                {
                    size = FileHelper.GetDirectorySize(path, subfolder);
                }
                task.MaxValue = size;
                var filePaths = Directory.GetFiles(path);

                foreach (var filePath in filePaths.Where(filePaths => !filePaths.EndsWith(".zs")))
                {
                    await WriteCompressedDataAsync(filePath, compressor, overwrite, verbose, output);
                    task.Value += new FileInfo(filePath).Length;
                    RemoveOriginal(filePath, remove);
                }
                if (subfolder)
                {
                    var subfolders = new DirectoryInfo(path).GetDirectories();
                    foreach (var folder in subfolders)
                    {
                        var files = Directory.GetFiles(folder.ToString());
                        foreach (var file in files.Where(files => !files.EndsWith(".zs")))
                        {
                            await WriteCompressedDataAsync(file, compressor, overwrite, verbose, output);
                            task.Value += new FileInfo(file).Length;
                            RemoveOriginal(file, remove);
                        }
                    }
                }

                return;
            }

            task.MaxValue = new FileInfo(path).Length;
            await WriteCompressedDataAsync(path, compressor, overwrite, verbose, output);
            task.Value += new FileInfo(path).Length;
            RemoveOriginal(path, remove);
        }

        private static async Task WriteCompressedDataAsync(string path, Compressor compressor, bool overwrite, bool verbose, string output)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            byte[] data = await File.ReadAllBytesAsync(path);
            var compressedBytes = compressor.Wrap(data);
            if (File.Exists($"{path}.zs") && !overwrite)
            {
                AnsiConsole.WriteLine($"A compressed file with the same name {Path.GetFileNameWithoutExtension(path)} already exists. Use the -o | --overwrite parameter to force overwrite.");
                return;
            }
            var writer = await FileHelper.WriteFileAsync(compressedBytes, path, output, ".zs");
            watch.Stop();
            if (verbose)
            {
                VerboseLogger.ShowLog(path, writer, watch);
            }
        }

        public static async Task DecompressAsync(string path, bool overwrite, bool subfolder, string output, bool remove, ProgressTask task, long size = 0)
        {
            using var decompressor = new Decompressor();
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                string[] filePaths = Directory.GetFiles(path);
                if (size == 0)
                {
                  size = FileHelper.GetDirectorySize(path, subfolder, ".zs");
                }
                task.MaxValue = size;
                foreach (var filePath in filePaths.Where(filePaths => filePaths.EndsWith(".zs")))
                {
                    await WriteDecompressedDataAsync(filePath, decompressor, overwrite, output);
                    task.Value += new FileInfo(filePath).Length;
                    RemoveOriginal(filePath, remove);
                }
                if (subfolder)
                {
                    var subfolders = new DirectoryInfo(path).GetDirectories();
                    foreach (var folder in subfolders)
                    {
                        var files = Directory.GetFiles(folder.ToString());
                        foreach (var file in files.Where(files => files.EndsWith(".zs")))
                        {
                            await WriteDecompressedDataAsync(file, decompressor, overwrite, output);
                            task.Value += new FileInfo(file).Length;
                            RemoveOriginal(file, remove);
                        }
                    }
                }

                return;
            }

            task.MaxValue = new FileInfo(path).Length;
            await WriteDecompressedDataAsync(path, decompressor, overwrite, output);
            task.Value += new FileInfo(path).Length;
            RemoveOriginal(path, remove);
        }
        private static async Task WriteDecompressedDataAsync(string path, Decompressor decompressor, bool overwrite, string output)
        {
            byte[] compressedData = await File.ReadAllBytesAsync($"{path}");
            var decompressedBytes = decompressor.Unwrap(compressedData);
            var unpackingPath = Path.ChangeExtension(path, "");

            if (File.Exists($"{path}") && !overwrite)
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

        public static async Task RunArchiver(string path, int compressionLevel, bool overwrite, bool subfolder, bool verbose, string output, bool remove, ProgressTask task)
        {
            if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory) && path.EndsWith(".zs"))
            {
                await DecompressAsync(path, overwrite, subfolder, output, remove, task);
                return;
            }
            if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory) && !path.EndsWith(".zs"))
            {
                await CompressAsync(path, compressionLevel, overwrite, subfolder, verbose, output, remove, task);
                return;
            }

            var size = FileHelper.GetTotalSize(path, subfolder);
            
            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                var paths = Directory.GetFiles(path);
                if (subfolder)
                {
                    paths = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                }
                foreach (var p in paths)
                {
                    if (p.EndsWith(".zs"))
                    {
                        await DecompressAsync(p, overwrite, subfolder, output, remove, task, size);
                    }

                    if (!p.EndsWith(".zs"))
                    {
                        await CompressAsync(p, compressionLevel, overwrite, subfolder, verbose, output, remove, task, size);
                    }
                }
            }
        }

    }
}
