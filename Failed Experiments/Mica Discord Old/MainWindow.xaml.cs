using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using ModernWpf;
using PInvoke;
using System.Drawing;
using Microsoft.UI;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace MicaDiscord;

public partial class MainWindow : Window
{
    bool DiscordEffectApplied = false;

    void OnLoaded(object sender, RoutedEventArgs e)
    {
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
        }
        
        void RefreshDarkMode() => CustomPInvoke.SetWindowAttribute(
            Handle,
            CustomPInvoke.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            1 // Always dark as of now, or you can change to: ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark ? 1 : 0
        );
        RefreshDarkMode();
        SizeChanged += (_, _) => RefreshFrame();
        IsVisibleChanged += (_, _) => RefreshFrame();
        //ThemeManager.Current.ActualApplicationThemeChanged += (_, _) => RefreshDarkMode();
        Width += 1;
        
        SetBackdrop((BackdropType)Enum.Parse(typeof(BackdropType), Settings.Default.BackdropType, ignoreCase: true));
        WebView.CoreWebView2InitializationCompleted += (_, _) =>
        {
            WebView.CoreWebView2.DocumentTitleChanged += (_, _) =>
            {
                var title = WebView.CoreWebView2.DocumentTitle;
                if (title == "Discord") title = "";
                Title = $"Mica Discord - {title}";
                WebsiteTitle.Text = title;
            };
            WebView.CoreWebView2.HistoryChanged += delegate
            {
                Back.IsEnabled = WebView.CanGoBack;
                Forward.IsEnabled = WebView.CanGoForward;
                RefreshFrame();
            };
            RefreshFrame();
        };
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
    --background-secondary: rgba(50,50,50,0.05);
    --background-secondary-alt: rgba(50,50,50,0.05);
    --background-tertiary: #fff0;
    --background-floating: rgba(50,50,50,0.75);
    --deprecated-store-bg: rgba(50,50,50,0.05);
    --channeltextarea-background: rgba(50,50,50,0.25);
}
.theme-dark .container-2cd8Mz {
    background-color: rgba(50,50,50,0.25);
}

.callContainer-HtHELf {
    background-color: rgba(50,50,50,0);
}
`.trim();
    document.head.appendChild(s);
})()
".Trim());
        };
        Closing += (_, e) =>
        {
            if (!ForceClose && Settings.Default.UseSystemTray)
            {
                e.Cancel = true;
                Hide();
            }
        };
        //TitleBarUI.Margin = new Thickness(0, 0, (SystemParameters.WindowCaptionButtonWidth+10) * 3, 0);
        //this.TitleBar.Height = TitleBar.Height;
    }
    
    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        DialogPlace.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Hidden;
    }

    private void RefreshPage(object sender, RoutedEventArgs e)
    {
        try
        {
            WebView.Reload();
        } catch { }
    }
}
public partial class MainWindow : Window
{
    public bool ForceClose { get; set; } = false;
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
        Icon = ImageSourceFromBitmap(ProgramResources.Logo);
        Back.Click += (_, _) =>
        {
            if (WebView.CanGoBack) WebView.GoBack();
        };
        Forward.Click += (_, _) =>
        {
            if (WebView.CanGoForward) WebView.GoForward();
        };
        
    }
    public static ImageSource ImageSourceFromBitmap(Bitmap bmp)
    {
        var handle = bmp.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        finally { CustomPInvoke.DeleteObject(handle); }
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

}
