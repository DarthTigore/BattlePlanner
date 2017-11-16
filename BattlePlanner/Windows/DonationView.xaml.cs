using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Drawing;

namespace BattlePlanner
{
    /// <summary>
    /// Interaction logic for DonationView.xaml
    /// </summary>
    public partial class DonationView : UserControl
    {
        private Platoon Platoon = null;
        private int Row = 0;
        private int Col = 0;

        private Bitmap AskBmp = null;
        private Bitmap MatchBmp = null;
        private string MatchPath = string.Empty;

        private bool IgnoreChanges = false;

        public DonationView()
        {
            InitializeComponent();
        }

        public void Setup(Platoon platoon, List<string> names, string unitName, string image1, string image2, int row, int col)
        {
            IgnoreChanges = true;

            Platoon = platoon;
            Row = row;
            Col = col;

            cbUnitName.Items.Clear();
            foreach (var name in names)
            {
                cbUnitName.Items.Add(name);
            }
            cbUnitName.SelectedValue = unitName;

            // setup the ask image
            AskBmp = new Bitmap(image1);
            var bmpSrc1 = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
              AskBmp.GetHbitmap(),
              IntPtr.Zero,
              Int32Rect.Empty,
              BitmapSizeOptions.FromEmptyOptions());
            AskImage.Source = bmpSrc1;

            LoadMatch(image2);

            IgnoreChanges = false;
        }

        public void LoadMatch(string imagePath)
        {
            if (imagePath != null)
            {
                MatchBmp = new Bitmap(imagePath);
                var bmpSrc2 = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                  MatchBmp.GetHbitmap(),
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  BitmapSizeOptions.FromEmptyOptions());
                MatchImage.Source = bmpSrc2;

                if (MatchPath.Length > 0 && MatchPath != imagePath)
                {
                    // delete the previous image
                    try
                    {
                        System.IO.File.Delete(MatchPath);
                    }
                    catch { }
                }
                MatchPath = imagePath;

                cbApproved.IsChecked = true;
            }
            else
            {
                MatchImage.Source = null;
                cbApproved.IsChecked = false;
            }
        }

        public void Clear()
        {
            IgnoreChanges = true;

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

            cbUnitName.Items.Clear();
            cbApproved.IsChecked = false;

            IgnoreChanges = false;
        }

        private void MatchImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (cbUnitName.SelectedIndex >= 0)
            {
                var unitName = cbUnitName.SelectionBoxItem.ToString();
                if (Units.Singleton != null && UnitWindow.CanCreate())
                {
                    var unit = Units.Singleton.GetUnit(unitName);

                    var win = new UnitWindow(this);
                    win.Setup(Platoon, unit, Row, Col);
                    
                    win.Visibility = Visibility.Visible;
                }
            }
        }

        private void cbApproved_Checked(object sender, RoutedEventArgs e)
        {
            if (!IgnoreChanges)
            {
                var donation = Donations.Get(Platoon.Zone, Platoon.Num, Row, Col);
                if (donation != null)
                {
                    donation.Upload = true;
                }
            }
        }

        private void cbApproved_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IgnoreChanges)
            {
                var donation = Donations.Get(Platoon.Zone, Platoon.Num, Row, Col);
                if (donation != null)
                {
                    donation.Upload = false;
                }
            }
        }

        public void FreeMatchResources()
        {
            // free up the match
            if (MatchBmp != null)
            {
                MatchImage.Source = null;
                MatchBmp.Dispose();
                MatchBmp = null;
            }
        }
    }
}
