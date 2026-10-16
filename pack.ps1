param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$publishPath = Join-Path $projectRoot "publish\$Runtime"
$version = ([xml](Get-Content -LiteralPath (Join-Path $projectRoot 'Directory.Build.props') -Raw)).Project.PropertyGroup.InformationalVersion
$archivePath = Join-Path $projectRoot "publish\dumpbin-gui-$version-$Runtime.zip"
$stagingPath = Join-Path $projectRoot ("build\staging\" + [guid]::NewGuid().ToString('N'))

Push-Location $projectRoot
try {
    Write-Host 'Building release package...' -ForegroundColor Cyan
    dotnet publish src/DumpbinGui.App/DumpbinGui.App.csproj -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $stagingPath
    if ($LASTEXITCODE -ne 0) { throw "Publish failed ($LASTEXITCODE)." }

    # Preserve the last successful package until publication completes.
    $resolvedPublish = [IO.Path]::GetFullPath($publishPath)
    $packageRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'publish')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedPublish.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid package path.' }
    if (Test-Path -LiteralPath $resolvedPublish) { Remove-Item -LiteralPath $resolvedPublish -Recurse -Force }
    New-Item -ItemType Directory -Path (Split-Path $resolvedPublish) -Force | Out-Null
    Move-Item -LiteralPath $stagingPath -Destination $resolvedPublish
    Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $resolvedPublish
    Compress-Archive -Path (Join-Path $resolvedPublish '*') -DestinationPath $archivePath -Force
    Write-Host "Package ready: $archivePath" -ForegroundColor Green
    Write-Host "Application: $resolvedPublish\DumpbinGui.App.exe"
}
finally { Pop-Location }
