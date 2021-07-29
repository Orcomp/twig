namespace wig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public static class FileHelper
    {
        public static long GetDirectorySize(string path, bool subfolder, string ext = ".")
        {
            IEnumerable<string> dir = null;
            var option = SearchOption.TopDirectoryOnly;
            if (subfolder)
            {
                option = SearchOption.AllDirectories;
            }

            if (ext == ".zs")
            {
                dir = Directory.GetFiles(path, "*.zs*", option);
            }
            else
            {
                dir = Directory.GetFiles(path, "*.*", option).Where(path => !path.EndsWith(".zs"));
            }

            long total = 0;

            foreach (var file in dir)
            {
                FileInfo info = new FileInfo(file);
                total += info.Length;
            }

            return total;
        }

        public static async Task<string> WriteFileAsync(byte[] data, string path, string destination = "", string extension = "")
        {
            var fileName = Path.GetFileName(path) + extension;
            var currentDirectory = Path.GetDirectoryName(path);
            var directory = currentDirectory;
            if (!String.IsNullOrEmpty(destination))
            {
                var folder = Path.Combine(currentDirectory, destination);
                if (!Directory.Exists(folder))
                {
                    directory = Directory.CreateDirectory(folder).ToString();
                }
                else
                {
                    directory = folder;
                }
            }
            
            var writePath = Path.Combine(directory, fileName);
            var fileMode = File.Exists(writePath) ? FileMode.Truncate : FileMode.OpenOrCreate;
           
            await using (FileStream fstream = new FileStream(writePath, fileMode))
            {
                await fstream.WriteAsync(data, 0, data.Length);
            }

            return writePath;
        }
    }
}
