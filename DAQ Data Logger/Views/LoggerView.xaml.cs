using DAQ_Data_Logger.Views;
using MccDaq;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DAQ_Data_Logger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnOpenViewer_Click(object sender, RoutedEventArgs e)
        {
            DatabaseBrowserView BrowserView = new DatabaseBrowserView();
            BrowserView.Show();
        }
    }
}

