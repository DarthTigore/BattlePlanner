using System.Windows.Controls;
using System.Drawing;
using System.IO;

namespace BattlePlanner
{
    /// <summary>
    /// Interaction logic for MatchControl.xaml
    /// </summary>
    public partial class MatchControl : UserControl
    {
        private Bitmap AskBmp = null;
        private Bitmap MatchBmp = null;

        public MatchControl()
        {
            InitializeComponent();
        }

        public void Setup(int zone, int platoon, int donation)
        {
            Reset();

            var row = 1 + ((donation - 1) / Settings.MaxCols);
            var col = 1 + ((donation - 1) % Settings.MaxCols);

            // setup ask image
            var path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "output");
            var fileName = string.Format("Zone{0}_{1}-{2}_{3}.png", zone, platoon, row, col);
            var askLoc = System.IO.Path.Combine(path, fileName);
            AskBmp = new Bitmap(askLoc);
            AskImage.Source = Utils.BitmapToBitmapImage(AskBmp);

            // setup match image
            var pattern = string.Format("Zone{0}_{1}-{2}_{3}-*.png", zone, platoon, row, col);
            var files = Directory.GetFiles(path, pattern);
            if (files.Length == 1)
            {
                MatchBmp = new Bitmap(files[0]);
                MatchImage.Source = Utils.BitmapToBitmapImage(MatchBmp);
            }

            labelDesc.Content = string.Format("Zone {0}: {1}-{2}", zone, platoon, donation);
        }

        public void Reset()
        {
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

            labelDesc.Content = string.Empty;
        }
    }
}
