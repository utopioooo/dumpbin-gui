namespace DumpbinGui.Core;
public record AnalysisOption(string Name, string Argument) { public override string ToString() => Name; }
public record SectionInfo(string Name, int VirtualAddress, int VirtualSize, int FileOffset, int Size, string Characteristics);
public record ResourceInfo(string Path, int FileOffset, int Size);
public record ArchiveMember(string Name, long FileOffset, long Size);
public record FileInspection(string Kind, IReadOnlyList<SectionInfo> Sections, IReadOnlyList<ResourceInfo> Resources) {
 public IReadOnlyList<ArchiveMember> Members { get; init; } = [];
}
public interface IFileInspector { FileInspection Inspect(string path); byte[] ReadBytes(string path, int offset, int size); }
public interface IDumpbinRunner { Task<string> RunAsync(string tool, string file, string arguments, CancellationToken cancellationToken); }
public interface IToolProvider { Task<string?> ResolveAsync(IProgress<string> progress, CancellationToken cancellationToken); }
public class AppSettings { public string ToolPath { get; set; } = ""; public bool DarkMode { get; set; } = true; public string Language { get; set; } = "zh-CN"; }
public interface ISettingsStore { AppSettings Load(); void Save(AppSettings settings); }
