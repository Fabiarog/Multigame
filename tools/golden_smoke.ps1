param([ValidateSet('regression','scene','gi')][string]$Mode='regression', [string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$projectDirectory=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $env:GODOT_BIN -or -not (Test-Path -LiteralPath $env:GODOT_BIN)) { throw 'Set GODOT_BIN to the Godot 4.7.2 .NET executable.' }
if (-not $OutputDirectory) { $OutputDirectory=Join-Path $projectDirectory ('temp/patch27-'+$Mode) }
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$previousAppData=$env:APPDATA; $previousQa=$env:MULTIGAME_QA_APPDATA
try {
    $env:APPDATA=Join-Path $OutputDirectory 'isolated-appdata'; $env:MULTIGAME_QA_APPDATA=$env:APPDATA
    New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null
    $arguments=@('--path',$projectDirectory,'--audio-driver','Dummy','--windowed','--position','-20000,-20000','--rendering-method','forward_plus','--rendering-driver','vulkan','--script',"res://tools/golden_$Mode.gd",'--',$OutputDirectory)
    $quoted=$arguments | ForEach-Object { if ($_ -match '"') { throw 'Unsupported quote in argument' }; '"'+$_+'"' }
    $stdout=Join-Path $OutputDirectory 'stdout.log'; $stderr=Join-Path $OutputDirectory 'stderr.log'
    $process=Start-Process -FilePath $env:GODOT_BIN -ArgumentList $quoted -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    if (-not $process.WaitForExit(300000)) { Stop-Process -Id $process.Id; throw "Golden QA timeout: $OutputDirectory" }
    Get-Content -LiteralPath $stdout,$stderr
    if ($process.ExitCode -ne 0 -or (Select-String -LiteralPath $stdout,$stderr -Pattern '(^|\s)(SCRIPT ERROR:|ERROR:|Unhandled exception)')) { throw "Golden QA failed: $OutputDirectory" }
}
finally { $env:APPDATA=$previousAppData; $env:MULTIGAME_QA_APPDATA=$previousQa }
