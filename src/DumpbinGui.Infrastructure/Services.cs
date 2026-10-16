using System.Diagnostics;
using System.Text.Json;
using DumpbinGui.Core;
namespace DumpbinGui.Infrastructure;
public sealed class SettingsStore : ISettingsStore {
 public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DumpbinGui");
 public AppSettings Load() { try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Path.Combine(Root,"settings.json"))) ?? new(); } catch { return new(); } }
 public void Save(AppSettings settings) { Directory.CreateDirectory(Root); File.WriteAllText(Path.Combine(Root,"settings.json"),JsonSerializer.Serialize(settings)); }
}
public sealed class DumpbinRunner : IDumpbinRunner {
 public async Task<string> RunAsync(string tool,string file,string arguments,CancellationToken cancellationToken) {
 var start = new ProcessStartInfo(tool) { UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true };
 foreach(var argument in CommandArguments.Parse(arguments)) start.ArgumentList.Add(argument);
 start.ArgumentList.Add(file);
 using var process = Process.Start(start) ?? throw new IOException("Cannot start dumpbin");
 using var registration = cancellationToken.Register(() => { try { process.Kill(true); } catch (InvalidOperationException) {} });
 var stdout=process.StandardOutput.ReadToEndAsync(); var stderr=process.StandardError.ReadToEndAsync();
 await process.WaitForExitAsync(cancellationToken);
 return await stdout + Environment.NewLine + await stderr + $"\nExit code: {process.ExitCode}";
 }
}
