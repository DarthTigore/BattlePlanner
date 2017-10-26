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
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CallingThread != null)
            {
                CallingThread.Abort();
            }
        }
    }
}
