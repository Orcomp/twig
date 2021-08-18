namespace twig
{
    using System;
    using System.Linq;
    using Spectre.Console;

    public static class ArgsHelper
    {
        public static string[] HandleArgs(string[] args, int defaultVal)
        {
            var argsList = args.ToList();

            if((argsList.Contains("--register") && argsList.Contains("--unregister")))
            {
                AnsiConsole.MarkupLine("[red]Cannot process register and unregister commands at the same time.[/]");
                Environment.Exit(0);
            }

            if (argsList.Contains("--register"))
            {
                FileAssociationHelper.RegisterForFileExtension(".zs");
                FileAssociationHelper.AddContextMenuOption("SOFTWARE\\Classes\\*\\shell\\twig", "Process with twig");
                FileAssociationHelper.AddContextMenuOption("SOFTWARE\\Classes\\Directory\\shell\\twig", "Process folder with twig");
                AnsiConsole.MarkupLine("[green]Application has been registered. [/]");
            }

            if (argsList.Contains("--unregister"))
            {
                FileAssociationHelper.UnregisterForFileExtension(".zs");
                FileAssociationHelper.RemoveContextMenuOption("SOFTWARE\\Classes\\*\\shell\\twig");
                FileAssociationHelper.RemoveContextMenuOption("SOFTWARE\\Classes\\Directory\\shell\\twig");
                AnsiConsole.MarkupLine("[green]Application has been unregistered. [/]");
            }

            if (argsList.Count == 1 && (argsList.Contains("--unregister") || argsList.Contains("--register")))
            {
                Environment.Exit(0);
            }

            if (!argsList.Contains("--advise"))
            {
                return args;
            }

            var adviseIdx = argsList.IndexOf("--advise");

            if (argsList.Count > adviseIdx + 1 && Int32.TryParse(argsList[adviseIdx + 1], out var val))
            {
                return argsList.ToArray();
            }

            argsList.Insert(adviseIdx + 1, defaultVal.ToString());
            return argsList.ToArray();
        }
    }
}

