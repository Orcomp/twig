// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssemblyDirectoryHelper.cs" company="Simply Effective Solutions">
//   Copyright (c) 2008 - GFG Simply Effective Solutions. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Wig.Zstd.Tests
{
    using System;
    using Catel.IO;

    internal static class AssemblyDirectoryHelper
    {
        public static string GetCurrentDirectory()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            return directory;
        }

        public static string Resolve(string fileName)
        {
            return Path.Combine(GetCurrentDirectory(), fileName);
        }
    }
}
