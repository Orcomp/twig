namespace twig
{
    using System.Diagnostics;
    using System.IO;
    using Spectre.Console;

    public static class VerboseLogger
    {
        public static void ShowLog(string sourcePath, string resultPath, Stopwatch watch)
        {
            var elapsedTime = watch.ElapsedMilliseconds;
            var fileName = Path.GetFileName(sourcePath);
            var originalSize = new FileInfo(sourcePath).Length; 
            var compressedSize = new FileInfo(resultPath).Length; 
            var ratio = originalSize / compressedSize;

            AnsiConsole.WriteLine($"Compressed {fileName} in {elapsedTime} ms. Original: {originalSize.ToFileSize()}. Compressed: {compressedSize.ToFileSize()}. Ratio: {ratio}");
        }
    }
}
