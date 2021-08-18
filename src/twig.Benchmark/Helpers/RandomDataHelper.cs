namespace twig.Benchmark
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    public static class RandomDataHelper
    {
        public static string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static string TempDirectory = "WildGums\\twig\\temp";

        public static string FileName = "testData.txt";

        private static readonly Random Random = new Random();

        public static void CreateRandomTextFile(int sizeKb)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_+=<>?/,.#$%^&*()";
            var str = new string(Enumerable.Repeat(chars, sizeKb * 1024)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
            var bytes = Encoding.ASCII.GetBytes(str);
            var directory = Path.Combine(AppDataPath, TempDirectory);
            Directory.CreateDirectory(directory);

            using (FileStream fstream = new FileStream(Path.Combine(directory, FileName), FileMode.Create))
            {
                fstream.Write(bytes, 0, str.Length);
            }
        }

        public static void CleanupTempDirectory()
        {
            var toDeletePath = Path.Combine(AppDataPath, TempDirectory);
            Directory.Delete(toDeletePath, true);
        }
    }
}
