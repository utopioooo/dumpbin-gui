using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;
namespace DumpbinGui.App;
public static class Theme {
 [DllImport("dwmapi.dll")]
 static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
 public static void ApplyTitleBar(Window window, bool dark) {
 var handle=new WindowInteropHelper(window).Handle;
 if(handle==IntPtr.Zero) return;
 int enabled=dark?1:0;
 DwmSetWindowAttribute(handle,20,ref enabled,sizeof(int));
 int caption=dark?0x00101010:0x00FCF6F2;
 int text=dark?0x00EBF0F4:0x00402C20;
 DwmSetWindowAttribute(handle,35,ref caption,sizeof(int));
 DwmSetWindowAttribute(handle,36,ref text,sizeof(int));
 }
 public static void Apply(bool dark) {
 var resources=Application.Current.Resources;
 void Brush(string key,string color)=>resources[key]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
 Brush("Background",dark?"#101010":"#F2F6FC"); Brush("Surface",dark?"#191919":"#FFFFFF"); Brush("Foreground",dark?"#F4F0EB":"#202C40"); Brush("Border",dark?"#33302C":"#DCE2EC"); Brush("Hover",dark?"#38291C":"#E7EEFA"); Brush("Accent",dark?"#FF982F":"#245DC1");
 Brush("ScrollThumb",dark?"#665A4F":"#ADBBD0");
 foreach(Window window in Application.Current.Windows) ApplyTitleBar(window,dark);
 }
}
