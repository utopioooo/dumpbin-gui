using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Text;
using Microsoft.Win32;
using DumpbinGui.Core;
using DumpbinGui.Infrastructure;
namespace DumpbinGui.App;
public partial class MainWindow : Window {
 readonly MainViewModel vm;
 void MinimizeWindow(object sender,RoutedEventArgs e)=>SystemCommands.MinimizeWindow(this);
 void ToggleMaximize(object sender,RoutedEventArgs e) {
 if(WindowState==WindowState.Maximized) SystemCommands.RestoreWindow(this);
 else SystemCommands.MaximizeWindow(this);
 }
 void CloseWindow(object sender,RoutedEventArgs e)=>SystemCommands.CloseWindow(this);
 async Task LoadFileAsync(string path) {
 vm.AnalyzeCommand.Cancel();
 if(vm.AnalyzeCommand.ExecutionTask is {} previous) { try { await previous; } catch(OperationCanceledException) {} }
 vm.FilePath=path;
 if(!File.Exists(vm.ToolPath)) {
 try { await vm.PrepareAsync(CancellationToken.None); if(!File.Exists(vm.ToolPath)) BrowseTool(this,new RoutedEventArgs()); } catch(Exception ex) { vm.Status=ex.Message; }
 }
 await vm.AnalyzeCommand.ExecuteAsync(null);
 }
 public MainWindow() { InitializeComponent(); vm=new(new SettingsStore(),new DumpbinRunner(),new FileInspector(),new ToolProvider()); DataContext=vm; vm.PropertyChanged+=(_,args)=> { if(args.PropertyName==nameof(vm.Members)) { MembersTab.Visibility=vm.Members.Count>0?Visibility.Visible:Visibility.Collapsed; if(vm.Members.Count>0) MembersTab.IsSelected=true; else AnalysisGrid.Focus(); } }; MembersTab.Visibility=Visibility.Collapsed; SourceInitialized+=(_,_)=> { WindowBounds.Attach(this); Theme.ApplyTitleBar(this,vm.DarkMode); }; ConfigureResultMenus(); }
 void ConfigureResultMenus() {
 var export=new MenuItem();
 export.Click+=(_,_)=>ExportCategory();
 CategoriesList.ContextMenu=new ContextMenu(); CategoriesList.ContextMenu.Items.Add(export);
 CategoriesList.ContextMenuOpening+=(_,_)=>export.Header=vm.English?"Export category…":"导出该分区…";
 CategoriesList.PreviewMouseRightButtonDown+=(_,e)=> {
 if(ItemsControl.ContainerFromElement(CategoriesList,e.OriginalSource as DependencyObject) is ListBoxItem item) item.IsSelected=true;
 };
 foreach(var grid in new[]{AnalysisGrid,SectionsGrid,ResourcesGrid,MembersGrid}) {
 grid.AutoGeneratingColumn+=(_,args)=> {
 if(grid.ItemsSource is null) { args.Cancel=true; return; }
 bool hasData=grid.ItemsSource.Cast<object>().Any(row=> {
 if(row is DataRowView view) return view.Row.Table.Columns.Contains(args.PropertyName) && ResultValidation.HasValue(view[args.PropertyName]);
 return ResultValidation.HasValue(System.ComponentModel.TypeDescriptor.GetProperties(row)[args.PropertyName]?.GetValue(row));
 });
 args.Cancel=!hasData;
 };
 var selected=new MenuItem(); var all=new MenuItem();
 selected.Click+=(_,_)=>CopyGrid(grid,false); all.Click+=(_,_)=>CopyGrid(grid,true);
 grid.ContextMenu=new ContextMenu(); grid.ContextMenu.Items.Add(selected); grid.ContextMenu.Items.Add(all);
 grid.ContextMenuOpening+=(_,_)=> { selected.Header=vm.English?"Copy selected rows":"复制选中行"; all.Header=vm.English?"Copy all rows":"复制全部"; selected.IsEnabled=grid.SelectedItems.Count>0; all.IsEnabled=grid.Items.Count>0; };
 grid.PreviewMouseRightButtonDown+=(_,e)=> {
 if(ItemsControl.ContainerFromElement(grid,e.OriginalSource as DependencyObject) is DataGridRow row && !row.IsSelected) { grid.SelectedItems.Clear(); row.IsSelected=true; }
 };
 }
 }
 static string Cell(object? value)=>Convert.ToString(value)?.Replace("\t"," ").Replace("\r"," ").Replace("\n"," ")??"";
 void CopyGrid(DataGrid grid,bool all) {
 try {
 var columns=grid.Columns.Where(c=>c.Visibility==Visibility.Visible).OrderBy(c=>c.DisplayIndex).ToArray();
 var text=new StringBuilder(); text.AppendLine(string.Join("\t",columns.Select(c=>Cell(c.Header))));
 var rows=all?grid.Items.Cast<object>():grid.SelectedItems.Cast<object>();
 foreach(var row in rows) {
 text.AppendLine(string.Join("\t",columns.Select(c=> {
 if(c is not DataGridBoundColumn bound || bound.Binding is not System.Windows.Data.Binding binding) return "";
 string path=binding.Path?.Path??"";
 if(row is DataRowView view) return Cell(view.Row.Table.Columns.Contains(path)?view[path]:null);
 return Cell(row.GetType().GetProperty(path)?.GetValue(row));
 })));
 }
 Clipboard.SetText(text.ToString());
 } catch(Exception ex) { vm.Status=ex.Message; }
 }
 void ExportCategory() {
 if(CategoriesList.SelectedItem is not ResultCategory category) return;
 var name=string.Join("_",category.Name.Split(Path.GetInvalidFileNameChars()));
 var dialog=new SaveFileDialog { FileName=name+".csv",Filter="CSV|*.csv" };
 if(dialog.ShowDialog()!=true) return;
 try {
 string Quote(object? value)=>"\""+(Convert.ToString(value)??"").Replace("\"","\"\"")+"\"";
 var table=category.Rows.Table!; var text=new StringBuilder();
 text.AppendLine(string.Join(",",table.Columns.Cast<DataColumn>().Select(c=>Quote(c.ColumnName))));
 foreach(DataRowView row in category.Rows) text.AppendLine(string.Join(",",row.Row.ItemArray.Select(Quote)));
 File.WriteAllText(dialog.FileName,text.ToString(),new UTF8Encoding(true));
 } catch(Exception ex) { vm.Status=ex.Message; }
 }
 async void BrowseFile(object sender,RoutedEventArgs e) { var d=new OpenFileDialog {Filter="Binaries|*.exe;*.dll;*.lib;*.obj;*.sys;*.ocx|All files|*.*"}; if(d.ShowDialog()==true) { await LoadFileAsync(d.FileName); } }
 void BrowseTool(object sender,RoutedEventArgs e) { var d=new OpenFileDialog {Filter="dumpbin.exe|dumpbin.exe|Executables|*.exe"}; if(d.ShowDialog()==true) vm.ToolPath=d.FileName; }
 async void OnDrop(object sender,DragEventArgs e) { if(e.Data.GetData(DataFormats.FileDrop) is string[] {Length:>0} paths) { await LoadFileAsync(paths[0]); } }
 async void PrepareTool(object sender,RoutedEventArgs e) {
 if(MessageBox.Show(vm.English?"Download Microsoft VC tools under the Visual Studio license terms? https://visualstudio.microsoft.com/license-terms/":"下载微软 VC 工具前，请确认接受 Visual Studio 许可条款：https://visualstudio.microsoft.com/license-terms/", "Microsoft Visual Studio",MessageBoxButton.OKCancel)!=MessageBoxResult.OK) return;
 PrepareButton.IsEnabled=false;
 try { await vm.PrepareAsync(CancellationToken.None); if(string.IsNullOrEmpty(vm.ToolPath)) BrowseTool(sender,e); if(File.Exists(vm.FilePath) && File.Exists(vm.ToolPath)) await vm.AnalyzeCommand.ExecuteAsync(null); } catch(Exception ex) { vm.Status=ex.Message; } finally { PrepareButton.IsEnabled=true; }
 }
 void SaveOutput(object sender,RoutedEventArgs e) { var d=new SaveFileDialog {Filter="Text|*.txt"}; if(d.ShowDialog()==true) { try { File.WriteAllText(d.FileName,vm.Output); } catch(Exception ex) { vm.Status=ex.Message; } } }
 void FindOutput(object sender,RoutedEventArgs e) { if(SearchText.Text.Length==0) return; int start=OutputBox.SelectionStart+OutputBox.SelectionLength; int index=vm.Output.IndexOf(SearchText.Text,Math.Min(start,vm.Output.Length),StringComparison.OrdinalIgnoreCase); if(index<0) index=vm.Output.IndexOf(SearchText.Text,StringComparison.OrdinalIgnoreCase); if(index>=0) { OutputBox.Focus(); OutputBox.Select(index,SearchText.Text.Length); OutputBox.ScrollToLine(OutputBox.GetLineIndexFromCharacterIndex(index)); } }
 void ExportSection(object sender,RoutedEventArgs e) { if(SectionsGrid.SelectedItem is SectionInfo s) Export(s.Name+".bin",s.FileOffset,s.Size); }
 void ExportResource(object sender,RoutedEventArgs e) { if(ResourcesGrid.SelectedItem is ResourceInfo r) Export("resource.bin",r.FileOffset,r.Size); }
 void Export(string name,int offset,int size) { var d=new SaveFileDialog {FileName=name,Filter="Binary|*.bin|All files|*.*"}; if(d.ShowDialog()==true) { try { File.WriteAllBytes(d.FileName,vm.ReadBytes(offset,size)); } catch(Exception ex) { vm.Status=ex.Message; } } }
}
