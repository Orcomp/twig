namespace wig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Spectre.Console;

    public static class VerboseLogger
    {
        public static void ShowLog(string sourcePath, string resultPath, System.Diagnostics.Stopwatch watch)
        {
            var elapsedTime = watch.ElapsedMilliseconds;
            var fileName = Path.GetFileName(sourcePath);
            var originalSize = new FileInfo(sourcePath).Length; 
            var compressedSize = new FileInfo(resultPath).Length; 
            var ratio = originalSize / compressedSize;

            AnsiConsole.WriteLine($"Compressed {fileName} in {elapsedTime} ms. Original: {originalSize/1024} KB. Compressed {compressedSize/1024} KB. Ratio: {ratio}");
        }
    }
}
