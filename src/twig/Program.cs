namespace twig
{
    using System.Threading.Tasks;
    using Spectre.Console.Cli;

    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var app = new CommandApp<DefaultCommand>();
            app.Configure(config =>
            {
                config.SetApplicationName("twig");
            });

            return await app.RunAsync(args);
        }
    }
}
