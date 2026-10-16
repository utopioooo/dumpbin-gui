using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace DumpbinGui.App;

internal static class WindowBounds
{
    public static void Attach(Window window)
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
        source?.AddHook(HandleMessage);
    }

    private static IntPtr HandleMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int GetMinMaxInfo = 0x0024;
        if (message != GetMinMaxInfo) return IntPtr.Zero;
        var monitor = MonitorFromWindow(hwnd, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return IntPtr.Zero;
        var bounds = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        bounds.MaxPosition.X = info.Work.Left - info.Monitor.Left;
        bounds.MaxPosition.Y = info.Work.Top - info.Monitor.Top;
        bounds.MaxSize.X = info.Work.Right - info.Work.Left;
        bounds.MaxSize.Y = info.Work.Bottom - info.Work.Top;
        Marshal.StructureToPtr(bounds, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MinMaxInfo
    {
        public Point Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize;
    }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor, Work;
        public int Flags;
    }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
