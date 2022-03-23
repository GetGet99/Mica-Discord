using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using PInvoke;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace MicaDiscord;

public partial class MainWindow : Window
{
    public static readonly bool NotSupportedBuild = Environment.OSVersion.Version.Build < 22523;
    const string Radius = "0.5rem";
    bool DiscordEffectApplied = false;
    bool _Dark = true;
    bool Dark
    {
        set
        {
            _Dark = value;
            (Resources["Color"] as SolidColorBrush ?? throw new NullReferenceException()).Color = value ? Colors.White : Colors.Black;
        }
        get => _Dark;
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        void RefreshFrame()
        {
            HwndSource mainWindowSrc = HwndSource.FromHwnd(Handle);
            if (NotSupportedBuild && !Settings.Default.UseBackdropAnyway)
            {
                mainWindowSrc.CompositionTarget.BackgroundColor = Dark ? System.Windows.Media.Color.FromRgb(50, 50, 50) :
                       System.Windows.Media.Color.FromRgb(250, 250, 250);
            } else
            {
                mainWindowSrc.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
                DwmApi.DwmExtendFrameIntoClientArea(Handle, new()
                {
                    cxLeftWidth = -1,
                    cxRightWidth = -1,
                    cyTopHeight = -1,
                    cyBottomHeight = -1,
                });
            }
        }
        
        void RefreshDarkMode(bool dark) => CustomPInvoke.SetWindowAttribute(
            Handle,
            CustomPInvoke.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            dark ? 1 : 0 // Always dark as of now, or you can change to: ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark ? 1 : 0
        );
        RefreshDarkMode(dark: true);
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
                if (title == "Discord")
                {
                    title = "";
                    Title = "Mica Discord";
                } else Title = $"Mica Discord - {title}";
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
            {

                var Dark = (await WebView.CoreWebView2.ExecuteScriptAsync("document.getElementsByTagName('html')[0].classList.contains('theme-dark')")) == "true";
                RefreshDarkMode(dark: Dark);
                this.Dark = Dark;
                var LightColorCSS = Dark && Settings.Default.ModeAwareCSS;
                var invc = LightColorCSS ? 250 : 0;
                var regc = LightColorCSS ? 0 : 255;
                var floating = Dark ? 0 : 255;
                await WebView.CoreWebView2.ExecuteScriptAsync($@"
(function () {{
    let s = document.createElement('style');
    s.innerHTML = `
*{{
    --background-primary: rgba({invc},{invc},{invc},0.05);
    --background-secondary: transparent;
    --background-secondary-alt: rgba({invc},{invc},{invc},0.075);
    --background-tertiary: transparent;
    --background-message-hover: rgba({invc},{invc},{invc},0.07);
    --background-floating: rgba({floating},{floating},{floating},0.75);
    --deprecated-store-bg: rgba({invc},{invc},{invc},0.05);
    --channeltextarea-background: rgba({invc},{invc},{invc},0.05);
    --scrollbar-auto-track: transparent;
    --scrollbar-thin-track: #0000;
    --scrollbar-thin-thumb: rgba({invc},{invc},{invc},0.25);
    --scrollbar-auto-thumb: rgba({invc},{invc},{invc},0.25);
}}
code, article {{
    --background-secondary: rgba({invc},{invc},{invc},0.05);
}}
.content-2a4AW9 {{
    --background-secondary: rgba({invc},{invc},{invc},0.025);
}}

.form-3gdLxP {{
    margin-top: 0px !important;
    padding-top: 16px !important;
}}
.form-3gdLxP:before {{
    width: 0px !important;
    height: 0px !important;
}}
.scroller-kQBbkU::-webkit-scrollbar-track {{
    margin-bottom: 0px !important;
}}
.scrollerSpacer-3AqkT9 {{
    display: none;
}}
.content-2a4AW9, .members-3WRCEx, .membersWrap-3NUR2t {{
    min-height: 100%;
    padding: 0px !important;
}}
.chat-2ZfjoI, .container-2cd8Mz, .container-36u7Lw, .applicationStore-2nk7Lo {{
    border-radius: {Radius} 0px 0px 0px;
}}
.chatContent-3KubbW {{
    background-color: rgba({invc},{invc},{invc},0.025) !important;
}}
.container-2cd8Mz {{
    background-color: rgba({invc},{invc},{invc},0.05) !important;
}}

.callContainer-HtHELf {{
    background-color: rgba({regc},{regc},{regc},0);
}}

.panels-3wFtMD {{
    border-radius: {Radius} 0px 0px 0px;
}}
.message-2CShn3, .sidebar-1tnWFu {{
    border-radius: {Radius};
}}
`.trim();
    document.head.appendChild(s);
}})()
".Trim());
                await WebView.CoreWebView2.ExecuteScriptAsync($@"
(function () {{
    
}})()
".Trim());
                RefreshFrame();
            }
        };
        WebView.WebMessageReceived += (_, e) =>
        {
            string s = e.TryGetWebMessageAsString();
            switch (s)
            {

            }
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
            if (SettingsDialog.RequiresReload)
                WebView.Reload();
        };
        SettingsDialog.OnSettingsChanged += () =>
        {
            SetBackdrop((BackdropType)Enum.Parse(typeof(BackdropType), Settings.Default.BackdropType, ignoreCase: true));
        };
        //Icon = ImageSourceFromBitmap(ProgramResources.Logo);
        Back.Click += (_, _) =>
        {
            if (WebView.CanGoBack) WebView.GoBack();
        };
        Forward.Click += (_, _) =>
        {
            if (WebView.CanGoForward) WebView.GoForward();
        };
        SizeChanged += (_, _) =>
        {
            if (WindowState == WindowState.Maximized)
            {
                TitleBar.Margin = new Thickness(7.5, 7.5, 0, 0);
                WebView.Margin = new Thickness(7.5, 0, 7.5, 7.5);
            } else
            {
                TitleBar.Margin = new Thickness(0);
                WebView.Margin = new Thickness(0);
            }
        };
        SetValue(WindowChrome.WindowChromeProperty, WindowChrome);
        WindowChrome.SetIsHitTestVisibleInChrome(Back, true);
        WindowChrome.SetIsHitTestVisibleInChrome(Forward, true);
        WindowChrome.SetIsHitTestVisibleInChrome(Reload, true);
        WindowChrome.SetIsHitTestVisibleInChrome(Setting, true);
        using var g = Graphics.FromImage(new Bitmap(1, 1));
        var dpix = g.DpiX;
    }
    WindowChrome WindowChrome { get; } = new WindowChrome
    {
        UseAeroCaptionButtons = true
    };
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
