namespace twig
{
    using System;
    using System.Threading.Tasks;
    using Spectre.Console;

    public static class ProgressExtensions
    {
        public static async Task StartExecuteAsync(this Progress consoleProgress, string description, Func<ProgressTask, Task> func)
        {
            await consoleProgress.StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[green] {description} [/]");
                await func(task);
            });
        }
    }
}
