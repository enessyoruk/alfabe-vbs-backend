using System;
using System.IO;
using System.Text;

namespace Alfabe.Helpers
{
    public static class SafeLogger
    {
        private static readonly object _lock = new();

        private static string LogFilePath =>
            Path.Combine(Directory.GetCurrentDirectory(), "logs", "vbs-jwt.log");

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    var folder = Path.GetDirectoryName(LogFilePath);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder!);

                    File.AppendAllText(LogFilePath,
                        $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | {message}\n",
                        Encoding.UTF8);
                }
            }
            catch { /* logging should never crash app */ }
        }
    }
}
