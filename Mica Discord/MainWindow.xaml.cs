using Microsoft.Web.WebView2.Core;
using PInvoke;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Windows.UI.ViewManagement;
using AccentColorTypes = CustomPInvoke.UxTheme.AccentColorTypes;
using System.IO;
//using WindowMessage = PInvoke.User32.WindowMessage;

namespace MicaDiscord;

public partial class MainWindow : Window
{
    static UISettings UISettings { get; } = new();
    static bool IsExcessiveAccentColorEnabled => Settings.Default.ExcessiveAccentColor;
    static string DefinedCSS
#if DEBUG
        => File.ReadAllText("../../../The CSS.css");// Read File Every time
#else
        = File.ReadAllText("./The CSS.css"); // Read only once
#endif
    const string Radius = "0.5rem";
    public static readonly bool NotSupportedBuild = Environment.OSVersion.Version.Build < 22523;
    public static readonly bool SupportAccent = Environment.OSVersion.Version.Major >= 10;
    bool DiscordEffectApplied = false;
    bool _Dark = true;
    bool Dark
    {
        set
        {
            _Dark = value;
            if (!IsExcessiveAccentColorEnabled) (Resources["Color"] as SolidColorBrush ?? throw new NullReferenceException()).Color = value ? Colors.White : Colors.Black;
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
        void RefreshDarkMode(bool dark) => CustomPInvoke.DwmApi.SetWindowAttribute(
            Handle,
            CustomPInvoke.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            dark ? 1 : 0 // Always dark as of now, or you can change to: ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark ? 1 : 0
        );
        static bool IsDarkBackground(Windows.UI.Color color)
        {
            return color.R + color.G + color.B < (255 * 3 - color.R - color.G - color.B);
        }
        RefreshDarkMode(dark: IsDarkBackground(UISettings.GetColorValue(UIColorType.Background)));
        SizeChanged += (_, _) => RefreshFrame();
        IsVisibleChanged += (_, _) => RefreshFrame();
        
        Width += 1;
        bool UserRequestOpenNewWindow = false;
        SetBackdrop((CustomPInvoke.BackdropType)Enum.Parse(typeof(CustomPInvoke.BackdropType), Settings.Default.BackdropType, ignoreCase: true));
        WebView.CoreWebView2InitializationCompleted += (_, _) =>
        {
            var CoreWebView2 = WebView.CoreWebView2;
            CoreWebView2.NewWindowRequested += (_, e) =>
            {
                if (UserRequestOpenNewWindow)
                {
                    UserRequestOpenNewWindow = false;
                    return;
                }
                System.Diagnostics.Process.Start("explorer", e.Uri);
                e.Handled = true;
            };
            
            CoreWebView2.ContextMenuRequested += (_, e) =>
            {
                if (e.ContextMenuTarget.HasLinkUri)
                {
                    var newItem = CoreWebView2.Environment.CreateContextMenuItem("Open In Default Browser", null, CoreWebView2ContextMenuItemKind.Command);
                    newItem.CustomItemSelected += (_, e2) =>
                    {
                        System.Diagnostics.Process.Start("explorer", e.ContextMenuTarget.LinkUri);
                    };
                    e.MenuItems.Insert(0, CoreWebView2.Environment.CreateContextMenuItem("", null, CoreWebView2ContextMenuItemKind.Separator));
                    e.MenuItems.Insert(0, newItem);

                    foreach (var menu in e.MenuItems)
                    {
                        if (menu.Label.ToLower().Replace("&","").Contains("new window")) menu.CustomItemSelected += (_, e) =>
                        {
                            UserRequestOpenNewWindow = true;
                        };
                    }
                }
            };
            var handle = WebView.Handle;
            _ = User32.SetWindowLong(handle, User32.WindowLongIndexFlags.GWL_EXSTYLE,
                (User32.SetWindowLongFlags)User32.GetWindowLong(handle, User32.WindowLongIndexFlags.GWL_EXSTYLE)
                | User32.SetWindowLongFlags.WS_CLIPCHILDREN
            );

            CoreWebView2.DocumentTitleChanged += (_, _) =>
            {
                var title = WebView.CoreWebView2.DocumentTitle;
                if (title == "Discord")
                {
                    title = "";
                    Title = "Mica Discord";
                } else Title = $"Mica Discord - {title}";
                WebsiteTitle.Text = title;
            };
            CoreWebView2.HistoryChanged += delegate
            {
                Back.IsEnabled = WebView.CanGoBack;
                Forward.IsEnabled = WebView.CanGoForward;
                RefreshFrame();
            };


            RefreshFrame();
        };
        WebView.NavigationCompleted += async delegate
        {

            if (!WebView.Source.OriginalString.Contains("discord.com")) return;
            DiscordEffectApplied = Settings.Default.ReplaceDiscordBackground;
            if (DiscordEffectApplied)
            {
                var Dark = (await WebView.CoreWebView2.ExecuteScriptAsync("document.getElementsByTagName('html')[0].classList.contains('theme-dark')")) == "true";
                var Light = (await WebView.CoreWebView2.ExecuteScriptAsync("document.getElementsByTagName('html')[0].classList.contains('theme-light')")) == "true";
                if (Dark is false && Light is false)
                    Dark = this.Dark; // Don't change
                Windows.UI.Color PrimaryColor = default, DisabledColor = default, HoverColor = default, Accent = default;
                if (SupportAccent)
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    PrimaryColor = UISettings.GetColorValue(UIColorType.AccentLight3);//CustomPInvoke.GetAccentColor(Dark ? AccentColorTypes.ImmersiveSaturatedHighlight : AccentColorTypes.ImmersiveSaturatedSelectionBackground);
                    DisabledColor = UISettings.GetColorValue(UIColorType.AccentLight2);//CustomPInvoke.GetAccentColor(AccentColorTypes.ImmersiveSaturatedCommandRowDisabled);
                    DisabledColor.A /= 2;
                    HoverColor = UISettings.GetColorValue(UIColorType.AccentLight2);
                    Accent = UISettings.GetColorValue(UIColorType.Accent);
#pragma warning restore CA1416 // Validate platform compatibility
                }

                if (IsExcessiveAccentColorEnabled) (Resources["Color"] as SolidColorBrush ?? throw new NullReferenceException()).Color = System.Windows.Media.Color.FromArgb(PrimaryColor.A, PrimaryColor.R, PrimaryColor.G, PrimaryColor.B);

                /*
                static double GetHue(int red, int green, int blue)
                {

                    float min = Math.Min(Math.Min(red, green), blue);
                    float max = Math.Max(Math.Max(red, green), blue);

                    if (min == max)
                    {
                        return 0;
                    }

                    float hue;
                    if (max == red) hue = (green - blue) / (max - min);
                    else if (max == green) hue = 2f + (blue - red) / (max - min);
                    else hue = 4f + (red - green) / (max - min);

                    hue *= 60;
                    if (hue < 0) hue += 360;

                    return hue;
                }
                */
                RefreshDarkMode(dark: Dark);
                this.Dark = Dark;
                var LightColorCSS = Dark && Settings.Default.ModeAwareCSS;
                var invc = LightColorCSS ? 250 : 0;
                var regcgray = LightColorCSS ? 50 : 200;
                var floating = Dark ? 0 : 255;
                var ErrorAccentColor = CustomPInvoke.UxTheme.GetAccentColor(AccentColorTypes.ImmersiveSaturatedInlineErrorText);
                await WebView.CoreWebView2.ExecuteScriptAsync(($@"
(function () {{
    let s = document.createElement('style');
    s.innerHTML = `
* {{ /* System Color */"+
(SupportAccent ?
@$"
    --sys-accent-prop: {Accent.R}, {Accent.G}, {Accent.B};
    --sys-accent-color: rgba({Accent.R}, {Accent.G}, {Accent.B}, {Accent.A});
    --sys-accent-light-3-color: rgba({PrimaryColor.R}, {PrimaryColor.G}, {PrimaryColor.B}, {PrimaryColor.A});
    --sys-accent-disabled-color: rgba({DisabledColor.R}, {DisabledColor.G}, {DisabledColor.B}, {DisabledColor.A});
    --sys-accent-disabled-color-half: rgba({DisabledColor.R}, {DisabledColor.G}, {DisabledColor.B}, {DisabledColor.A / 2});
    --sys-error-accent-color: rgba({ErrorAccentColor.R}, {ErrorAccentColor.G}, {ErrorAccentColor.B}, {ErrorAccentColor.A});
    --sys-hover-accent-color: rgba({HoverColor.R}, {HoverColor.G}, {HoverColor.B}, {HoverColor.A});
" : "") + $@"
    --sys-light-color-bg: {(LightColorCSS ? 1 : 0)};
    --sys-layering-strength: {invc};
    --sys-floating-strength: {floating};
    --sys-normal-strength: {regcgray};
    --sys-border-radius: {Radius};
}}
* {{
{(
    IsExcessiveAccentColorEnabled ? @"
    --interactive-active: var(--sys-accent-light-3-color);
    --interactive-normal: var(--sys-accent-light-3-color);
    --interactive-hover: var(--sys-hover-accent-color);
    --interactive-disabled: var(--sys-accent-disabled-color-half);
    --text-muted: var(--sys-accent-disabled-color-half);
    --channels-default: var(--sys-accent-disabled-color);
" : ""
)}
    
}}
" + DefinedCSS + @"
`.trim();
    document.head.appendChild(s);
})()
").Trim());
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
                default:
                    break;
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
    }
    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        SettingsDialog.ResetRequiresReload();
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
    public static SolidColorBrush GetColorFromHex(string hexaColor)
    {
        return new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(
            Convert.ToByte(hexaColor[1..2], 16),
            Convert.ToByte(hexaColor[3..2], 16),
            Convert.ToByte(hexaColor[5..2], 16),
            Convert.ToByte(hexaColor[7..2], 16)
        ));
    }
    public static System.Windows.Media.Color GetColorFromUInt(uint value)
        => System.Windows.Media.Color.FromArgb(
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)(value)
        );
}
public partial class MainWindow : Window
{
    public bool ForceClose { get; set; } = false;
    public MainWindow()
    {
        AllowsTransparency = false;
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
            SetBackdrop((CustomPInvoke.BackdropType)Enum.Parse(typeof(CustomPInvoke.BackdropType), Settings.Default.BackdropType, ignoreCase: true));
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
            var a = WindowState == WindowState.Maximized ? 7.5 : 0;
            TitleBar.Margin = new Thickness(a, a, 0, 0);
            WebView.Margin = new Thickness(7.5, 0, 7.5, 7.5);
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
        UseAeroCaptionButtons = true,
        ResizeBorderThickness = new Thickness(7.5)
    };
    public static ImageSource ImageSourceFromBitmap(Bitmap bmp)
    {
        var handle = bmp.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        finally { Gdi32.DeleteObject(handle); }
    }

    void SetBackdrop(CustomPInvoke.BackdropType BackdropType) => SetBackdrop((int)BackdropType);
    void SetBackdrop(int BackdropType)
    {
        CustomPInvoke.DwmApi.SetWindowAttribute(
            Handle,
            CustomPInvoke.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            BackdropType);
    }

    public WindowInteropHelper WindowInteropHelper { get; }
    public IntPtr Handle => WindowInteropHelper.Handle;
    
}
