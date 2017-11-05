using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;

namespace BattlePlanner
{
    /// <summary>
    /// Interaction logic for UnitWindow.xaml
    /// </summary>
    public partial class UnitWindow : Window
    {
        private static UnitWindow Singleton = null;

        private Platoon Platoon = null;
        private Unit Unit = null;
        private int Row = 0;
        private int Col = 0;
        private Bitmap AskBmp = null;
        private Bitmap MatchBmp = null;

        private double PixelPctBackup = 0.0;
        private double ColorPctBackup = 0.0;
        private bool Saved = false;

        public static bool CanCreate()
        {
            return (Singleton == null);
        }

        public UnitWindow()
        {
            Singleton = this;
            InitializeComponent();
        }

        public void Setup(Platoon platoon, Unit unit, int row, int col)
        {
            // set the title
            this.Title = "Debug " + unit.Name;

            // set the position
            Top = Settings.WinY + 200;
            Left = Settings.WinX + 200;

            // cache values
            Platoon = platoon;
            Unit = unit;
            Row = row;
            Col = col;

            // setup the ask image
            var fileName = string.Format("Zone{0}_{1}-{2}_{3}.png", platoon.Zone, platoon.Num, row, col);
            var askLoc = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "output", fileName);
            AskBmp = new Bitmap(askLoc);
            AskImage.Source = Utils.BitmapToBitmapImage(AskBmp);

            tbPixelPct.Text = (unit.PixelPct * 100.0).ToString();
            tbColorPct.Text = (unit.ColorPct * 100.0).ToString();

            // backup the original settings
            PixelPctBackup = Unit.PixelPct;
            ColorPctBackup = Unit.ColorPct;
        }

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            // dispose of resources
            if (MatchBmp != null)
            {
                MatchImage.Source = null;
                MatchBmp.Dispose();
                MatchBmp = null;
            }

            var path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "temp");
            var pattern = string.Format("Zone{0}_{1}-{2}_{3}-*.png", Platoon.Zone, Platoon.Num, Row, Col);
            
            // make sure the directory exists
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // clear the old files
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                File.Delete(file);
            }

            // store the current settings
            Unit.PixelPct = 0.01 * Convert.ToDouble(tbPixelPct.Text);
            Unit.ColorPct = 0.01 * Convert.ToDouble(tbColorPct.Text);

            // make sure the values are clamped
            Clamp();

            // process the image
            ImageCompare.Singleton.GetUnits((Platoon.IsGround) ? Settings.Filter : Units.ShipsFilter);
            ImageCompare.Singleton.ProcessCell(Platoon, Row - 1, Col - 1, path);

            // see if it worked
            files = Directory.GetFiles(path, pattern);
            if (files.Length == 1)
            {
                MatchBmp = new Bitmap(files[0]);
                MatchImage.Source = Utils.BitmapToBitmapImage(MatchBmp);
            }
        }

        private void Clamp()
        {
            Unit.PixelPct = Math.Max(Math.Min(Unit.PixelPct, 1.0), 0.0);
            Unit.ColorPct = Math.Max(Math.Min(Unit.ColorPct, 1.0), 0.0);
            tbPixelPct.Text = (Unit.PixelPct * 100.0).ToString();
            tbColorPct.Text = (Unit.ColorPct * 100.0).ToString();
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            // save the unit changes
            Unit.PixelPct = 0.01 * Convert.ToDouble(tbPixelPct.Text);
            Unit.ColorPct = 0.01 * Convert.ToDouble(tbColorPct.Text);
            Clamp();
            Units.Singleton.Save();

            Saved = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Saved)
            {
                Unit.PixelPct = PixelPctBackup;
                Unit.ColorPct = ColorPctBackup;
            }

            // dispose of resources
            if (AskBmp != null)
            {
                AskImage.Source = null;
                AskBmp.Dispose();
                AskBmp = null;
            }
            if (MatchBmp != null)
            {
                MatchImage.Source = null;
                MatchBmp.Dispose();
                MatchBmp = null;
            }

            if (Saved)
            {
                var srcPath = Path.Combine(Directory.GetCurrentDirectory(), "temp");
                var dstPath = Path.Combine(Directory.GetCurrentDirectory(), "output");
                var baseName = string.Format("Zone{0}_{1}-{2}_{3}", Platoon.Zone, Platoon.Num, Row, Col);
                var pattern = string.Format("{0}-*.png", baseName);

                var files = Directory.GetFiles(srcPath, pattern);
                foreach (var file in files)
                {
                    // copy the new matched file over
                    var fileName = Path.GetFileName(file);
                    var dst = Path.Combine(dstPath, fileName);
                    File.Copy(file, dst);

                    // update the donation
                    var donation = Donations.Get(Platoon.Zone, Platoon.Num, Row, Col);
                    fileName = fileName.Substring(baseName.Length + 1);
                    var unit = Units.Singleton.GetByPath(fileName);
                    if (donation != null && unit != null)
                    {
                        // update existing donation
                        donation.Name = unit.Name;
                    }
                }
            }

            Singleton = null;
        }
    }
}
