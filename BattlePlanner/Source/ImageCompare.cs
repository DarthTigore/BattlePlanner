using System;
using System.Collections.Generic;
using System.IO;

using System.Drawing;
using System.Drawing.Imaging;

using System.Threading;

namespace BattlePlanner
{
    public class ImageCompare
    {
        public static ImageCompare Singleton = null;

        private Units Units = null;
        private List<Unit> UnitList = new List<Unit>();
        private int Phase = 1;
        private double MaxDonations = 0;
        private string Filter = string.Empty;

        // handle threading and progess
        private ProgressBar Bar = null;
        private Thread Thread = null;

        public static bool EndThread = false;

        public ImageCompare(Units units)
        {
            Singleton = this;
            Units = units;
        }

        /// <summary>
        /// Load images for all platoons
        /// </summary>
        public bool LoadPlatoons()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "platoons");
            
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path);
                var airPossible = (files.Length == (Settings.PlattonsPerZone * Settings.MaxZones));
                var count = 0;

                foreach (var file in files)
                {
                    var zone = Platoon.Platoons.Count / Settings.PlattonsPerZone;
                    count %= Settings.PlattonsPerZone;

                    var isGround = (!airPossible || zone > 0);
                    var platoon = new Platoon(file, zone + 1, ++count, isGround);

                    if (platoon.Bmp != null)
                    {
                        // only add the platoon if it succeeded
                        Platoon.Platoons.Add(platoon);
                    }
                }
            }

            return Platoon.Platoons.Count > 0;
        }

        /// <summary>
        /// Get the pixel format used in the platoon images
        /// </summary>
        /// <returns></returns>
        public PixelFormat GetPlatoonFormat()
        {
            if (Platoon.Platoons.Count > 0 && Platoon.Platoons[0].Bmp != null)
            {
                return Platoon.Platoons[0].Bmp.PixelFormat;
            }

            return PixelFormat.Format24bppRgb;
        }

        /// <summary>
        /// Make sure the platoons provided match the current phase
        /// </summary>
        /// <param name="phase"></param>
        /// <returns></returns>
        public bool Validate(int phase)
        {
            var maxPlatoons = Settings.MaxZones * Settings.PlattonsPerZone;
            return (Platoon.Platoons.Count >= Math.Min(maxPlatoons, phase * Settings.PlattonsPerZone));
        }

        public Thread Process(int phase, ProgressBar bar, string filter)
        {
            Phase = phase;
            Bar = bar;
            Filter = filter;
            EndThread = false;

            Start();

            return Thread;
        }

        public void Start()
        {
            // reset the thread
            if (Thread != null)
            {
                Thread.Abort();
                Thread = null;
            }

            // kick off the thread
            if (Thread == null)
            {
                ThreadStart threadStart = new ThreadStart(ThreadWorker);
                Thread = new Thread(threadStart);
                Thread.Name = "CompareImages";
                Thread.IsBackground = true;
            }
            Thread.Start();
        }

        public void GetUnits(string filter)
        {
            UnitList.Clear();
            UnitList = Units.GetUnits(filter);
        }

        private void ThreadWorker()
        {
            GetUnits(Units.ShipsFilter);
            Donations.Clear();

            // make sure the output directory exists
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "output");
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // clear the directory
            if (!Utils.ClearDirectory(outputPath))
            {
                Utils.LastError = "Processing screenshots failed to delete old files.";
                Bar.Done();
                return;
            }

            // find units to assign to platoons
            var groundOnly = (Platoon.Platoons.Count < (Settings.MaxZones * Settings.PlattonsPerZone));
            var lastLoc = false;
            var count = 0;
            MaxDonations = Platoon.Platoons.Count * Settings.DonationsPerPlatoon;
            MatchLog.Reset();

            foreach (var platoon in Platoon.Platoons)
            {
                platoon.UnitNames.Clear();

                if (lastLoc != platoon.IsGround)
                {
                    // switching to ground units
                    GetUnits(Filter);
                }
                var zone = 1 + (count++ / Settings.PlattonsPerZone);
                LoadByCell(platoon, outputPath, zone);

                // platoon completed, now upload to spreadsheet
                if (Settings.AutoPost)
                {
                    if (groundOnly)
                    {
                        // when sending to the spreadsheet, first ground zone is always zone 2
                        zone++;
                    }
                    Spreadsheet.Singleton.Write(platoon.UnitNames, zone, platoon.Num);
                }

                lastLoc = platoon.IsGround;
            }

            MatchLog.Save();
            Bar.Done();
        }

        public void LoadByCell(Platoon platoon, string outputPath, int zone)
        {
            // load in the platoon
            for (int row = 0; row < Settings.MaxRows; ++row)
            {
                for (int col = 0; col < Settings.MaxCols; ++col)
                {
                    var prevZones = Convert.ToDouble((zone - 1) * Settings.DonationsPerZone);
                    var prevPlats = Convert.ToDouble((platoon.Num - 1) * Settings.DonationsPerPlatoon);
                    var pct = 100.0 
                        * (prevZones + prevPlats
                        + Convert.ToDouble(row * Settings.MaxCols + col)) / MaxDonations;
                    Bar.UpdateBar(zone, platoon.Num, row * Settings.MaxCols + col + 1, pct);
                    ProcessCell(platoon, row, col, outputPath);
                }
            }
        }

        public bool ProcessCell(Platoon platoon, int row, int col, string outputPath)
        {
            var rect = new Rectangle(Settings.XStart + col * Settings.XOffset, 
                Settings.YStart + row * Settings.YOffset, Settings.CellDim, Settings.CellDim);
            var donationSrc = platoon.Bmp.Clone(rect, platoon.Bmp.PixelFormat);
            var donateBmp = (Bitmap)donationSrc.GetThumbnailImage(Settings.CompareSize, Settings.CompareSize, null, IntPtr.Zero);
            var donation = new Donation(platoon.Zone, platoon.Num, row + 1, col + 1);
            bool matchFound = false;

            // save the donation ask image
            var nameBase = string.Format("Zone{0}_{1}-{2}_{3}", platoon.Zone, platoon.Num, row + 1, col + 1);
            donateBmp.Save(Path.Combine(outputPath, nameBase + ".png"));

            // track the closest match
            var closest = 0;
            Unit match = null;

            foreach (var unit in UnitList)
            {
                if (EndThread)
                {
                    EndThread = false;
                    Thread.Abort();
                    return false;
                }

                var matchedPixels = 0;    // track how close the match was
                var loc = SearchBitmap(unit.Small, donateBmp, unit.ColorPct, unit.PixelPct, ref matchedPixels);
                if (matchedPixels > closest)
                {
                    closest = matchedPixels;
                    match = unit;
                }

                if (loc.Width != 0)
                {
                    match = unit;

                    // found a match, now make sure it isn't part of a group
                    if (unit.Group.Length > 0)
                    {
                        var group = Units.GetGroup(unit.Group);
                        closest = matchedPixels;
                        foreach (var groupUnit in group)
                        {
                            SearchBitmap(groupUnit.Small, donateBmp, groupUnit.ColorPct, 1.0, ref matchedPixels);
                            if (matchedPixels > closest)
                            {
                                closest = matchedPixels;
                                match = groupUnit;
                            }
                        }

                        var pct = Math.Min(100.0, 100.0 * Convert.ToDouble(closest) / Convert.ToDouble(match.Small.Width * match.Small.Height));
                        MatchLog.Add(string.Format("Z{0} P{1} {2},{3} (Group): {4} @ {5:0.00}%", 
                            platoon.Zone, platoon.Num, row + 1, col + 1, match.Name, pct));
                    }

                    // save the image
                    var nameExt = string.Format("{0}-{1}", nameBase, Path.GetFileNameWithoutExtension(match.BmpPath));
                    match.Thumb.Save(Path.Combine(outputPath, nameExt + ".png"));
                    donation.Name = match.Name;
#if DEBUG
                    Console.WriteLine("Unit found: Z{0} P{1} {2},{3} = {4}", platoon.Zone, platoon.Num, row + 1, col + 1, match.Name);
#endif
                    platoon.UnitNames.Add(match.Name);

                    matchFound = true;
                    break;
                }
            }
            
            // if no match found, log the best match
            if (!matchFound && match != null)
            {
                var pct = Math.Min(100.0, 100.0 * Convert.ToDouble(closest) / Convert.ToDouble(match.Small.Width * match.Small.Height));
                MatchLog.Add(string.Format("Z{0} P{1} {2},{3} (Unmatched): {4} @ {5:0.00}%",
                    platoon.Zone, platoon.Num, row + 1, col + 1, match.Name, pct));
            }

            // cache the donation
            Donations.Add(donation);

            return matchFound;
        }

        /// <summary>
        /// Search for an image within another image
        /// </summary>
        /// <param name="smallBmp"></param>
        /// <param name="bigBmp"></param>
        /// <param name="colorTolerance"></param>
        /// <param name="pixelsTolerance"></param>
        /// <returns></returns>
        private Rectangle SearchBitmap(Bitmap smallBmp, Bitmap bigBmp, double colorTolerance, double pixelsTolerance, ref int closest)
        {
            // initialize the data
            BitmapData smallData = smallBmp.LockBits(new Rectangle(0, 0, smallBmp.Width, smallBmp.Height), 
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bigData = bigBmp.LockBits(new Rectangle(0, 0, bigBmp.Width, bigBmp.Height), 
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int smallStride = smallData.Stride;
            int bigStride = bigData.Stride;

            int bigWidth = bigBmp.Width - smallBmp.Width + 1;
            int bigHeight = bigBmp.Height - smallBmp.Height + 1;
            int smallWidth = smallBmp.Width * 3;
            int smallHeight = smallBmp.Height;

            Rectangle location = Rectangle.Empty;
            int margin = Convert.ToInt32(255.0 * (1.0 - colorTolerance));
            int minMatchPixels = Convert.ToInt32(smallBmp.Width * smallBmp.Height * pixelsTolerance);

            unsafe
            {
                byte* pSmall = (byte*)(void*)smallData.Scan0;
                byte* pBig = (byte*)(void*)bigData.Scan0;

                int smallOffset = smallStride - smallBmp.Width * 3;
                int bigOffset = bigStride - bigBmp.Width * 3;

                bool matchFound = false;

                for (int y = 0; y < bigHeight; y++)
                {
                    for (int x = 0; x < bigWidth; x++)
                    {
                        byte* pBigBackup = pBig;
                        byte* pSmallBackup = pSmall;

                        int matchCount = 0;
                        matchFound = false;

                        // search for the small image
                        for (int i = 0; i < smallHeight; i++)
                        {
                            int j = 0;

                            for (j = 0; j < smallWidth; j++)
                            {
                                // pSmall should be between the margins
                                int low = pBig[0] - margin;
                                int high = pBig[0] + margin;
                                if (low < pSmall[0] && pSmall[0] < high)
                                {
                                    matchCount++;
                                    closest = Math.Max(closest, matchCount);
                                }

                                pBig++;
                                pSmall++;
                            }

                            // make sure we match enough pixels
                            if (matchCount >= minMatchPixels)
                            {
                                matchFound = true;
                                break;
                            }

                            // restore the pointers
                            pSmall = pSmallBackup;
                            pBig = pBigBackup;

                            // next rows of the images
                            pSmall += smallStride * (1 + i);
                            pBig += bigStride * (1 + i);
                        }

                        if (matchFound)
                        {
                            location.X = x;
                            location.Y = y;
                            location.Width = smallBmp.Width;
                            location.Height = smallBmp.Height;
                            break;
                        }
                        else
                        {
                            // no match found, restore pointers and continue
                            pBig = pBigBackup;
                            pSmall = pSmallBackup;
                            pBig += 3;
                        }
                    }

                    if (matchFound)
                    {
                        break;
                    }

                    pBig += bigOffset;
                }
            }

            bigBmp.UnlockBits(bigData);
            smallBmp.UnlockBits(smallData);
            
            return location;
        }


    }
}
