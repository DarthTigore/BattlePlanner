using System;
using System.Windows;
using System.Threading;

namespace BattlePlanner
{
    /// <summary>
    /// Interaction logic for ProgressBar.xaml
    /// </summary>
    public partial class ProgressBar : Window
    {
        Thread CallingThread = null;

        int LastZone1 = 0;
        int LastPlatoon1 = 0;
        int LastDonation1 = 0;

        int LastZone2 = 0;
        int LastPlatoon2 = 0;
        int LastDonation2 = 0;

        public ProgressBar()
        {
            InitializeComponent();
            pbStatus.Value = 0;
            labelDesc.Content = string.Empty;
        }

        public void SetThread(Thread callingThread)
        {
            CallingThread = callingThread;
        }

        public void Done()
        {
            CallingThread = null;
            this.Dispatcher.Invoke(new Action(() => this.Close()));
        }

        public void UpdateBar(int zone, int platoon, int donation, double progress)
        {
            var text = string.Format("Zone {0}: Platoon {1}-{2}", zone, platoon, donation);
            pbStatus.Dispatcher.Invoke(new Action(()=> pbStatus.Value = progress));
            labelDesc.Dispatcher.Invoke(new Action(() => labelDesc.Content = text));

            if (LastZone1 > 0)
            {
                Match1.Dispatcher.Invoke(new Action(() => Match1.Setup(LastZone1, LastPlatoon1, LastDonation1)));
            }
            if (LastZone2 > 0)
            {
                Match2.Dispatcher.Invoke(new Action(() => Match2.Setup(LastZone2, LastPlatoon2, LastDonation2)));
            }

            // cache the last values
            LastZone2 = LastZone1;
            LastPlatoon2 = LastPlatoon1;
            LastDonation2 = LastDonation1;
            LastZone1 = zone;
            LastPlatoon1 = platoon;
            LastDonation1 = donation;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Match1.Reset();
            Match2.Reset();

            if (CallingThread != null)
            {
                ImageCompare.EndThread = true;
            }
        }
    }
}
