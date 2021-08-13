namespace twig
{
    using System;
    using System.Linq;

    public static class ArgsHelper
    {
        public static string[] HandleArgs(string[] args, int defaultVal)
        {
            var argsList = args.ToList();

            if (argsList.Contains("--register"))
            {
                FileAssociationHelper.RegisterForFileExtension(".zs");
                FileAssociationHelper.AddContextMenuOption("SOFTWARE\\Classes\\*\\shell\\twig", "Process with twig");
                FileAssociationHelper.AddContextMenuOption("SOFTWARE\\Classes\\Directory\\shell\\twig", "Process folder with twig");
                Console.WriteLine("Application has been registered.");
            }

            if (argsList.Contains("--unregister"))
            {
                FileAssociationHelper.UnregisterForFileExtension(".zs");
                FileAssociationHelper.RemoveContextMenuOption("SOFTWARE\\Classes\\*\\shell\\twig");
                FileAssociationHelper.RemoveContextMenuOption("SOFTWARE\\Classes\\Directory\\shell\\twig");
                Console.WriteLine("Application has been unregistered.");
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

