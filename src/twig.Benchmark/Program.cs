namespace twig.Benchmark
{
    using BenchmarkDotNet.Running;
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
