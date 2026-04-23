using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using WhatKey.Services;

namespace WhatKey.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            Version v = App.CurrentVersion;
            VersionText.Text = $"v{v.Major}.{v.Minor}.{v.Build}";
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            button.IsEnabled = false;
            try
            {
                var svc = new UpdateService();
                UpdateCheckResult result = await svc.CheckForUpdateAsync(App.CurrentVersion);
                App.ShowUpdateResult(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not check for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
