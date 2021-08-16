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

        [ParamsSource(nameof(ValuesForLevel))]
        public int Level { get; set; }
        public IEnumerable<int> ValuesForLevel => new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 };

        [Benchmark]
        public async Task Compress()
        {
            await Archiver.CompressAsync(_toCompressPath, Level, true, false, false, false, Path.Combine(_toCompressPath + Level), false, false);
        }

        [Benchmark]
        public async Task Decompress()
        {
            await Archiver.DecompressAsync(Path.Combine(_toCompressPath + Level), true, false, false, "", false, false);
        }
    }
}
