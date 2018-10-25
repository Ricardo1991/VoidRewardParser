﻿using System;
using System.Windows;
using VoidRewardParser.Logic;
using VoidRewardParser.Properties;

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

            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            this.Top = Settings.Default.Top;
            this.Left = Settings.Default.Left;
            this.Height = Settings.Default.Height;
            this.Width = Settings.Default.Width;
            ViewModel.FetchPlatinum = Settings.Default.fetchPlat;
            ViewModel.SkipNotFocus = Settings.Default.SkipNotFocus;
            ViewModel.RenderOverlay = Settings.Default.RenderOverlay;
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
                Settings.Default.Top = RestoreBounds.Top;
                Settings.Default.Left = RestoreBounds.Left;
                Settings.Default.Height = RestoreBounds.Height;
                Settings.Default.Width = RestoreBounds.Width;
            }
            else
            {
                Settings.Default.Top = this.Top;
                Settings.Default.Left = this.Left;
                Settings.Default.Height = this.Height;
                Settings.Default.Width = this.Width;
            }

            Settings.Default.fetchPlat = ViewModel.FetchPlatinum;
            Settings.Default.SkipNotFocus = ViewModel.SkipNotFocus;
            Settings.Default.RenderOverlay = ViewModel.RenderOverlay;
            Settings.Default.Save();
        }
    }
}