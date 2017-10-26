using System;
using System.Collections.Generic;

using System.IO;
using System.Xml.Linq;

namespace BattlePlanner
{
    /// <summary>
    /// Storage class for Preset data
    /// </summary>
    public class Preset
    {
        // platoon coordinate system
        public int Width = 1334;
        public int Height = 750;
        public int StartX = 0;
        public int StartY = 0;
        public int CellDim = 0;
        public int OffsetX = 0;
        public int OffsetY = 0;

        // unit sub-image cropping
        public int CompareSize = 5;
        public int Crop = 0;

        public double AspectRatio = 0.0;
    }

    public class Presets
    {
        private static string FileName = "Presets.xml";

        public static List<Preset> PresetList = new List<Preset>();

        /// <summary>
        /// Find the best fit for a resolution and update settings
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public static void BestFit(int w, int h)
        {
            Settings.PlatoonW = w;
            Settings.PlatoonH = h;

            // look for a perfect match
            foreach (var preset in PresetList)
            {
                if (preset.Width == w && preset.Height == h)
                {
                    ChangeSettings(preset);
                    return;
                }
            }

            // look for a near match based on aspect ratio
            var aspectRatio = Convert.ToDouble(w) / Convert.ToDouble(h);
            foreach (var preset in PresetList)
            {
                if (preset.AspectRatio == aspectRatio)
                {
                    ChangeSettings(preset, w, h);
                    return;
                }
            }

            // look for the closest match based on width
            Preset last = PresetList[0];
            foreach (var preset in PresetList)
            {
                if (preset.Width <= w)
                {
                    last = preset;
                }
                else
                {
                    break;
                }
            }
            ChangeSettings(last, w, h);
        }

        /// <summary>
        /// Update the settings with a given preset
        /// </summary>
        /// <param name="preset"></param>
        private static void ChangeSettings(Preset preset)
        {
            Settings.XStart = preset.StartX;
            Settings.YStart = preset.StartY;
            Settings.CellDim = preset.CellDim;
            Settings.XOffset = preset.OffsetX;
            Settings.YOffset = preset.OffsetY;
            Settings.CompareSize = preset.CompareSize;
            Settings.Crop = preset.Crop;
        }

        /// <summary>
        /// Update the settings with a given preset, scaled to a resolution
        /// </summary>
        /// <param name="preset"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        private static void ChangeSettings(Preset preset, int w, int h)
        {
            // scale the values before sending
            var xScale = Convert.ToDouble(w) / Convert.ToDouble(preset.Width);
            var yScale = Convert.ToDouble(h) / Convert.ToDouble(preset.Height);

            Settings.XStart = Convert.ToInt32(preset.StartX * xScale);
            Settings.YStart = Convert.ToInt32(preset.StartY * yScale);
            Settings.CellDim = Convert.ToInt32(preset.CellDim * xScale);
            Settings.XOffset = Convert.ToInt32(preset.OffsetX * xScale);
            Settings.YOffset = Convert.ToInt32(preset.OffsetY * yScale);

            Settings.CompareSize = Math.Min(preset.CompareSize, Settings.CellDim);
            if (Settings.CompareSize < preset.CompareSize)
            {
                var cropScale = Convert.ToDouble(Settings.CellDim) / Convert.ToDouble(preset.Crop);
                Settings.Crop = Convert.ToInt32(preset.Crop * cropScale);
            }
        }

        /// <summary>
        /// Load the presets
        /// </summary>
        public static void Load()
        {
            if (File.Exists(FileName))
            {
                try
                {
                    XDocument doc = XDocument.Load(FileName);
                    XElement root = doc.Element("Presets");
                    foreach (var element in root.Elements())
                    {
                        var preset = new Preset();
                        preset.Width = Convert.ToInt32(element.Attribute("Width").Value);
                        preset.Height = Convert.ToInt32(element.Attribute("Height").Value);
                        preset.StartX = Convert.ToInt32(element.Attribute("StartX").Value);
                        preset.StartY = Convert.ToInt32(element.Attribute("StartY").Value);
                        preset.CellDim = Convert.ToInt32(element.Attribute("CellDim").Value);
                        preset.OffsetX = Convert.ToInt32(element.Attribute("OffsetX").Value);
                        preset.OffsetY = Convert.ToInt32(element.Attribute("OffsetY").Value);
                        preset.CompareSize = Convert.ToInt32(element.Attribute("CompareDim").Value);
                        preset.Crop = Convert.ToInt32(element.Attribute("Crop").Value);
                        preset.AspectRatio = Convert.ToDouble(preset.Width) / Convert.ToDouble(preset.Height);

                        PresetList.Add(preset);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: {0}", e.ToString());
                }
            }
        }
    }
}
