using System;

namespace BattlePlanner
{
    public class Settings
    {
        // constants
        public const int MaxRows = 3;
        public const int MaxCols = 5;
        public const int MaxPhases = 6;
        public const int PlattonsPerZone = 6;
        public const int MaxZones = 3;
        public const int DonationsPerPlatoon = MaxRows * MaxCols;
        public const int DonationsPerZone = DonationsPerPlatoon * PlattonsPerZone;

        // spreadsheet
        public static string SpreadsheetID = string.Empty;

        // unit sub-image cropping
        public static int CompareSize = 75;
        public static int Crop = 20;

        // platoon coordinate system
        public static int PlatoonW = 1334;
        public static int PlatoonH = 750;
        public static int XStart = 400;
        public static int YStart = 235;
        public static int CellDim = 75;
        public static int XOffset = 40 + CellDim;
        public static int YOffset = 62 + CellDim;

        // current settings
        public static int Phase = 1;
        public static int WinX = 0;
        public static int WinY = 0;
        public static string Filter = string.Empty;
        public static bool AutoPost = false;

        /// <summary>
        /// Save the settings
        /// </summary>
        public static void Save()
        {
            try
            {
                Properties.Settings.Default.WinX = WinX;
                Properties.Settings.Default.WinY = WinY;

                Properties.Settings.Default.SpreadsheetID = SpreadsheetID;

                Properties.Settings.Default.PlatoonW = PlatoonW;
                Properties.Settings.Default.PlatoonH = PlatoonH;
                Properties.Settings.Default.XStart = XStart;
                Properties.Settings.Default.YStart = YStart;
                Properties.Settings.Default.CellDim = CellDim;
                Properties.Settings.Default.XOffset = XOffset;
                Properties.Settings.Default.YOffset = YOffset;
                Properties.Settings.Default.SubDim = Crop;
                Properties.Settings.Default.CompareSize = CompareSize;

                Properties.Settings.Default.Phase = Phase;
                Properties.Settings.Default.Filter = Filter;
                Properties.Settings.Default.AutoPost = AutoPost;

                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                ErrorLog.AddLine("Settings.Save - " + e.ToString());
                Console.WriteLine("Exception: {0}", e.ToString());
            }
        }

        /// <summary>
        /// Load the settings
        /// </summary>
        public static void Load()
        {
            try
            {
                WinX = Properties.Settings.Default.WinX;
                WinY = Properties.Settings.Default.WinY;

                SpreadsheetID = Properties.Settings.Default.SpreadsheetID;

                PlatoonW = Properties.Settings.Default.PlatoonW;
                PlatoonH = Properties.Settings.Default.PlatoonH;
                XStart = Properties.Settings.Default.XStart;
                YStart = Properties.Settings.Default.YStart;
                CellDim = Properties.Settings.Default.CellDim;
                XOffset = Properties.Settings.Default.XOffset;
                YOffset = Properties.Settings.Default.YOffset;
                Crop = Properties.Settings.Default.SubDim;
                CompareSize = Properties.Settings.Default.CompareSize;

                Phase = Properties.Settings.Default.Phase;
                Filter = Properties.Settings.Default.Filter;
                AutoPost = Properties.Settings.Default.AutoPost;
            }
            catch (Exception e)
            {
                ErrorLog.AddLine("Settings.Load - " + e.ToString());
                Console.WriteLine("Exception: {0}", e.ToString());
            }
        }
    }
}
