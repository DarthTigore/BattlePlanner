
namespace BattlePlanner
{
    public class Donation
    {
        public string Name = string.Empty;
        public int Zone = 0;
        public int Platoon = 0;
        public int Row = 0;
        public int Col = 0;
        public bool Upload = false;

        public Donation(string name, int zone, int platoon, int row, int col)
        {
            Name = name;
            Zone = zone;
            Platoon = platoon;
            Row = row;
            Col = col;
            Upload = (name.Length > 0);
        }

        public Donation(int zone, int platoon, int row, int col)
        {
            Zone = zone;
            Platoon = platoon;
            Row = row;
            Col = col;
        }

    }
}
