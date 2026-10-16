using DumpbinGui.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DumpbinGui.Core;
namespace DumpbinGui.App;
public partial class MainViewModel : ObservableObject {
 readonly ISettingsStore store; readonly IDumpbinRunner runner; readonly IFileInspector inspector; readonly IToolProvider tools; readonly AppSettings settings;
 public MainViewModel(ISettingsStore store, IDumpbinRunner runner, IFileInspector inspector, IToolProvider tools) { this.store=store; this.runner=runner; this.inspector=inspector; this.tools=tools; settings=store.Load(); toolPath=settings.ToolPath; darkMode=settings.DarkMode; english=settings.Language=="en-US"; Theme.Apply(darkMode); Localization.Current.SetLanguage(settings.Language); }
 [ObservableProperty] private IReadOnlyList<ResultCategory> categories=[];
 [ObservableProperty] private ResultCategory? selectedCategory;
 [ObservableProperty] private string filePath="";
 [ObservableProperty] private string toolPath="";
 [ObservableProperty] private string output="";
 [ObservableProperty] private string status="";
 [ObservableProperty] private string fileKind="";
 [ObservableProperty] private bool english;
 [ObservableProperty] private bool darkMode;
 [ObservableProperty] private bool settingsOpen;
 [ObservableProperty] private bool isBusy;
 [ObservableProperty] private IReadOnlyList<SectionInfo> sections=[];
 [ObservableProperty] private IReadOnlyList<ArchiveMember> members=[];
 [ObservableProperty] private IReadOnlyList<ResourceInfo> resources=[];
 partial void OnDarkModeChanged(bool value) { Theme.Apply(value); settings.DarkMode=value; store.Save(settings); }
 partial void OnEnglishChanged(bool value) { settings.Language=value?"en-US":"zh-CN"; Localization.Current.SetLanguage(settings.Language); store.Save(settings); }
 partial void OnToolPathChanged(string value) { settings.ToolPath=value; store.Save(settings); }
 [RelayCommand] void ToggleSettings()=>SettingsOpen=!SettingsOpen;
 public byte[] ReadBytes(int offset,int size)=>inspector.ReadBytes(FilePath,offset,size);
 public async Task PrepareAsync(CancellationToken token) { ToolPath=await tools.ResolveAsync(new Progress<string>(s=>Status=s),token)??""; }
 [RelayCommand(IncludeCancelCommand=true)] private async Task AnalyzeAsync(CancellationToken token) {
 IsBusy=true; Categories=[]; SelectedCategory=null; Output=""; Sections=[]; Resources=[]; Members=[]; FileKind="";
 try {
 string path=FilePath;
 if(!System.IO.File.Exists(path)) throw new System.IO.FileNotFoundException(English?"Select an input file":"请选择有效的输入文件");
 Status=English?"Inspecting file…":"正在识别文件…";
 try { var result=await Task.Run(()=>inspector.Inspect(path),token); Sections=result.Sections; Resources=result.Resources; Members=result.Members; FileKind=result.Kind; } catch(Exception ex) when(ex is not OperationCanceledException) { FileKind=ex.Message; }
 if(!System.IO.File.Exists(ToolPath)) { Status=English?"Configure dumpbin in Settings":"请在设置中配置 dumpbin 工具"; SettingsOpen=true; return; }
 Status=English?"Analyzing all information…":"正在分析全部信息…";
 var watch=System.Diagnostics.Stopwatch.StartNew();
 Output=await runner.RunAsync(ToolPath,path,"/ALL",token);
 Categories=await Task.Run(()=>OutputParser.Parse(Output),token); SelectedCategory=Categories.FirstOrDefault(c=>c.Rows.Count>0)??Categories.FirstOrDefault();
 Status=(English?"Analysis complete · ":"分析完成 · ")+watch.Elapsed.ToString(@"mm\:ss\.fff");
 } catch(OperationCanceledException) { Status=English?"Cancelled":"已取消"; } catch(Exception ex) { Status=ex.Message; } finally { IsBusy=false; }
 }
}
