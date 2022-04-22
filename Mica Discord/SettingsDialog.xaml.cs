using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
namespace MicaDiscord;

/// <summary>
/// Interaction logic for SettingsDialog.xaml
/// </summary>
public partial class SettingsDialog : Grid
{
    public bool RequiresReload { get; private set; } = false;
    public void ResetRequiresReload() => RequiresReload = false;
    public SettingsDialog()
    {
        InitializeComponent();
        if (!MainWindow.NotSupportedBuild) OSWarning.Visibility = Visibility.Collapsed;
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
            {
#if DEBUG
                ReloadCSSButton.Visibility = Visibility.Hidden;
#endif
                Backdrop.SelectedItem = Enum.Parse(typeof(CustomPInvoke.BackdropType), Settings.Default.BackdropType);
                ReplaceBackgroundToggle.Content = Settings.Default.ReplaceDiscordBackground ? "Disable" : "Enable";
                UseBackdropAnyway.Content = Settings.Default.UseBackdropAnyway ? "Use Solid Color" : "Enable Anyway";
                Systray.IsChecked = Settings.Default.UseSystemTray;
                DevTools.Content = Settings.Default.EnableDevTools ? "Disable" : "Enable";
                ModeAwareCSS.IsChecked = Settings.Default.ModeAwareCSS;
                ExcessiveAccentColor.IsChecked = Settings.Default.ExcessiveAccentColor;
                RequiresReload = false;
            }
        };
        Backdrop.SelectionChanged += (_, _) =>
        {
            Settings.Default.BackdropType = Backdrop.SelectedItem.ToString();
            Settings.Default.Save();
            OnSettingsChanged?.Invoke();
        };
        ReplaceBackgroundToggle.Click += (_, _) =>
        {
            bool newValue = !Settings.Default.ReplaceDiscordBackground;
            if (newValue)
            {
                if (MessageBox.Show(
                
                    caption: "Warning",
                    messageBoxText: "Replacing Discord Background might violate Discord TOS because this app runs JavaScript code to " +
                    "inject CSS when the page refresh! Please review Discord TOS first and use it with caution!\n" +
                    "Are you sure you still want to continue?\n\n" +
                    "NOTE: We do not take any responsibility of anything happening to your Discord account regardless of clicking Yes or No!",
                    button: MessageBoxButton.YesNo,
                    icon: MessageBoxImage.Warning
                    )
                != MessageBoxResult.Yes)
                    return;
            }
            Settings.Default.ReplaceDiscordBackground = newValue;
            ReplaceBackgroundToggle.Content = newValue ? "Disable" : "Enable";
            Settings.Default.Save();
            OnSettingsChanged?.Invoke();
            RequiresReload = true;
        };
        DevTools.Click += (_, _) =>
        {
            bool newValue = !Settings.Default.EnableDevTools;
            if (newValue)
            {
                if (MessageBox.Show(

                    caption: "Warning",
                    messageBoxText: "If somebody asked you to turn this on (or you did turn it on yourself), please remember that " +
                    "you can get your discord account hacked if you copy and paste malicious code in the DevTools Console.\n" +
                    "Are you sure you still want to continue?\n\n" +
                    "NOTE: We do not take any responsibility of anything happening to your Discord account regardless of clicking Yes or No!",
                    button: MessageBoxButton.YesNo,
                    icon: MessageBoxImage.Warning
                    )
                != MessageBoxResult.Yes)
                    return;
            }
            Settings.Default.EnableDevTools = newValue;
            DevTools.Content = newValue ? "Disable" : "Enable";
            Settings.Default.Save();
            OnSettingsChanged?.Invoke();
            RequiresReload = true;
        };
        UseBackdropAnyway.Click += (_, _) =>
        {
            bool newValue = !Settings.Default.UseBackdropAnyway;
            if (newValue)
            {
                if (MessageBox.Show(

                    caption: "Warning",
                    messageBoxText: "This might cause an unexpected behavior if your build does not support the feature!",
                    button: MessageBoxButton.YesNo,
                    icon: MessageBoxImage.Warning
                    )
                != MessageBoxResult.Yes)
                    return;
            }
            Settings.Default.UseBackdropAnyway = newValue;
            UseBackdropAnyway.Content = newValue ? "Use Solid Color" : "Enable Anyway";
            Settings.Default.Save();
            OnSettingsChanged?.Invoke();
            RequiresReload = true;
        };
    }
    public event Action? OnClose;
    private void CloseDialog(object sender, RoutedEventArgs e) => OnClose?.Invoke();
    public event Action? OnSettingsChanged;

    private void OpenAppFolder(object sender, RoutedEventArgs e)
    {
        Process.Start("powershell", new string[] { "-c", $"Invoke-Item \"{AppDomain.CurrentDomain.BaseDirectory}\"" });
    }

    private void SystrayToggled(object sender, RoutedEventArgs e)
    {
        Settings.Default.UseSystemTray = Systray.IsChecked ?? false;
        Settings.Default.Save();
    }
    private void ModeAwareCSSToggled(object sender, RoutedEventArgs e)
    {
        Settings.Default.ModeAwareCSS = ModeAwareCSS.IsChecked ?? false;
        Settings.Default.Save();
        RequiresReload = true;
    }
    private void ExcessiveAccentColorToggled(object sender, RoutedEventArgs e)
    {
        Settings.Default.ExcessiveAccentColor = ExcessiveAccentColor.IsChecked ?? false;
        Settings.Default.Save();
        RequiresReload = true;
    }

    private void ReloadCSS(object sender, RoutedEventArgs e)
    {
#if DEBUG
#else
        MainWindow.DefinedCSS = System.IO.File.ReadAllText("./The CSS.css");
        RequiresReload = true;
#endif
    }
}
