using System;


namespace BattlePlanner
{
    public class MatchLog
    {
        private static string FileName = "MatchLog.txt";
        private static string Log = string.Empty;

        /// <summary>
        /// Reset the log's cache
        /// </summary>
        public static void Reset()
        {
            Log = string.Empty;
        }

        /// <summary>
        /// Add a new entry to the log
        /// </summary>
        /// <param name="entry"></param>
        public static void Add(string entry)
        {
            Log += entry + Environment.NewLine;
        }

        /// <summary>
        /// Save the log file
        /// </summary>
        public static void Save()
        {
            Utils.WriteFile(FileName, Log);
            Reset();
        }
    }
}
