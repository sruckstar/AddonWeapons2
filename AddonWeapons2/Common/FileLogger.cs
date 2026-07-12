using System;
using System.IO;

namespace AddonWeapons2.Common
{
    public static class FileLogger
    {
        private const string LogFile = "Scripts\\AddonWeapons\\AddonWeapons.log";

        public static void Write(string tag, string message)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogFile);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(
                    LogFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tag}] {message}{Environment.NewLine}"
                );
            }
            catch
            {
            }
        }
    }
}
