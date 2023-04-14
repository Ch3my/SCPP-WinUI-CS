using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS
{
    public static class FileLogger
    {
        private static object _lock = new object();

        public static void AppendToFile(string text)
        {
            lock (_lock)
            {
                using (StreamWriter writer = new StreamWriter("log.txt", true))
                {
                    writer.WriteLine(text);
                }
            }
        }
    }
}
