using System;
using System.IO;

namespace BattlePlanner
{
    class ErrorLog
    {
        private static string FileName = "ErrorLog.txt";

        /// <summary>
        /// Reset the log's cache
        /// </summary>
        public static void Clear()
        {
            try
            {
                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }
            }
            catch
            {
                // handle exception...
            }
        }

        /// <summary>
        /// Add a new entry to the log
        /// </summary>
        /// <param name="entry"></param>
        public static void AddLine(string entry)
        {
            try
            {
                Utils.WriteFile(FileName, entry + Environment.NewLine, true);
            }
            catch
            {
                // handle exception...
            }
        }
    }
}
