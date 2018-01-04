using System;
using System.Collections.Generic;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Linq;

namespace BattlePlanner
{
    /// <summary>
    /// Storage class for unit data pulled from swgoh.gg
    /// </summary>
    public class SourceUnit
    {
        public string Name = string.Empty;
        public string Url = string.Empty;

        public SourceUnit(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }

    public class Units
    {
        public static string ShipsFilter = "ships";
        public static System.Globalization.CultureInfo CultureUS;

        private static string RootUrl = "https://swgoh.gg/";
        private static string UnitsFileName = "Units.xml";
        private static string DefaultFileName = "Default.xml";
        public static Units Singleton = null;

        private List<Unit> UnitList = new List<Unit>();
        private List<Unit> DefaultList = new List<Unit>();

        public Units()
        {
            Singleton = this;
            CultureUS = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        }

        /// <summary>
        /// Get all Units of the supplied filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public List<Unit> GetUnits(string filter)
        {
            List<Unit> list = new List<Unit>();
            foreach (var unit in UnitList)
            {
                if (unit.BmpPath.Contains(filter))
                {
                    list.Add(unit);
                }
            }

            return list;
        }

        /// <summary>
        /// Get a Unit Group by label
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public List<Unit> GetGroup(string group)
        {
            List<Unit> list = new List<Unit>();
            foreach (var unit in UnitList)
            {
                if (unit.Group == group)
                {
                    list.Add(unit);
                }
            }

            return list;
        }

        /// <summary>
        /// Get a Unit by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Unit GetUnit(string name)
        {
            foreach (var unit in UnitList)
            {
                if (unit.Name == name)
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>
        /// Get a default Unit by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Unit GetDefaultUnit(string name)
        {
            foreach (var unit in DefaultList)
            {
                if (unit.Name == name)
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>
        /// Get a unit's Name by supplying the image path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetNameByPath(string path)
        {
            var lookup = Path.GetFileName(path).ToLower();
            foreach (var unit in UnitList)
            {
                var bmpPath = Path.GetFileName(unit.BmpPath).ToLower();
                if (bmpPath == lookup)
                {
                    return unit.Name;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Get a Unit by supplying the image path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Unit GetByPath(string path)
        {
            var lookup = Path.GetFileName(path).ToLower();
            foreach (var unit in UnitList)
            {
                var bmpPath = Path.GetFileName(unit.BmpPath).ToLower();
                if (bmpPath == lookup)
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>
        /// Release resources before refreshing data
        /// </summary>
        public void StartRefreshData()
        {
            foreach (var unit in UnitList)
            {
                unit.Release();
            }
        }

        /// <summary>
        /// Refresh all stored unit images
        /// </summary>
        public void RefreshData(bool fullUpdate = true)
        {
            // get the list of light side heroes
            RefreshData("characters/f/Light%20Side/", "light-side", fullUpdate);

            // get the list of dark side heroes
            RefreshData("characters/f/Dark%20Side/", "dark-side", fullUpdate);

            // get the list of heroes
            RefreshData("ships/", Units.ShipsFilter, fullUpdate);

            Save();
        }

        /// <summary>
        /// Refresh all stored unit images of a given type
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="dir"></param>
        private void RefreshData(string filter, string dir, bool fullUpdate)
        {
            // get the list of units
            var url = RootUrl + filter;
            var units = GetSourceUnits(url);
            DownloadImages(units, dir, fullUpdate);
        }

        /// <summary>
        /// Save the custom unit settings
        /// </summary>
        public void Save()
        {
            try
            {
                XElement root = new XElement("Units");
                foreach (var unit in UnitList)
                {
                    XElement unitElement = new XElement("Unit",
                        new XAttribute("Name", unit.Name),
                        new XAttribute("BmpPath", unit.BmpPath));
                    var defUnit = GetDefaultUnit(unit.Name);

                    // only save these values if they are unique
                    if (unit.ColorPct != Unit.ColorPctDefault && (defUnit == null || unit.ColorPct != defUnit.ColorPct))
                    {
                        unitElement.Add(new XAttribute("ColorPct", string.Format(CultureUS, "{0:0.000}", unit.ColorPct)));
                    }
                    if (unit.PixelPct != Unit.PixelPctDefault && (defUnit == null || unit.PixelPct != defUnit.PixelPct))
                    {
                        unitElement.Add(new XAttribute("PixelPct", string.Format(CultureUS, "{0:0.000}", unit.PixelPct)));
                    }
                    if (unit.Priority != Unit.MaxPriority && (defUnit == null || unit.Priority != defUnit.Priority))
                    {
                        unitElement.Add(new XAttribute("Pri", unit.Priority));
                    }
                    if (unit.Group.Length > 0 && (defUnit == null || unit.Group != defUnit.Group))
                    {
                        unitElement.Add(new XAttribute("Group", unit.Group));
                    }

                    root.Add(unitElement);
                }

                XDocument doc = new XDocument();
                doc.Add(root);
                doc.Save(UnitsFileName);
            }
            catch (Exception e)
            {
                ErrorLog.AddLine("Units.Save - " + e.ToString());
                Console.WriteLine("Exception: {0}", e.ToString());
            }
        }

        /// <summary>
        /// Load custom unit settings to overwrite defaults
        /// </summary>
        /// <returns></returns>
        public bool Load()
        {
            // clear the contents
            UnitList.Clear();

            LoadDefaults();

            if (File.Exists(UnitsFileName))
            {
                try
                {
                    XDocument doc = XDocument.Load(UnitsFileName);
                    XElement root = doc.Element("Units");
                    foreach (var element in root.Elements())
                    {
                        var name = element.Attribute("Name").Value;
                        var bmpPath = element.Attribute("BmpPath").Value;
                        var unit = new Unit(bmpPath, name);
                        var defUnit = GetDefaultUnit(name);

                        // see if pixel pct was overwritten
                        var attribute = element.Attribute("PixelPct");
                        if (attribute != null)
                        {
                            unit.PixelPct = Convert.ToDouble(attribute.Value.Replace(",", ".").Replace(" ", "."), CultureUS);
                        }
                        else if (defUnit != null)
                        {
                            unit.PixelPct = defUnit.PixelPct;
                        }

                        // see if pixel pct was overwritten
                        attribute = element.Attribute("ColorPct");
                        if (attribute != null)
                        {
                            unit.ColorPct = Convert.ToDouble(attribute.Value.Replace(",", ".").Replace(" ", "."), CultureUS);
                        }
                        else if (defUnit != null)
                        {
                            unit.ColorPct = defUnit.ColorPct;
                        }

                        // see if priority was overwritten
                        attribute = element.Attribute("Pri");
                        if (attribute != null)
                        {
                            unit.Priority = Convert.ToInt32(attribute.Value);
                        }
                        else if (defUnit != null)
                        {
                            unit.Priority = defUnit.Priority;
                        }

                        // see if group was overwritten
                        attribute = element.Attribute("Group");
                        if (attribute != null)
                        {
                            unit.Group = attribute.Value;
                        }
                        else if (defUnit != null)
                        {
                            unit.Group = defUnit.Group;
                        }

                        Add(unit);
                    }

                    return true;
                }
                catch (Exception e)
                {
                    ErrorLog.AddLine("Units.Load - " + e.ToString());
                    Console.WriteLine("Exception: {0}", e.ToString());
                }
            }

            return false;
        }

        /// <summary>
        /// Add a new unit into the list based on Priority and Name
        /// </summary>
        /// <param name="unit"></param>
        private void Add(Unit unit)
        {
            for (var i = 0; i < UnitList.Count; ++i)
            {
                if (unit.Priority < UnitList[i].Priority)
                {
                    // priority is higher than the current unit, so insert here
                    UnitList.Insert(i, unit);
                    return;
                }
                else if (unit.Priority == UnitList[i].Priority && string.Compare(unit.Name, UnitList[i].Name) < 0)
                {
                    // same priority, but this unit comes first alphabetically
                    UnitList.Insert(i, unit);
                    return;
                }
            }

            // add the unit if it wasn't already done
            UnitList.Add(unit);
        }

        /// <summary>
        /// Load the default unit settings
        /// </summary>
        /// <returns></returns>
        public bool LoadDefaults()
        {
            // clear the contents
            DefaultList.Clear();

            if (File.Exists(DefaultFileName))
            {
                try
                {
                    XDocument doc = XDocument.Load(DefaultFileName);
                    XElement root = doc.Element("Units");
                    foreach (var element in root.Elements())
                    {
                        var name = element.Attribute("Name").Value;
                        var unit = new Unit(name);

                        // see if pixel pct was overwritten
                        var attribute = element.Attribute("PixelPct");
                        if (attribute != null)
                        {
                            unit.PixelPct = Convert.ToDouble(attribute.Value, CultureUS);
                        }

                        // see if pixel pct was overwritten
                        attribute = element.Attribute("ColorPct");
                        if (attribute != null)
                        {
                            unit.ColorPct = Convert.ToDouble(attribute.Value, CultureUS);
                        }

                        // see if group was overwritten
                        attribute = element.Attribute("Group");
                        if (attribute != null)
                        {
                            unit.Group = attribute.Value;
                        }

                        // see if priority was overwritten
                        attribute = element.Attribute("Pri");
                        if (attribute != null)
                        {
                            unit.Priority = Convert.ToInt32(attribute.Value);
                        }

                        DefaultList.Add(unit);
                    }

                    return true;
                }
                catch (Exception e)
                {
                    ErrorLog.AddLine("Units.LoadDefaults - " + e.ToString());
                    Console.WriteLine("Exception: {0}", e.ToString());
                }
            }

            return false;
        }

        /// <summary>
        /// Optimize all units for faster comparison.
        /// </summary>
        /// <param name="format"></param>
        public void Optimize(PixelFormat format)
        {
            foreach (var unit in UnitList)
            {
                Optimize(unit, format);
            }
        }

        /// <summary>
        /// Optimize a unit's image for faster comparisons.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="format"></param>
        private void Optimize(Unit unit, PixelFormat format)
        {
            // clean data from previous optimize
            unit.ReleaseOpt();

            if (unit.Bmp != null)
            {
                // resize the image to match cells
                unit.Thumb = (Bitmap)unit.Bmp.GetThumbnailImage(Settings.CompareSize, Settings.CompareSize, null, IntPtr.Zero);

                // get the middle part of the image for matching
                var unitRect = new Rectangle(Settings.Crop, Settings.Crop,
                    unit.Thumb.Width - Settings.Crop * 2,
                    unit.Thumb.Height - Settings.Crop * 2);

                if (unitRect.Width > 0 && unitRect.Height > 0)
                {
                    unit.Small = unit.Thumb.Clone(unitRect, format);
                }
            }
        }

        /// <summary>
        /// Get all units and image links from a url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private List<SourceUnit> GetSourceUnits(string url)
        {
            List<SourceUnit> units = new List<SourceUnit>();

            // get the source
            var data = Utils.GetWebSource(url);

            // parse the data
            var lines = data.Split('\n');
            var portraitToken = (url.Contains(Units.ShipsFilter)) ? "class=\"ship-portrait" : "class=\"char-portrait";
            bool foundHeroes = false;
            foreach (var line in lines)
            {
                // <li class="media list-group-item p-0 character" data-name-lower="aayla secura" data-tags="light side support galactic republic jedi ">
                // <a class="media-body character light-side" href="/characters/aayla-secura/">
                // <div class="char-portrait
                //   char-portrait-light-side
                //   ">
                // <div class="char-portrait-image"><img class="char-portrait-img" src="//swgoh.gg/static/img/assets/tex.charui_aaylasecura.png" alt="Aayla Secura"/></div>
                if (line.IndexOf("data-name-lower=") != -1)
                {
                    foundHeroes = true;
                }

                if (foundHeroes)
                {
                    if (line.IndexOf("alt=") != -1 && line.Contains(portraitToken))
                    {
                        // found new unit
                        var unitName = Utils.GetSubstring(line, "alt=\"", "\" />");
                        unitName = Utils.FixString(unitName);
                        var unitUrl = Utils.GetSubstring(line, " src=\"", "\" alt=");
                        unitUrl = "https:" + unitUrl;

                        var unit = new SourceUnit(unitName, unitUrl);
                        units.Add(unit);
                    }
                }
            }

            return units;
        }

        /// <summary>
        /// Download all images for the units and store them on disc
        /// </summary>
        /// <param name="units"></param>
        /// <param name="dir"></param>
        private void DownloadImages(List<SourceUnit> units, string dir, bool fullUpdate)
        {
            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), dir);
            if (fullUpdate)
            {
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }

                // clear the directory
                if (!Utils.ClearDirectory(rootPath))
                {
                    Utils.LastError = "Data refresh failed to delete old files.";
                }
            }

            // download the images
            foreach (var srcUnit in units)
            {
                if (!fullUpdate)
                {
                    bool unitExists = false;

                    // only download image if it is new
                    foreach (var cachedUnit in UnitList)
                    {
                        if (cachedUnit.Name == srcUnit.Name)
                        {
                            unitExists = true;
                            break;
                        }
                    }

                    if (unitExists)
                    {
                        continue;
                    }
                }

                // strip the root file name (tex.charui_aaylasecura.png)
                var fileName = Path.GetFileName(srcUnit.Url).Replace("tex.charui_", "");
                var filePath = Path.Combine(rootPath, fileName);
                DownloadImage(srcUnit.Url, filePath);

                Unit unit = GetUnit(srcUnit.Name);
                bool reuseUnit = true;
                if (unit == null)
                {
                    // couldn't find unit by name, maybe the name changed
                    unit = GetByPath(fileName);
                }
                if (unit != null)
                {
                    unit.Name = srcUnit.Name;   // just in case the name changed in-game
                    unit.BmpPath = Path.Combine(dir, fileName);
                }
                else
                {
                    // new unit found
                    unit = new Unit(srcUnit, Path.Combine(dir, fileName));
                    reuseUnit = false;
                }

                if (!reuseUnit)
                {
                    Add(unit);
                }
            }
        }

        /// <summary>
        /// Download an image from the web and store it on disc
        /// </summary>
        /// <param name="link"></param>
        /// <param name="path"></param>
        private void DownloadImage(string link, string path)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
#if DEBUG
                    Console.WriteLine("Downloading {0}...", Path.GetFileName(path));
#endif
                    webClient.DownloadFile(link, path);
                }
            }
            catch (Exception e)
            {
                ErrorLog.AddLine("Units.DownloadImage - " + e.ToString());
                Console.WriteLine("Exception: {0}", e.ToString());
            }
        }

    }
}
