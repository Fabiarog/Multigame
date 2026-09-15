param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$taskProject = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskTools = Join-Path $env:TEMP 'multigame-tools'
$taskDotnetRoot = if ($env:DOTNET_ROOT) { $env:DOTNET_ROOT } else { Join-Path $taskTools 'dotnet' }
$taskGodot = if ($env:GODOT_BIN) { $env:GODOT_BIN } else { Join-Path $taskTools 'godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' }
if (-not (Test-Path -LiteralPath (Join-Path $taskDotnetRoot 'dotnet.exe')) -or -not (Test-Path -LiteralPath $taskGodot)) {
    throw 'Configure DOTNET_ROOT com a pasta do SDK .NET 8 e GODOT_BIN com o executável Godot 4.7.2 .NET.'
}
$env:DOTNET_ROOT = $taskDotnetRoot
$env:PATH = $taskDotnetRoot + ';' + $env:PATH
if (-not $SkipBuild) {
    & (Join-Path $taskDotnetRoot 'dotnet.exe') build (Join-Path $taskProject 'GameHub.csproj') --nologo --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar C#.' }
}
& $taskGodot --headless --path $taskProject --editor --import --quit
if ($LASTEXITCODE -ne 0) { throw 'Falha ao importar recursos.' }
Start-Process -FilePath $taskGodot -ArgumentList @('--path', ('"' + $taskProject + '"')) -WindowStyle Hidden
