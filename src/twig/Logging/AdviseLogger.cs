namespace twig
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Spectre.Console;

    public static class AdviseLogger
    {
        public static async Task CheckForBestLevel(DefaultCommand.Settings settings)
        {
            var bestLevel = 1;
            var appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var tempDirectory = "WildGums\\twig\\temp";
            settings.OutputPath = Path.Combine(appdataPath, tempDirectory);
            settings.Overwrite = true;
            var path = settings.Path;
            var duration = settings.AdviseDuration;
            try
            {
                for (int level = 1; level < 22; level++)
                {
                    settings.CompressionLevel = level;
                    var watch = Stopwatch.StartNew();
                    await Archiver.CompressAsync(settings);
                    watch.Stop();
                    if (watch.ElapsedMilliseconds <= duration)
                    {
                        AnsiConsole.MarkupLine($"[gray] Checking level {level} of 22. [/]");
                        AnsiConsole.MarkupLine($"[gray] Finished checking level {level} in {watch.ElapsedMilliseconds} ms [/]");
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
                Directory.Delete(settings.OutputPath, true);
            }

            AnsiConsole.MarkupLine($"[green] The best compression level for {path} and duration {duration} is: {bestLevel}. [/]");
        }
    }
}
