using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using ModernWpf;
using PInvoke;
using System.Drawing;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
namespace MicaDiscord;

public partial class MainWindow : Window
{
    bool DiscordEffectApplied = false;
    void OnLoaded(object sender, RoutedEventArgs e)
    {
        var TitleBar = AppWindow.TitleBar;
        void RefreshFrame()
        {
            HwndSource mainWindowSrc = HwndSource.FromHwnd(Handle);
            mainWindowSrc.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);

            Graphics desktop = Graphics.FromHwnd(Handle);
            float DesktopDpiX = desktop.DpiX;

            DwmApi.DwmExtendFrameIntoClientArea(Handle, new()
            {
                cxLeftWidth = Convert.ToInt32(5 * (DesktopDpiX / 96)),
                cxRightWidth = Convert.ToInt32(5 * (DesktopDpiX / 96)),
                cyTopHeight = Convert.ToInt32(((int)ActualHeight + 5) * (DesktopDpiX / 96)),
                cyBottomHeight = Convert.ToInt32(5 * (DesktopDpiX / 96))
            });
            var location1 = TitleTextBlock.TransformToAncestor(this).Transform(new(0, 0));
            var location2 = TitleBarDragable.TransformToAncestor(this).Transform(new(0, 0));
            TitleBar.SetDragRectangles(new Windows.Graphics.RectInt32[]
            {
                new(_X: 0, 0, (int)(TitleTextBlock.ActualWidth+location1.X), TitleBar.Height),
                new(_X: (int)location2.X, 0, (int)TitleBarDragable.ActualWidth, TitleBar.Height)
            });
        }
        
        void RefreshDarkMode() => CustomPInvoke.SetWindowAttribute(
            Handle,
            CustomPInvoke.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            1 // Always dark as of now, or you can change to: ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark ? 1 : 0
        );
        RefreshDarkMode();
        SizeChanged += (_, _) => RefreshFrame();
        IsVisibleChanged += (_, _) => RefreshFrame();
        ThemeManager.Current.ActualApplicationThemeChanged += (_, _) => RefreshDarkMode();
        Width += 1;
        
        SetBackdrop((BackdropType)Enum.Parse(typeof(BackdropType), Settings.Default.BackdropType, ignoreCase: true));
        WebView.CoreWebView2InitializationCompleted += (_, _) => RefreshFrame();
        WebView.NavigationCompleted += async delegate
        {
            DiscordEffectApplied = Settings.Default.ReplaceDiscordBackground;
            if (DiscordEffectApplied)
                await WebView.CoreWebView2.ExecuteScriptAsync(@"
(function () {
    let s = document.createElement('style');
    s.innerHTML = `
*{
    --background-primary: #fff0;
    --background-secondary: rgba(50,50,50,0.25);
    --background-secondary-alt: rgba(50,50,50,0.25);
    --background-tertiary: #fff0;
    --background-floating: rgba(50,50,50,0.75);
    --deprecated-store-bg: rgba(50,50,50,0.25);
}
.theme-dark .container-2cd8Mz {
    background-color: rgba(50,50,50,0.25);
}
`.trim();
    document.head.appendChild(s);
})()
".Trim());
        };
        
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        this.TitleBar.Height = TitleBar.Height;
    }

    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        DialogPlace.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Hidden;
    }
}
public partial class MainWindow : Window
{
    public MainWindow()
    {
        WindowInteropHelper = new WindowInteropHelper(this);
        InitializeComponent();
        Loaded += OnLoaded;
        SettingsDialog.OnClose += () =>
        {
            DialogPlace.Visibility = Visibility.Hidden;
            WebView.Visibility = Visibility.Visible;
            if (DiscordEffectApplied != Settings.Default.ReplaceDiscordBackground) WebView.Reload();
        };
        SettingsDialog.OnSettingsChanged += () =>
        {
            SetBackdrop((BackdropType)Enum.Parse(typeof(BackdropType), Settings.Default.BackdropType, ignoreCase: true));
        };
    }

    void SetBackdrop(BackdropType BackdropType) => SetBackdrop((int)BackdropType);
    void SetBackdrop(int BackdropType)
    {
        CustomPInvoke.SetWindowAttribute(
            Handle,
            CustomPInvoke.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            BackdropType);
    }

    public WindowInteropHelper WindowInteropHelper { get; }
    public IntPtr Handle => WindowInteropHelper.Handle;
    public WindowId Id => Win32Interop.GetWindowIdFromWindow(Handle);
    public AppWindow AppWindow => AppWindow.GetFromWindowId(Id);

}
