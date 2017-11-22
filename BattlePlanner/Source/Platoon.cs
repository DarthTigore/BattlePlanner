using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace BattlePlanner
{
    public class Platoon
    {
        public static Platoon Singleton = null;
        public static List<Platoon> Platoons = new List<Platoon>();

        public Bitmap Bmp = null;
        public string BmpPath = string.Empty;
        public int Zone = 0;
        public int Num = 0;
        public bool IsGround = true;

        public List<string> UnitNames = new List<string>();

        public Platoon(string path, int zone, int num, bool isGround)
        {
            Singleton = this;
            try
            {
                Bmp = new Bitmap(path);
            }
            catch (Exception e)
            {
                ErrorLog.AddLine("Failed to load " + path);
                ErrorLog.AddLine(e.ToString());
                Console.WriteLine("Failed to load " + path);
            }

            BmpPath = path;
            Zone = zone;
            Num = num;
            IsGround = isGround;
        }

        public static Platoon Get(int zone, int num)
        {
            foreach (var platoon in Platoons)
            {
                if (platoon.Zone == zone && platoon.Num == num)
                {
                    return platoon;
                }
            }

            return null;
        }
    }
}
