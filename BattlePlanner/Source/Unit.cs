using System.Drawing;

namespace BattlePlanner
{
    public class Unit
    {
        public static double PixelPctDefault = 0.85;
        public static double ColorPctDefault = 0.975;
        public static int MaxPriority = 100;

        public Bitmap Bmp = null;
        public Bitmap Thumb = null;
        public Bitmap Small = null;

        public string Name = string.Empty;

        public string BmpPath = string.Empty;
        public string RawPath = string.Empty;

        public double PixelPct = PixelPctDefault;
        public double ColorPct = ColorPctDefault;

        public int Priority = MaxPriority;

        public string Group = string.Empty;

        public Unit(SourceUnit unit, string bmpPath)
        {
            Name = unit.Name;
            BmpPath = bmpPath;
        }

        public Unit(string path, string name)
        {
            BmpPath = path;
            Bmp = new Bitmap(path);
            Name = name;
        }

        public Unit(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Release all resources
        /// </summary>
        public void Release()
        {
            ReleaseOpt();

            if (Bmp != null)
            {
                Bmp.Dispose();
                Bmp = null;
            }
        }

        /// <summary>
        /// Release the optimized resources
        /// </summary>
        public void ReleaseOpt()
        {
            if (Small != null)
            {
                Small.Dispose();
                Small = null;
            }
            if (Thumb != null)
            {
                Thumb.Dispose();
                Thumb = null;
            }
        }
    }
}
