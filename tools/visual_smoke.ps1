param(
    [string]$OutputDirectory = '',
    [switch]$SkipBuild,
    [switch]$AllowLayoutWarnings
)

# Prerequisites: DOTNET_ROOT points to a .NET 8 SDK folder, GODOT_BIN to the
# Godot 4.7.2 .NET console executable. No machine-wide installation is required.
$ErrorActionPreference = 'Stop'
$projectDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $env:DOTNET_ROOT -or -not (Test-Path -LiteralPath (Join-Path $env:DOTNET_ROOT 'dotnet.exe'))) {
    throw 'Set DOTNET_ROOT to the folder containing the .NET 8 SDK dotnet.exe.'
}
if (-not $env:GODOT_BIN -or -not (Test-Path -LiteralPath $env:GODOT_BIN)) {
    throw 'Set GODOT_BIN to the Godot 4.7.2 .NET console executable.'
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $projectDirectory 'docs\screenshots'
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$qaDirectory = Join-Path ([IO.Path]::GetTempPath()) ('multigame-visual-qa-' + [guid]::NewGuid().ToString('N'))
$qaAppData = Join-Path $qaDirectory 'AppData'
New-Item -ItemType Directory -Path $qaAppData -Force | Out-Null
$reportPath = Join-Path $qaDirectory 'report.json'
$originalEnvironment = @{}
foreach ($key in @('APPDATA', 'PATH', 'MULTIGAME_QA_APPDATA', 'DOTNET_CLI_TELEMETRY_OPTOUT')) {
    $originalEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
}

function Invoke-GodotQa {
    param([string[]]$GodotArguments, [string]$LogName)
    $stdoutPath = Join-Path $qaDirectory ($LogName + '.log')
    $stderrPath = Join-Path $qaDirectory ($LogName + '.errors.log')
    # Start-Process uses a Windows command line. These arguments are literal
    # paths/flags, double-quoted to preserve spaces, with no shell invocation.
    $quotedArguments = $GodotArguments | ForEach-Object {
        if ($_ -match '"') { throw 'A command argument contains an unsupported double quote.' }
        '"' + $_ + '"'
    }
    $process = Start-Process -FilePath $env:GODOT_BIN -ArgumentList $quotedArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    if (-not $process.WaitForExit(60000)) {
        Stop-Process -Id $process.Id
        throw "Godot QA timed out after 60 seconds. Logs: $qaDirectory"
    }
    Get-Content -LiteralPath $stdoutPath
    Get-Content -LiteralPath $stderrPath
    if ($process.ExitCode -ne 0) {
        throw "Godot QA failed with exit code $($process.ExitCode). Report/logs: $qaDirectory"
    }
    # Godot may report script exceptions without returning a nonzero code.
    if (Select-String -LiteralPath $stdoutPath, $stderrPath -Pattern '(^|\s)(SCRIPT ERROR:|ERROR:|Unhandled exception)' -Quiet) {
        throw "Godot reported runtime errors. Logs: $qaDirectory"
    }
}

try {
    $env:PATH = $env:DOTNET_ROOT + ';' + $env:PATH
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    if (-not $SkipBuild) {
        & (Join-Path $env:DOTNET_ROOT 'dotnet.exe') build (Join-Path $projectDirectory 'GameHub.csproj') --nologo --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw 'C# build failed.' }
    }

    # Godot's Windows user:// lookup reads APPDATA. Isolate it before the
    # engine starts, including autoload initialization and editor imports.
    $env:APPDATA = $qaAppData
    $env:MULTIGAME_QA_APPDATA = $qaAppData
    Invoke-GodotQa -GodotArguments @('--headless', '--path', $projectDirectory, '--editor', '--import', '--quit') -LogName 'import'
    Invoke-GodotQa -GodotArguments @('--headless', '--path', $projectDirectory, '--script', 'res://tools/gameplay_smoke.gd') -LogName 'gameplay'
    $captureArguments = @('--path', $projectDirectory, '--rendering-method', 'gl_compatibility', '--rendering-driver', 'opengl3', '--audio-driver', 'Dummy', '--windowed', '--resolution', '1280x720', '--position', '-20000,-20000', '--script', 'res://tools/visual_smoke.gd', '--', $OutputDirectory, $reportPath)
    if ($AllowLayoutWarnings) { $captureArguments += '--allow-layout-warnings' }
    Invoke-GodotQa -GodotArguments $captureArguments -LogName 'capture'
    Write-Output "Screenshots: $OutputDirectory"
    Write-Output "QA report and isolated session data: $qaDirectory"
}
finally {
    foreach ($key in $originalEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($key, $originalEnvironment[$key], 'Process')
    }
}
