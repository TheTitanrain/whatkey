using System;
using System.Diagnostics;
using System.Reflection;
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
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
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
            try
            {
                var svc = new UpdateService();
                Version current = Assembly.GetExecutingAssembly().GetName().Version;
                UpdateCheckResult result = await svc.CheckForUpdateAsync(current);

                if (result.UpdateAvailable)
                {
                    var answer = MessageBox.Show(
                        $"Version {result.LatestVersion} is available. Open release page?",
                        "Update available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (answer == MessageBoxResult.Yes && !string.IsNullOrEmpty(result.ReleaseUrl))
                        Process.Start(new ProcessStartInfo(result.ReleaseUrl) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("You're up to date.", "No updates", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not check for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
