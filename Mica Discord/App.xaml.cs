using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using WPFApplication = System.Windows.Application;

namespace MicaDiscord;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WPFApplication
{
    NotifyIcon NotifyIcon { get; } = new NotifyIcon
    {
        Text = "Mica Discord",
        ContextMenuStrip = new ContextMenuStrip() { BackColor = Color.FromArgb(50,50,50), ShowImageMargin = false },
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
            NotifyIcon.Click += (_, _) => OpenMenu();
            var mainwindow = (MainWindow)MainWindow;
            NotifyIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
            new ToolStripButton("Open", default, (_, _) => Dispatcher.Invoke(OpenMenu)) { ForeColor = Color.White },
            new ToolStripButton("Exit", default, (_, _) => Dispatcher.Invoke(() => {
                mainwindow.ForceClose = true;
                MainWindow.Close();
            })) { ForeColor = Color.White }
            });
            NotifyIcon.Visible = Settings.Default.UseSystemTray;
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
