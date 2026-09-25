param([switch]$BuildOnly)
$ErrorActionPreference = 'Stop'
$localDotnet = Join-Path $env:LOCALAPPDATA 'FitlogTools\dotnet\dotnet.exe'
$dotnetCommand = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
$projectPath = Join-Path $PSScriptRoot 'Fitlog\Fitlog.csproj'
if ($BuildOnly) { & $dotnetCommand build $projectPath } else { & $dotnetCommand run --project $projectPath }
exit $LASTEXITCODE
