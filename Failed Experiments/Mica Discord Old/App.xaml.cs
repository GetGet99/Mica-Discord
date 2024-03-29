﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MicaDiscord;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MicaDiscord
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {

        NotifyIcon NotifyIcon { get; } = new NotifyIcon
        {
            Text = "Mica Discord",
            ContextMenuStrip = new ContextMenuStrip(),
            Icon = ToIcon(ProgramResources.Logo)
        };
        static Icon ToIcon(Bitmap img)
        {
            var iconHandle = img.GetHicon();
            return Icon.FromHandle(iconHandle);
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                if (Environment.OSVersion.Version.Build < 22523)
                {
                    System.Windows.MessageBox.Show("This demonstration requires Windows 11 Insider Preview build 22523 or newer.", "Incompatible Windows build", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
                NotifyIcon.Click += (_, _) => OpenMenu();
                var mainwindow = new MainWindow();
                NotifyIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
                {
                new ToolStripButton("Open", default, (_, _) => Dispatcher.Invoke(OpenMenu)),
                new ToolStripButton("Exit", default, (_, _) => Dispatcher.Invoke(() => {
                    mainwindow.ForceClose = true;
                    MainWindow.Close();
                }))
                });
                NotifyIcon.Visible = Settings.Default.UseSystemTray;
                mainwindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"{ex.Message} {ex.Source} {ex.StackTrace}");
            }
        }
        void OpenMenu()
        {
            var window = MainWindow;
            if (window is not null)
            {
                window.Show();
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
                window.Activate();
            }
        }
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            NotifyIcon.Visible = false;
        }
        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                var ex = (Exception)eventArgs.ExceptionObject;
                System.Windows.Forms.MessageBox.Show($"{ex.Message} {ex.Source} {ex.StackTrace}");
            };
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
