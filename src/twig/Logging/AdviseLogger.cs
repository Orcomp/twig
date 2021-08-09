namespace twig
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Spectre.Console;

    public static class AdviseLogger
    {
        public static async Task CheckForBestLevel(int duration, string path)
        {
            var bestLevel = 1;
            var tempPath = Path.Combine(Path.GetDirectoryName(path), "temp");
            try
            {
                for (int level = 1; level < 22; level++)
                {
                    var watch = Stopwatch.StartNew();
                    await Archiver.CompressAsync(path, level, true, false, false, tempPath, false);
                    watch.Stop();
                    if (watch.ElapsedMilliseconds <= duration)
                    {
                        AnsiConsole.MarkupLine($"[gray] Checking level {level} of 22. [/]");
                        bestLevel = level;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                Directory.Delete(tempPath, true);
            }

            AnsiConsole.MarkupLine($"[green] Advised compression level is {bestLevel}. [/]");
        }
    }
}
