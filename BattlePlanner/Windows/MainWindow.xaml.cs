using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

using System.IO;
using System.Threading;
using System.Reflection;

namespace BattlePlanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Units Units = new Units();
        private ImageCompare Compare = null;
        private int PlatoonNum = 1;

        private List<PlatoonView> PlatoonViews = new List<PlatoonView>();

        private Spreadsheet Sheet = null;

        private Timer RefreshTimer = null;
        private Timer ProcessTimer = null;
        
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                ErrorLog.Clear();
                InitPlanner();
            }
            catch (Exception e)
            {
                ErrorLog.AddLine(e.ToString());
                Console.WriteLine("Exception: {0}", e.ToString());

                MessageBox.Show(e.ToString(), "Initialize Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        /// <summary>
        /// Check the latest version of the tool and ask for update before launching
        /// </summary>
        /// <returns></returns>
        private void VersionCheck()
        {
            //const string url = "https://drive.google.com/open?id=0B_wez8EOgFTGaUEwVHh4cWlFa28";
        }

        private void InitPlanner()
        {
            VersionCheck();

            try
            {
                // get the settings
                Settings.Load();
                Presets.Load();
            }
            catch
            {
                var message = "Failed to initialize settings.";
                ErrorLog.AddLine(message);
                MessageBox.Show(message, "Initialize Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            try
            {
                // setup the spreadsheet connection
                Sheet = new Spreadsheet();
            }
            catch
            {
                var message = "Failed to initialize Google Sheets.";
                ErrorLog.AddLine(message);
                MessageBox.Show(message, "Initialize Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // initialize UI with settings
            Title = string.Format("Tigore's SWGoH TB Planner ({0})", System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);
            cbPhase.SelectedIndex = Settings.Phase - 1;
            Top = Settings.WinY;
            Left = Settings.WinX;
            cbFilter.Text = Settings.Filter;

            // cache platton view controls
            PlatoonViews.Add(PlatoonView1);
            PlatoonViews.Add(PlatoonView2);
            PlatoonViews.Add(PlatoonView3);
            foreach (var view in PlatoonViews)
            {
                view.Visibility = Visibility.Hidden;
            }

            // load the platoons
            try
            {
                Compare = new ImageCompare(Units);
                if (!Compare.LoadPlatoons())
                {
                    MessageBox.Show("Make sure you place the platoon screenshots in the Platoons directory and they are not locked.",
                        "No Platoons Found",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }
            }
            catch
            {
                var message = "Failed to load platoon images.";
                ErrorLog.AddLine(message);
                MessageBox.Show(message, "Initialize Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // see if we match a preset
            bool settingsChanged = false;
            if (Platoon.Platoons.Count > 0)
            {
                var platoon = Platoon.Platoons[0];
                if (platoon.Bmp.Width != Settings.PlatoonW || platoon.Bmp.Height != Settings.PlatoonH)
                {
                    try
                    {
                        Presets.BestFit(platoon.Bmp.Width, platoon.Bmp.Height);
                        settingsChanged = true;
                    }
                    catch
                    {
                        var message = "Failed to detect best preset for resolution.";
                        ErrorLog.AddLine(message);
                        MessageBox.Show(message, "Initialize Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }
                }
            }

            // load all units
            bool unitsDownloaded = false;
            if (!Units.Load())
            {
                try
                {
                    RefreshData(null);
                    unitsDownloaded = true;
                }
                catch
                {
                    var message = "Failed to refresh data from swgoh.gg.";
                    ErrorLog.AddLine(message);
                    MessageBox.Show(message + " Make sure the Battle Planner directory is not write protected.", "Initialize Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }
            }

            try
            {
                if (!unitsDownloaded)
                {
                    // see if there were any new units added since the last download
                    Units.RefreshData(false);
                }

                // optimize the unit images
                var format = Compare.GetPlatoonFormat();
                Units.Optimize(format);
            }
            catch
            {
                var message = "Failed to optimize images.";
                ErrorLog.AddLine(message);
                MessageBox.Show(message, "Initialize Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // show settings if we haven't initialized
            if (settingsChanged || Settings.SpreadsheetID == null || Settings.SpreadsheetID.Length == 0)
            {
                try
                {
                    SettingsWin win = new SettingsWin();
                    win.Visibility = Visibility.Visible;
                    if (!win.IsReady)
                    {
                        MessageBox.Show("Failed to open the settings window.", "Initialize Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }
                }
                catch
                {
                    var message = "Failed to open the settings window.";
                    ErrorLog.AddLine(message);
                    MessageBox.Show(message, "Initialize Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }
            }

            try
            {
                // load the previous donations
                Donations.Load();
            }
            catch
            {
                var message = "Failed to load donations.";
                ErrorLog.AddLine(message);
                MessageBox.Show(message, "Initialize Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
        }

        private void RefreshData(Object stateInfo)
        {
            if (RefreshTimer != null)
            {
                RefreshTimer.Dispose();
                RefreshTimer = null;
            }

            Units.RefreshData();

            Units.Load();

            // optimize the unit images
            var format = Compare.GetPlatoonFormat();
            Units.Optimize(format);
        }

        private void ProcessPlatoons(Object stateInfo)
        {
            if (ProcessTimer != null)
            {
                ProcessTimer.Dispose();
                ProcessTimer = null;
            }

            // make sure we have the right number of platoons
            var phase = Convert.ToInt32(stateInfo);
            if (Compare.Validate(phase))
            {
                var bar = new ProgressBar();
                bar.Visibility = Visibility.Visible;
                bar.Top = Top + 100;
                bar.Left = Left + 100;

                var filter = GetFilter();
                var thread = Compare.Process(phase, bar, filter);

                bar.SetThread(thread);

                // show the first set of platoons
                //PlatoonNum = 1;
                //labelPlatoon.Content = "Platoon " + PlatoonNum;
                //SetupPlatoons();
            }
            else
            {
                // warn that not enough platoons provided for the phase
                MessageBox.Show("Make sure you have the correct number of screenshots for the selected Phase.",
                    "Phase/Screenshot Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Scan through the output directory and find which units have not been matched before.
        /// </summary>
        private void Audit()
        {
            var filter = GetFilter();
            var output = Audit(Units.ShipsFilter);
            output += Audit(filter);

            Utils.WriteFile("audit.txt", output);
        }

        private string Audit(string filter)
        {
            var path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "output");
            var units = Units.GetUnits(filter);
            var output = string.Empty;

            if (Directory.Exists(path))
            {
                foreach (var unit in units)
                {
                    var fileName = System.IO.Path.GetFileName(unit.BmpPath);
                    var files = Directory.GetFiles(path, "*" + fileName);
                    if (files.Length == 0)
                    {
                        output += unit.Name + Environment.NewLine;
                    }
                }
            }

            return output;
        }

        private string GetFilter()
        {
            var filter = cbFilter.SelectionBoxItem.ToString();
            return filter;
        }

        private void SetupPlatoons()
        {
            var phase = cbPhase.SelectedIndex + 1;
            var filter = GetFilter();

            var zone = 0;
            foreach (var view in PlatoonViews)
            {
                ++zone;
                view.Visibility = Visibility.Hidden;
                if (zone == 1 || phase >= 2)
                {
                    var platoon = Platoon.Get(zone, PlatoonNum);
                    if (platoon != null)
                    {
                        view.Setup(platoon, Units, phase, zone, filter);
                        view.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private List<string> GetPlatoonNames(Platoon platoon, Units units, int phase, int zone, string filter)
        {
            List<string> names = new List<string>();
            for (var row = 0; row < Settings.MaxRows; ++row)
            {
                for (var col = 0; col < Settings.MaxCols; ++col)
                {
                    Donation donation = Donations.Get(zone, platoon.Num, row + 1, col + 1);
                    if (donation != null && donation.Name.Length > 0 && donation.Upload)
                    {
                        names.Add(donation.Name);
                    }
                    else
                    {
                        names.Add(string.Empty);
                    }
                }
            }

            return names;
        }

        private void tbRefresh_Click(object sender, RoutedEventArgs e)
        {
            // download the source data from swgoh.gg
            RefreshTimer = new Timer(RefreshData, null, 1000, 0);
            Units.StartRefreshData();
        }

        private void tbProcess_Click(object sender, RoutedEventArgs e)
        {
            var sheetPhase = Sheet.GetPhase();
            var uiPhase = cbPhase.SelectedIndex + 1;
            if (sheetPhase != uiPhase)
            {
                var mbResult = MessageBox.Show("The phase in the UI does not match the phase in the spreadsheet. Are you sure you want to continue?",
                    "Warning: Phase Mismatch",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (mbResult != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // process the images
            foreach (var view in PlatoonViews)
            {
                view.Reset();
                view.Visibility = Visibility.Hidden;
            }

            ProcessPlatoons(cbPhase.SelectedIndex + 1);
            //ProcessTimer = new Timer(ProcessPlatoons, cbPhase.SelectedIndex + 1, 1000, 0);
        }

        private void tbUpload_Click(object sender, RoutedEventArgs e)
        {
            // cycle through platoons
            var phase = Sheet.GetPhase();
            var filter = GetFilter();
            var groundOnly = (Platoon.Platoons.Count < (Settings.MaxZones * Settings.PlattonsPerZone));

            for (int platoonNum = 1; platoonNum <= Settings.PlattonsPerZone; ++platoonNum)
            {
                for (int zone = 1; zone <= Settings.MaxZones; ++zone)
                {
                    if (phase < 3 && zone > phase)
                    {
                        continue;
                    }

                    var platoon = Platoon.Get(zone, platoonNum);
                    var names = GetPlatoonNames(platoon, Units, phase, zone, filter);
                    var zoneOffset = (groundOnly) ? 1 : 0;

                    Sheet.Write(names, zone + zoneOffset, platoon.Num);
                }
            }
        }

        private void tbSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWin win = new SettingsWin();
            win.Visibility = Visibility.Visible;
        }

        private void tbPrevious_Click(object sender, RoutedEventArgs e)
        {
            // cylce to the previous plattons
            PlatoonNum = (PlatoonView1.Visibility == Visibility.Hidden) ? 1 : Math.Max(1, --PlatoonNum);
            labelPlatoon.Content = "Platoon " + PlatoonNum;
            SetupPlatoons();
        }

        private void tbNext_Click(object sender, RoutedEventArgs e)
        {
            // cycle to the next platoons
            PlatoonNum = (PlatoonView1.Visibility == Visibility.Hidden) ? 1 : Math.Min(Settings.PlattonsPerZone, ++PlatoonNum);
            labelPlatoon.Content = "Platoon " + PlatoonNum;
            SetupPlatoons();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // update the settings before closing
            Settings.Phase = cbPhase.SelectedIndex + 1;
            Settings.WinX = Convert.ToInt32(Left);
            Settings.WinY = Convert.ToInt32(Top);
            Settings.Save();

            // close all child windows
            if (SettingsWin.Singleton != null)
            {
                SettingsWin.Singleton.Close();
            }
            if (UnitWindow.Singleton != null)
            {
                UnitWindow.Singleton.Close();
            }
        }

        private void cbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Filter = GetFilter();
        }

        private void tbWeb_Click(object sender, RoutedEventArgs e)
        {
            string link = Spreadsheet.GetLink();
            if (link.Length > 0)
            {
                // open the spreadsheet
                System.Diagnostics.Process.Start(link);
            }
        }
    }
}
