using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;

namespace BattlePlanner.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWin.xaml
    /// </summary>
    public partial class SettingsWin : Window
    {
        public bool IsReady = false;

        private Bitmap BmpPlatoon = null;
        private Bitmap BmpUnit = null;

        private int ScrollXDiff = 0;
        private int ScrollYDiff = 0;

        public SettingsWin()
        {
            InitializeComponent();

            Populate();
        }

        private void Clear()
        {
            if (BmpPlatoon != null)
            {
                BmpPlatoon.Dispose();
                BmpPlatoon = null;
            }
            if (BmpUnit != null)
            {
                BmpUnit.Dispose();
                BmpUnit = null;
            }
        }

        private void Populate()
        {
            tbID.Text = Settings.SpreadsheetID;
            checkAutoPost.IsChecked = Settings.AutoPost;

            tbStartX.Text = Settings.XStart.ToString();
            tbStartY.Text = Settings.YStart.ToString();
            tbCellDim.Text = Settings.CellDim.ToString();
            tbOffsetX.Text = Settings.XOffset.ToString();
            tbOffsetY.Text = Settings.YOffset.ToString();
            tbSubDim.Text = Settings.Crop.ToString();
            tbCompareSize.Text = Settings.CompareSize.ToString();

            if (Platoon.Platoons.Count > 0 && Platoon.Platoons[0].Bmp != null)
            {
                try
                {
                    // try to populate the platoon image
                    BmpPlatoon = (Bitmap)Platoon.Platoons[0].Bmp.Clone();
                    var bmpSrc1 = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                      BmpPlatoon.GetHbitmap(),
                      IntPtr.Zero,
                      Int32Rect.Empty,
                      BitmapSizeOptions.FromEmptyOptions());
                    brushBackground.ImageSource = bmpSrc1;

                    // try to populate the unit image
                    BmpUnit = (Bitmap)Units.Singleton.GetUnits("light-side")[0].Bmp.Clone();
                    var bmpSrc2 = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                      BmpUnit.GetHbitmap(),
                      IntPtr.Zero,
                      Int32Rect.Empty,
                      BitmapSizeOptions.FromEmptyOptions());
                    brushUnitBackground.ImageSource = bmpSrc2;

                    // set the canvas size
                    canvasPlatoon.Width = BmpPlatoon.Width;
                    canvasPlatoon.Height = BmpPlatoon.Height;

                    // store the difference between the original size
                    ScrollXDiff = Convert.ToInt32(this.Width - scrollPlatoon.Width);
                    ScrollYDiff = Convert.ToInt32(this.Height - scrollPlatoon.Height);

                    DrawGrid();

                    IsReady = true;
                }
                catch
                {
                    // failed to initialze the window
                    Close();
                }
            }
            else
            {
                // failed to initialize the window
                Close();
            }
        }

        private int Clamp(TextBox tb, int min, int max)
        {
            var value = Convert.ToInt32(tb.Text);
            value = Math.Min(Math.Max(value, min), max);
            tb.Text = value.ToString();

            return value;
        }

        private void DrawGrid()
        {
            try
            {
                var xStart = Clamp(tbStartX, 0, BmpPlatoon.Width);
                var yStart = Clamp(tbStartY, 0, BmpPlatoon.Height);
                var cellDim = Clamp(tbCellDim, 10, BmpPlatoon.Height / Settings.MaxRows);
                var xOffset = Clamp(tbOffsetX, cellDim, BmpPlatoon.Width / Settings.MaxCols);
                var yOffset = Clamp(tbOffsetY, cellDim, BmpPlatoon.Height / Settings.MaxRows);
                var xMax = xStart + cellDim + (Settings.MaxCols - 1) * xOffset;
                var yMax = yStart + cellDim + (Settings.MaxRows - 1) * yOffset;

                // draw the starting lines
                bool isPlatoon = true;
                DrawLine(xStart, yStart, xMax, yStart, isPlatoon);
                DrawLine(xStart, yStart, xStart, yMax, isPlatoon);

                for (var row = 0; row < Settings.MaxRows; ++row)
                {
                    // horizontal lines
                    var y = yStart + row * yOffset;
                    DrawLine(xStart, y, xMax, y, isPlatoon);
                    DrawLine(xStart, y + cellDim, xMax, y + cellDim, isPlatoon);

                    for (var col = 0; col < Settings.MaxCols; ++col)
                    {
                        // vertical lines
                        var x = xStart + col * xOffset;
                        DrawLine(x, yStart, x, yMax, isPlatoon);
                        DrawLine(x + cellDim, yStart, x + cellDim, yMax, isPlatoon);
                    }
                }

                // draw the unit's grid
                var compareSize = Clamp(tbCompareSize, 10, cellDim);
                var scale = Convert.ToDouble(BmpUnit.Width) / Convert.ToDouble(compareSize);
                var subDim = Convert.ToInt32(scale * Convert.ToDouble(Clamp(tbSubDim, 0, compareSize / 2)));
                int x1 = subDim;
                int x2 = BmpUnit.Width - subDim;
                int y1 = subDim;
                int y2 = BmpUnit.Height - subDim;
                isPlatoon = false;

                // top line
                DrawLine(x1, y1, x2, y1, isPlatoon);

                // bottom line
                DrawLine(x1, y2, x2, y2, isPlatoon);

                // left line
                DrawLine(x1, y1, x1, y2, isPlatoon);

                // right line
                DrawLine(x2, y1, x2, y2, isPlatoon);
            }
            catch
            {
                MessageBox.Show("Failed to draw the debug images in Settings window.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DrawLine(double x1, double y1, double x2, double y2, bool isPlatoon)
        {
            var line = new Line();

            line.Stroke = System.Windows.Media.Brushes.Red;
            line.StrokeThickness = 1;
            line.SnapsToDevicePixels = true;

            line.X1 = x1;
            line.X2 = x2;
            line.Y1 = y1;
            line.Y2 = y2;

            if (isPlatoon)
            {
                canvasPlatoon.Children.Add(line);
            }
            else
            {
                canvasUnit.Children.Add(line);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            // get the values
            Settings.SpreadsheetID = tbID.Text;
            Settings.AutoPost = Convert.ToBoolean(checkAutoPost.IsChecked);

            Settings.XStart = Clamp(tbStartX, 0, BmpPlatoon.Width);
            Settings.YStart = Clamp(tbStartY, 0, BmpPlatoon.Height);
            Settings.CellDim = Clamp(tbCellDim, 10, BmpPlatoon.Height / Settings.MaxRows);
            Settings.XOffset = Clamp(tbOffsetX, Settings.CellDim, BmpPlatoon.Width / Settings.MaxCols);
            Settings.YOffset = Clamp(tbOffsetY, Settings.CellDim, BmpPlatoon.Height / Settings.MaxRows);
            Settings.CompareSize = Clamp(tbCompareSize, 10, Settings.CellDim);
            Settings.Crop = Clamp(tbSubDim, 0, Settings.CompareSize / 2);
            
            // store and close
            Settings.Save();

            // optimize assets
            var format = ImageCompare.Singleton.GetPlatoonFormat();
            Units.Singleton.Optimize(format);

            Close();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            canvasPlatoon.Children.Clear();
            canvasUnit.Children.Clear();
            DrawGrid();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            scrollPlatoon.Width = e.NewSize.Width - ScrollXDiff;
            scrollPlatoon.Height = e.NewSize.Height - ScrollYDiff;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // cleanup resources
            Clear();
        }
    }
}
