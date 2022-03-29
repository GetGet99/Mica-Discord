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
using AccentColorTypes = MicaDiscord.CustomPInvoke.AccentColorTypes;
namespace MicaDiscord;

public partial class MainWindow : Window
{
    const bool IsAccentColorEnabled = false;
    const string Radius = "0.5rem";
    public static readonly bool NotSupportedBuild = Environment.OSVersion.Version.Build < 22523;
    bool DiscordEffectApplied = false;
    bool _Dark = true;
    bool Dark
    {
        set
        {
            _Dark = value;
            if (!IsAccentColorEnabled) (Resources["Color"] as SolidColorBrush ?? throw new NullReferenceException()).Color = value ? Colors.White : Colors.Black;
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

                var PrimaryColor = CustomPInvoke.GetAccentColor(Dark ? AccentColorTypes.ImmersiveSaturatedHighlight : AccentColorTypes.ImmersiveSaturatedSelectionBackground);
                var DisabledColor = CustomPInvoke.GetAccentColor(AccentColorTypes.ImmersiveSaturatedCommandRowDisabled);
                var HoverColor = CustomPInvoke.GetAccentColor(AccentColorTypes.ImmersiveSaturatedCommandRowHover);
                if (IsAccentColorEnabled) (Resources["Color"] as SolidColorBrush ?? throw new NullReferenceException()).Color = PrimaryColor;
                
                RefreshDarkMode(dark: Dark);
                this.Dark = Dark;
                var LightColorCSS = Dark && Settings.Default.ModeAwareCSS;
                var invc = LightColorCSS ? 250 : 0;
                var regc = LightColorCSS ? 0 : 255;
                var regcgray = LightColorCSS ? 50 : 200;
                var floating = Dark ? 0 : 255;
                var ErrorAccentColor = CustomPInvoke.GetAccentColor(AccentColorTypes.ImmersiveSaturatedInlineErrorText);
                await WebView.CoreWebView2.ExecuteScriptAsync($@"
(function () {{
    let s = document.createElement('style');
    s.innerHTML = `
.theme-{(Dark ? "dark" : "light")} {{
    --background-layering: rgba({invc},{invc},{invc},0.05);
    --background-layering-half: rgba({invc},{invc},{invc},0.025);
    --background-primary: var(--background-layering);
    --background-secondary: transparent;
    --background-secondary-alt: rgba({invc},{invc},{invc},0.075);
    --background-tertiary: transparent;
    --background-message-hover: rgba({invc},{invc},{invc},0.07);
    --background-floating: rgba({floating},{floating},{floating},0.75);
    --deprecated-store-bg: rgba({invc},{invc},{invc},0.05);
    --channeltextarea-background: var(--background-layering);
    --scrollbar-auto-track: transparent;
    --scrollbar-thin-track: #0000;
    --scrollbar-thin-thumb: rgba({invc},{invc},{invc},0.25);
    --scrollbar-auto-thumb: rgba({invc},{invc},{invc},0.25);
    --background-modifier-hover: var(--background-layering-half);
    --background-modifier-selected: var(--background-layering);
    --background-mentioned-hover: hsla(38,calc(var(--saturation-factor, 1)*95.7%),54.1%,{0.08 + (LightColorCSS ? 0.1 : 0)});
    --win-accent-color: rgba({PrimaryColor.R}, {PrimaryColor.G}, {PrimaryColor.B}, {PrimaryColor.A});
    --win-accent-disabled-color: rgba({DisabledColor.R}, {DisabledColor.G}, {DisabledColor.B}, {DisabledColor.A});
    --win-accent-disabled-color-half: rgba({DisabledColor.R}, {DisabledColor.G}, {DisabledColor.B}, {DisabledColor.A / 2});
    --win-error-accent-color: rgba({ErrorAccentColor.R}, {ErrorAccentColor.G}, {ErrorAccentColor.B}, {ErrorAccentColor.A});
    --win-hover-accent-color: rgba({HoverColor.R}, {HoverColor.G}, {HoverColor.B}, {HoverColor.A});
    --text-link: var(--win-accent-color);
{(
    IsAccentColorEnabled ? @"
    --interactive-active: var(--win-accent-color);
    --interactive-normal: var(--win-accent-color);
    --interactive-hover: var(--win-hover-accent-color);
    --interactive-disabled: var(--win-accent-disabled-color-half);
    --text-muted: var(--win-accent-disabled-color-half);
    --channels-default: var(--win-accent-disabled-color);
" : ""
)}
    
}}
.content-3spvdd {{
    --background-primary: rgb({regcgray},{regcgray},{regcgray});
}}
code, article {{
    --background-secondary: rgba({invc},{invc},{invc},0.05);
}}
.lookFilled-yCfaCM.colorPrimary-2AuQVo, .lookFilled-yCfaCM.colorGrey-2iAG-B {{
    background-color: rgba({invc},{invc},{invc},0.05) !important;
}}
.content-2a4AW9 {{
    --background-secondary: var(--background-layering-half);
    border-color: black;
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
    background-color: var(--background-layering-half) !important;
    border-color: black;
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
.message-2CShn3 {{
    border-radius: {Radius};
}}
.sidebar-1tnWFu {{
border-radius: {Radius} {Radius} 0px 0px;
}}
.popout-TdhJ6Z {{
    --background-tertiary: rgba({floating},{floating},{floating},0.75);
}}
.popout-1KHNAq {{
    --background-secondary: rgba({floating},{floating},{floating},0.75);
}}
.lookFilled-1GseHa.select-1Ia3hD {{
    --background-secondary: rgba({invc},{invc},{invc},0.05);
    --background-tertiary: rgba({invc},{invc},{invc},0.75);
}}
.unread-36eUEm, .item-2LIpTv {{
    border-radius: 4px;
}}
.unread-36eUEm /* Unread Text Channels */
{{
    background-color: var(--win-accent-color);
}}
.item-2LIpTv {{
    margin: 0px 0px 0px 2px;
    width: 4px;
    background: var(--win-accent-color);
}}
.gradientContainer-phMG8d /* In-Call Gradient effect on top and bottom */
{{
    height: 40px;
    background-image: linear-gradient(rgba({invc},{invc},{invc},0.05),transparent);
}}
.modeSelected-3DmyhH::before /* Selected Text Channel */
{{
    content: """";
    position: absolute;
    height: 18px;
    width: 4px;
    border-radius: 4px;
    top: 50%;
    left: 0;
    margin-top: -9px;
    background: var(--win-accent-color);
}}
.modeUnread-3Cxepe .channelName-3KPsGw {{
    font-weight: 600;
}}
.hljs-comment {{
    color: seagreen !important;
}}
.layer-2aCOJ3 /* Thread Menu */
{{
    background-color: var(--background-floating);
    border-radius: {Radius}
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
        
        //TitleBarUI.Margin = new Thickness(0, 0, (SystemParameters.WindowCaptionButtonWidth+10) * 3, 0);
        //this.TitleBar.Height = TitleBar.Height;
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
