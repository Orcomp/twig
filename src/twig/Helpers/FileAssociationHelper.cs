namespace twig
{
    using System;
    using System.Dynamic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;
    using Spectre.Console;

    public static class FileAssociationHelper
    {
        public static void AddContextMenuOption(string subKey, string value)
        {
            var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var key = Registry.CurrentUser.CreateSubKey(subKey, true);
            key.SetValue("", value);
            var newSubKey = key.CreateSubKey("command");
            newSubKey.SetValue("", appPath + " \"%1\"");
            newSubKey.Close();
            key.Close();

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        public static void RegisterForFileExtension(string extension)
        {
            var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var key = Registry.CurrentUser.CreateSubKey("Software\\Classes\\" + extension);
            var subKey = key.CreateSubKey("shell\\open\\command");
            subKey.SetValue("", appPath + " \"%1\"");
            subKey.Close();
            key.Close();

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        public static void RemoveContextMenuOption(string subKey)
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(subKey);

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        public static void UnregisterForFileExtension(string extension)
        {
            var key = Registry.CurrentUser.OpenSubKey("Software\\Classes\\" + extension, true);
            key.DeleteSubKey("shell\\open\\command");
            key.Close();

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
