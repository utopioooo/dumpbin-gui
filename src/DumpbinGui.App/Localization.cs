using System.ComponentModel;
using System.Globalization;
using System.Resources;
namespace DumpbinGui.App;
public sealed class Localization : INotifyPropertyChanged {
 public static Localization Current {get;}=new();
 bool english;
 readonly Dictionary<string,(string Zh,string En)> words=new() {
 ["Settings"]=("设置","Settings"),["Subtitle"]=("二进制文件分析","Binary file inspection"),["DropHint"]=("拖入文件，即刻分析全部信息","Drop a binary to analyze everything"),["Appearance"]=("外观","Appearance"),["DarkMode"]=("深色模式","Dark mode"),["Browse"]=("浏览","Browse"),["Tool"]=("工具路径","Tool path"),["Run"]=("运行","Run"),["Cancel"]=("取消","Cancel"),["Output"]=("输出","Output"),["Sections"]=("节区","Sections"),["Resources"]=("资源","Resources"),["Prepare"]=("获取工具","Get tools"),["Save"]=("保存输出","Save output"),["Export"]=("导出选中数据","Export selected data"),["Search"]=("查找","Find") };
 public string this[string key]=>words.TryGetValue(key,out var value)?(english?value.En:value.Zh):key;
 public void SetLanguage(string language) { english=language=="en-US"; PropertyChanged?.Invoke(this,new PropertyChangedEventArgs("Item[]")); }
 public event PropertyChangedEventHandler? PropertyChanged;
}
