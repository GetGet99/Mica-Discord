using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using CSharpUI;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
namespace MicaDiscord;

partial class MainPage : Page
{
    ApplicationViewTitleBar AppTitleBar { get; } = ApplicationView.GetForCurrentView().TitleBar;
    public MainPage()
    {
        BackdropMaterial.SetApplyToRootOrPageBackground(this, true);
        AppTitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
        CoreTitleBar.ExtendViewIntoTitleBar = true;

        InitializeUI();
        CoreTitleBar.LayoutMetricsChanged += delegate
        {
            TitleBar.Height = CoreTitleBar.Height;
            Window.Current.SetTitleBar(TitleBarDragable);
        };

    }
}
partial class MainPage
{
    static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);
    Button Back, Forward, Reload, Setting;
    Grid TitleBarDragable;
    //static readonly SolidColorBrush TestBrush = new(Colors.Red);
    Border TitleBar;
    ColumnGrid TitleBarUI;
    TextBlock WebsiteTitle, TitleBarText;
#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
    WebView2 WebView;
#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
    void InitializeUI()
    {
        Content = 
        new RowGrid()
        .AddChild(GridType.Auto,
            new Border
            {
                Child = new ColumnGrid()
                .AddChild(GridType.Auto,
                    new TextBlock
                    {
                        Margin = new Thickness(10),
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = "Mica Discord"
                    }
                    .SetToVariable(out TitleBarText)
                )
                .AddChild(GridType.Auto,
                    new Button
                    {
                        Width = 50,
                        Height = 50,
                        Background = TransparentBrush,
                        BorderBrush = TransparentBrush,
                        VerticalAlignment = VerticalAlignment.Center,
                        Content = new SymbolIcon(Symbol.Back)
                    }
                    .SetToVariable(out Back)
                )
                .AddChild(GridType.Auto,
                    new Button
                    {
                        Width = 50,
                        Height = 50,
                        Background = TransparentBrush,
                        BorderBrush = TransparentBrush,
                        VerticalAlignment = VerticalAlignment.Center,
                        Content = new SymbolIcon(Symbol.Forward)
                    }
                    .SetToVariable(out Forward)
                )
                .AddChild(GridType.Auto,
                    new Button
                    {
                        Width = 50,
                        Height = 50,
                        Background = TransparentBrush,
                        BorderBrush = TransparentBrush,
                        VerticalAlignment = VerticalAlignment.Center,
                        Content = new SymbolIcon(Symbol.Refresh)
                    }
                    .SetToVariable(out Reload)
                )
                .AddChild(GridType.Auto,
                    new Button
                    {
                        Width = 50,
                        Height = 50,
                        Background = TransparentBrush,
                        BorderBrush = TransparentBrush,
                        VerticalAlignment = VerticalAlignment.Center,
                        Content = new SymbolIcon(Symbol.Setting)
                    }
                    .SetToVariable(out Setting)
                )
                .AddChild(GridType.Star,
                    new Grid
                    {
                        Background = TransparentBrush,
                    }
                    .AddChild(
                        new TextBlock
                        {
                            Margin = new Thickness(10),
                            VerticalAlignment = VerticalAlignment.Center
                        }
                        .SetToVariable(out WebsiteTitle)
                    )
                    .SetToVariable(out TitleBarDragable)
                )
                .SetToVariable(out TitleBarUI)
            }
            .Edit(x => Grid.SetRow(x, 0))
            .SetToVariable(out TitleBar)
        )
        .AddChild(GridType.Star,
#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
            new WebView2
            {
                Source = new Uri("https://discord.com/channels/@me"),
                VerticalAlignment = VerticalAlignment.Stretch
            }
            .Edit(x => Grid.SetRow(x, 1))
#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
            .SetToVariable(out WebView)
        );
    }
}
