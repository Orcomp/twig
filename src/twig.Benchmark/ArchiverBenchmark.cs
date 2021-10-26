namespace twig.Benchmark
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using Catel.IO;
    using twig;

    public class ArchiverBenchmark
    {
        private readonly string _toCompressPath = Path.Combine(
            RandomDataHelper.AppDataPath,
            RandomDataHelper.TempDirectory,
            RandomDataHelper.FileName);

        private DefaultCommand.Settings Settings { get; set; } = new ();

        [ParamsSource(nameof(ValuesForLevel))]
        public int Level { get; set; }
        public IEnumerable<int> ValuesForLevel => new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 };

        [Benchmark]
        public async Task Compress()
        {
            Settings.Path = _toCompressPath;
            Settings.CompressionLevel = Level;
            Settings.Overwrite = true;
            Settings.OutputPath = Path.Combine(_toCompressPath + Level);

            await Archiver.CompressAsync(Settings);
        }

        [Benchmark]
        public async Task Decompress()
        {
            Settings.Path = Path.Combine(_toCompressPath + Level);
            Settings.Overwrite = true;
            await Archiver.DecompressAsync(Settings);
        }
    }
}
