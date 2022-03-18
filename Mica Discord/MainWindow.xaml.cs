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
namespace SystemBackdropTypes;

public partial class MainWindow : Window
{

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
        }
        RefreshFrame();
        void RefreshDarkMode() => CustomPInvoke.SetWindowAttribute(
            Handle,
            CustomPInvoke.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            1 // Always dark as of now, or you can change to: ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark ? 1 : 0
        );
        RefreshDarkMode();
        SizeChanged += (_, _) => RefreshFrame();
        ThemeManager.Current.ActualApplicationThemeChanged += (_, _) => RefreshDarkMode();
        
        SetBackdrop(CustomPInvoke.BackdropType.Mica);

        WebView.NavigationCompleted += delegate
        {
            WebView.CoreWebView2.ExecuteScriptAsync(@"
(function () {
    let s = document.createElement('style');
    s.innerHTML = `
*{
    --background-primary: #fff0;
    --background-secondary: rgba(50,50,50,0.25);
    --background-secondary-alt: rgba(50,50,50,0.25);
    --background-tertiary: #fff0;
}
`.trim();
    document.head.appendChild(s);
})()
".Trim());
        };
        //var TitleBar = AppWindow.TitleBar;
        //TitleBar.ExtendsContentIntoTitleBar = true;
        //TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        //this.TitleBar.Height = TitleBar.Height;
    }
}
public partial class MainWindow : Window
{
    public MainWindow()
    {
        Bootstrap.Initialize(0x00010000);
        WindowInteropHelper = new WindowInteropHelper(this);
        InitializeComponent();
        Loaded += OnLoaded;

    }
    protected override void OnClosed(EventArgs e)
    {
        Bootstrap.Shutdown();
    }

    void SetBackdrop(CustomPInvoke.BackdropType BackdropType) => SetBackdrop((int)BackdropType);
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
