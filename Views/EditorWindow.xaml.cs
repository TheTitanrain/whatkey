using System.ComponentModel;
using System.Windows;
using WhatKey.ViewModels;

namespace WhatKey.Views
{
    public partial class EditorWindow : Window
    {
        public EditorWindow(EditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Hide instead of close so the app stays alive in tray
            e.Cancel = true;
            Hide();
        }
    }
}
