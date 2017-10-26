using System.Collections.Generic;
using System.IO;

namespace BattlePlanner
{
    public class Donations
    {
        private static List<Donation> DonationList = new List<Donation>();

        /// <summary>
        /// Get a specific Donation
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="platoon"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static Donation Get(int zone, int platoon, int row, int col)
        {
            foreach (var donation in DonationList)
            {
                if (donation.Zone == zone && donation.Platoon == platoon 
                    && donation.Row == row && donation.Col == col)
                {
                    return donation;
                }
            }

            return null;
        }

        /// <summary>
        /// Add a Donation
        /// </summary>
        /// <param name="donation"></param>
        public static void Add(Donation donation)
        {
            DonationList.Add(donation);
        }

        /// <summary>
        /// Clear all Donations
        /// </summary>
        public static void Clear()
        {
            DonationList.Clear();
        }

        /// <summary>
        /// Load all Donations
        /// </summary>
        public static void Load()
        {
            //  clear the resources
            Clear();
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "output");

            if (Directory.Exists(basePath))
            {
                for (var zone = 1; zone <= Settings.MaxZones; ++zone)
                {
                    for (var platoon = 1; platoon <= Settings.PlattonsPerZone; ++platoon)
                    {
                        for (var row = 1; row <= Settings.MaxRows; ++row)
                        {
                            for (var col = 1; col <= Settings.MaxCols; ++col)
                            {
                                var baseName = string.Format("Zone{0}_{1}-{2}_{3}", zone, platoon, row, col);
                                var pattern = string.Format("{0}*.png", baseName);
                                var files = Directory.GetFiles(basePath, pattern);
                                var name = string.Empty;

                                if (files.Length == 2)
                                {
                                    var fileName = Path.GetFileName(files[0]).Substring(baseName.Length + 1);
                                    var unit = Units.Singleton.GetByPath(fileName);
    
                                    if (unit != null)
                                    {
                                        name = unit.Name;
                                    }
                                }

                                var donation = new Donation(name, zone, platoon, row, col);
                                Add(donation);
                            }
                        }
                    }
                }
            }
        }
    }
}
