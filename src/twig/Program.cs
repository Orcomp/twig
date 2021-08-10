namespace twig
{
    using System;
    using System.Threading.Tasks;
    using Spectre.Console.Cli;

    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var processedArgs = ArgsHelper.HandleArgs(args, 100);
            var app = new CommandApp<DefaultCommand>();
            app.Configure(config =>
            {
                config.SetApplicationName("twig");
            });

            return await app.RunAsync(processedArgs);
        }
    }
}
