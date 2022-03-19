using System;
using System.Runtime.InteropServices;

namespace MicaDiscord;

public enum BackdropType
{
    Regular = 1,
    Mica = 2, // Mica
    Acrylic = 3, // Acrylic
    Tabbed = 4 // Tabbed
}
public class CustomPInvoke
{
    [Flags]
    enum DWM_SYSTEMBACKDROP_TYPE
    {
        DWMSBT_MAINWINDOW = 2, // Mica
        DWMSBT_TRANSIENTWINDOW = 3, // Acrylic
        DWMSBT_TABBEDWINDOW = 4 // Tabbed
    }


    [Flags]
    public enum DWMWINDOWATTRIBUTE
    {
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        DWMWA_SYSTEMBACKDROP_TYPE = 38
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);


    public static int SetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, int parameter)
        => DwmSetWindowAttribute(hwnd, attribute, ref parameter, Marshal.SizeOf<int>());
}
