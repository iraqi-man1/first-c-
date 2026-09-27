param([string]$IsccPath)

$ErrorActionPreference = 'Stop'
$repository = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repository 'Fitlog\Fitlog.csproj'
$publishRoot = [System.IO.Path]::GetFullPath((Join-Path $repository 'artifacts\publish'))
$publishDirectory = [System.IO.Path]::GetFullPath((Join-Path $publishRoot 'win-x64'))
$brandingDirectory = Join-Path $repository 'installer\Branding'
$directorySeparator = [System.IO.Path]::DirectorySeparatorChar
if (-not $publishDirectory.StartsWith($publishRoot + $directorySeparator, [System.StringComparison]::OrdinalIgnoreCase) -or [System.IO.Path]::GetFileName($publishDirectory) -ne 'win-x64') {
    throw "Refusing to clean an unexpected publish path: $publishDirectory"
}
$localDotnet = Join-Path $env:LOCALAPPDATA 'FitlogTools\dotnet\dotnet.exe'
$dotnetCommand = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }

& (Join-Path $PSScriptRoot 'Generate-Icon.ps1') | Out-Null
& (Join-Path $PSScriptRoot 'Generate-InstallerArt.ps1') | Out-Null
& $dotnetCommand test (Join-Path $repository 'Fitlog.slnx') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
if (Test-Path -LiteralPath $publishDirectory) { Remove-Item -LiteralPath $publishDirectory -Recurse -Force }
& $dotnetCommand publish $project -c Release -r win-x64 --self-contained true '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' '-p:EnableCompressionInSingleFile=true' -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

$version = ([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup.Version
if (-not $IsccPath) {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 7\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe')
    )
    $IsccPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $IsccPath) { throw 'Inno Setup compiler ISCC.exe was not found. Install Inno Setup 6 or 7, or pass -IsccPath.' }
& $IsccPath "/DAppVersion=$version" "/DPublishDir=$publishDirectory" "/DBrandingDir=$brandingDirectory" (Join-Path $repository 'installer\Fitlog.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
Write-Output (Join-Path $repository "artifacts\installer\Fitlog-Setup-$version-win-x64.exe")
