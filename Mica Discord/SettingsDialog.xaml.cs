using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using ModernWpf.Controls;
namespace MicaDiscord;

/// <summary>
/// Interaction logic for SettingsDialog.xaml
/// </summary>
public partial class SettingsDialog : Grid
{
    public SettingsDialog()
    {
        InitializeComponent();
        IsVisibleChanged += (_, _) =>
        {
            Backdrop.SelectedItem = Enum.Parse(typeof(BackdropType), Settings.Default.BackdropType);
            ReplaceBackgroundToggle.Content = Settings.Default.ReplaceDiscordBackground ? "Disable" : "Enable";
            Systray.IsChecked = Settings.Default.UseSystemTray;
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
    }
}
