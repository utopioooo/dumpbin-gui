using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using DumpbinGui.Core;
namespace DumpbinGui.Infrastructure;

public sealed class ToolProvider : IToolProvider
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };
    public async Task<string?> ResolveAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        string directory = Path.Combine(SettingsStore.Root, "tools");
        string executable = Path.Combine(directory, "dumpbin.exe");
        if (File.Exists(executable)) return executable;
        try
        {
            progress.Report("Downloading Microsoft tools / 正在下载微软工具…");
            using var channel = JsonDocument.Parse(await Http.GetStringAsync("https://aka.ms/vs/stable/channel", cancellationToken));
            var manifestItem = channel.RootElement.GetProperty("channelItems").EnumerateArray()
                .First(x => x.GetProperty("id").GetString() == "Microsoft.VisualStudio.Manifests.VisualStudio");
            using var manifest = JsonDocument.Parse(await Http.GetStringAsync(manifestItem.GetProperty("payloads")[0].GetProperty("url").GetString()!, cancellationToken));
            var package = manifest.RootElement.GetProperty("packages").EnumerateArray().Last(x =>
                Regex.IsMatch(x.GetProperty("id").GetString()!, @"^Microsoft\.VC\.[\d.]+\.Tools\.HostX64\.TargetX64\.base$"));
            var payload = package.GetProperty("payloads")[0];
            byte[] bytes = await Http.GetByteArrayAsync(payload.GetProperty("url").GetString()!, cancellationToken);
            if (payload.TryGetProperty("sha256", out var hash) && !Convert.ToHexString(SHA256.HashData(bytes)).Equals(hash.GetString(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Download checksum mismatch");
            Directory.CreateDirectory(directory);
            using var archive = new ZipArchive(new MemoryStream(bytes));
            foreach (var entry in archive.Entries.Where(x => x.FullName.Contains("/bin/Hostx64/x64/", StringComparison.OrdinalIgnoreCase) && x.Name.Length > 0))
            {
                cancellationToken.ThrowIfCancellationRequested();
                entry.ExtractToFile(Path.Combine(directory, entry.Name), true);
            }
            if (File.Exists(executable)) return executable;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            progress.Report("Download failed; searching locally / 下载失败，正在本地查找: " + ex.Message);
        }
        foreach (string folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            string candidate = Path.Combine(folder.Trim('"'), "dumpbin.exe");
            if (File.Exists(candidate)) return candidate;
        }
        string vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (!File.Exists(vswhere)) return null;
        var start = new ProcessStartInfo(vswhere) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (string argument in new[] { "-products", "*", "-find", @"VC\Tools\MSVC\**\bin\Hostx64\x64\dumpbin.exe" }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).LastOrDefault(File.Exists);
    }
}
