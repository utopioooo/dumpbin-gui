# Dumpbin GUI V0.1

Windows WPF application using .NET 10 and CommunityToolkit.Mvvm.

## Run

```powershell
dotnet run --project src/DumpbinGui.App
```

Use Get tools to download the x64 Microsoft VC tools from the Visual Studio stable manifest. On download failure the application searches PATH and Visual Studio installations using vswhere, then opens a file picker. A saved tool path is reused. Downloaded files and settings are stored under `%LOCALAPPDATA%\DumpbinGui`.

Select or drop an input binary. All file extensions can be selected; dumpbin determines command compatibility. Static and import libraries share the COFF archive format. Opening or dropping a file automatically runs /ALL and reads sections/resources. The command selection sidebar and Run button have been removed. Language, theme and tool configuration are in Settings. Dark mode uses orange and black; light mode uses blue and white. Cancellation terminates the tool process tree.

PE sections and resource directory entries can be inspected without dumpbin and exported as binary data. Resource paths currently use numeric type/name/language identifiers. English switches application toolbar labels immediately; analysis names currently show both languages. Original tool output is preserved. Ctrl+C copies selected output; Find wraps around.

## Projects

- Core: models and service contracts
- Infrastructure: process execution, Microsoft tool acquisition, settings, PE/COFF inspection
- App: WPF views and Toolkit view model
- Tests: executable smoke checks (`dotnet run --project tests/DumpbinGui.Tests`)

## Current limitations

Resource image/string/version previews and a hierarchical resource tree are not implemented yet. Download progress currently reports stages rather than percentage; download cannot yet be cancelled from the UI. Full localization of grid columns and diagnostics remains to be completed. Automatic acquisition currently targets x64 hosts. The live Microsoft download path has not yet been verified end to end.


## One-click packaging

Double-click pack.cmd or run ./pack.ps1. The default is a self-contained Windows x64 single-file application. Output: publish/win-x64 and publish/dumpbin-gui-V0.1-win-x64.zip. A .NET SDK and network access for runtime restore are needed on the build machine. Microsoft dumpbin is acquired separately on first use. For ARM64: ./pack.ps1 -Runtime win-arm64 (tool auto-download currently targets x64).
