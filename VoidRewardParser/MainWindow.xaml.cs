using System;
using System.Windows;
using VoidRewardParser.Logic;

namespace VoidRewardParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            ViewModel.MissionComplete += ViewModel_MissionComplete;

            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Width;
            ViewModel.FetchPlatinum = Properties.Settings.Default.fetchPlat;
        }

        private void ViewModel_MissionComplete(object sender, EventArgs e)
        {
            if (ViewModel.MoveToTop)
            {
                Activate();
                Topmost = true;
                Topmost = false;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ViewModel.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadCommand.Execute();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
            }
            else
            {
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
            }

            Properties.Settings.Default.fetchPlat = ViewModel.FetchPlatinum;
            Properties.Settings.Default.Save();
        }
    }
}