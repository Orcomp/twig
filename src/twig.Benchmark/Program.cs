namespace twig.Benchmark
{
    using System;
    using System.IO;
    using BenchmarkDotNet.Running;
    using Path = Catel.IO.Path;

    public class Program
    {
        public static void Main(string[] args)
        {
            RandomDataHelper.CreateRandomTextFile(20000);

            var summary = BenchmarkRunner.Run<ArchiverBenchmark>();

            RandomDataHelper.CleanupTempDirectory();
        }
    }
}
