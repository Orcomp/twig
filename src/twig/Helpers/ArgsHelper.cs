namespace twig
{
    using System;
    using System.Linq;

    public static class ArgsHelper
    {
        public static string[] HandleArgs(string[] args, int defaultVal)
        {
            var argsList = args.ToList();
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

