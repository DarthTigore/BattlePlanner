using System.Collections.Generic;
using System.Windows.Controls;
using System.IO;

namespace BattlePlanner
{
    /// <summary>
    /// Interaction logic for PlatoonView.xaml
    /// </summary>
    public partial class PlatoonView : UserControl
    {
        private DonationView[,] Donations = new DonationView[3,5];
        private string BasePath = string.Empty;

        public PlatoonView()
        {
            InitializeComponent();

            BasePath = Path.Combine(Directory.GetCurrentDirectory(), "output");

            // initialize the arrays
            var row = 0;
            var col = 0;
            Donations[row, col++] = Donation11;
            Donations[row, col++] = Donation12;
            Donations[row, col++] = Donation13;
            Donations[row, col++] = Donation14;
            Donations[row++, col++] = Donation15;

            col = 0;
            Donations[row, col++] = Donation21;
            Donations[row, col++] = Donation22;
            Donations[row, col++] = Donation23;
            Donations[row, col++] = Donation24;
            Donations[row++, col++] = Donation25;

            col = 0;
            Donations[row, col++] = Donation31;
            Donations[row, col++] = Donation32;
            Donations[row, col++] = Donation33;
            Donations[row, col++] = Donation34;
            Donations[row++, col++] = Donation35;
        }

        private List<string> GetNames(List<Unit> units)
        {
            List<string> names = new List<string>();
            foreach (var unit in units)
            {
                names.Add(unit.Name);
            }
            return names;
        }

        private string GetName(List<Unit> units, string fileName)
        {
            var lookup = Path.GetFileName(fileName).ToLower().Split('-');
            if (lookup.Length == 3)
            {
                foreach (var unit in units)
                {
                    var path = Path.GetFileName(unit.BmpPath).ToLower();
                    if (path == lookup[2])
                    {
                        return unit.Name;
                    }
                }
            }

            return string.Empty;
        }

        public void Setup(Platoon platoon, Units units, int phase, int zone, string filter)
        {
            // clear out any previous data
            Reset();

            //*
            var ships = units.GetUnits(Units.ShipsFilter);
            var heroes = units.GetUnits(filter);
            var shipNames = GetNames(ships);
            var heroNames = GetNames(heroes);
            bool useHeroes = (phase < 3 || zone > 1);
            //*/

            if (Directory.Exists(BasePath))
            {
                for (var row = 0; row < Settings.MaxRows; ++row)
                {
                    for (var col = 0; col < Settings.MaxCols; ++col)
                    {
                        /*
                        Donation donation = BattlePlanner.Donations.Get(zone, platoon.Num, row + 1, col + 1);
                        if (donation != null)
                        {
                            SetupCell(platoon, (useHeroes) ? heroNames : shipNames, name, Donations[row, col],
                                files[1], files[0], row + 1, col + 1);
                        }
                        //*/
                        //*
                        var pattern = string.Format("Zone{0}_{1}-{2}_{3}*.png", zone, platoon.Num, row + 1, col + 1);
                        var files = Directory.GetFiles(BasePath, pattern);
                        if (files.Length == 2)
                        {
                            var name = GetName((useHeroes) ? heroes : ships, files[0]);
                            SetupCell(platoon, (useHeroes) ? heroNames : shipNames, name, Donations[row, col],
                                files[1], files[0], row + 1, col + 1);
                        }
                        else if (files.Length == 1)
                        {
                            SetupCell(platoon, (useHeroes) ? heroNames : shipNames, "", Donations[row, col],
                                files[0], null, row + 1, col + 1);
                        }
                        else
                        {
                            Donations[row, col].Clear();
                        }
                        //*/
                    }
                }
            }
        }

        private void SetupCell(Platoon platoon, List<string> names, string unitName, DonationView donation, 
            string fileName1, string fileName2, int row, int col)
        {
            var path1 = System.IO.Path.Combine(BasePath, fileName1);
            var path2 = (fileName2 == null) ? null : System.IO.Path.Combine(BasePath, fileName2);
            donation.Setup(platoon, names, unitName, path1, path2, row, col);
        }

        public void Reset()
        {
            for (var row = 0; row < Settings.MaxRows; ++row)
            {
                for (var col = 0; col < Settings.MaxCols; ++col)
                {
                    Donations[row, col].Clear();
                }
            }
        }

    }
}
