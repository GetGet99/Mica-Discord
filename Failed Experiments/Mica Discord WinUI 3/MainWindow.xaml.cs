using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Composition;
using WinRT;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Mica_Discord_WinUI_3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();
            var m_configurationSource = new SystemBackdropConfiguration();
            DesktopAcrylicController Backdrop = new();
            Backdrop.TintOpacity = 0;
            Backdrop.LuminosityOpacity = 0;

            Backdrop.TintColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            Backdrop.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            Backdrop.SetSystemBackdropConfiguration(m_configurationSource);
        }

    }
    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                _ = CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }
}
