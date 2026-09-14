param([string]$OutputDirectory = 'C:/workspace/multigame/docs/network-qa', [switch]$Visual)
$ErrorActionPreference = 'Stop'
$projectDirectory = Split-Path $PSScriptRoot -Parent
$godot = 'C:/Users/Lucas/AppData/Local/Temp/multigame-tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64.exe'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$original = @{}
foreach ($key in @('APPDATA','DOTNET_ROOT','MULTIGAME_NET_QA','MULTIGAME_QA_APPDATA')) { $original[$key] = [Environment]::GetEnvironmentVariable($key,'Process') }
try {
 $env:DOTNET_ROOT = 'C:/Users/Lucas/AppData/Local/Temp/multigame-tools/dotnet'
 $env:MULTIGAME_NET_QA = '1'
 $port = 17770
 foreach ($game in @('truco','fodinha','poker_classic')) {
  $processes = @()
  foreach ($role in @('host','client')) {
   $env:APPDATA = Join-Path $env:TEMP ('multigame-net-' + [guid]::NewGuid().ToString('N'))
   New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
   $env:MULTIGAME_QA_APPDATA = $env:APPDATA
   $arguments = @('--path',$projectDirectory,'--script','res://tools/network_integration.gd','--',$role,$game,$port)
   if ($Visual) { $arguments = @('--rendering-method','gl_compatibility','--resolution','1280x720') + $arguments; $arguments = $arguments[0..($arguments.Length-2)] + @('visual',$port) } else { $arguments = @('--headless') + $arguments }
   $arguments = $arguments | ForEach-Object { '"' + $_ + '"' }
   $processes += Start-Process -FilePath $godot -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $OutputDirectory "$game-$role.log") -RedirectStandardError (Join-Path $OutputDirectory "$game-$role.errors.log")
   if ($role -eq 'host') { Start-Sleep -Milliseconds 1800 }
  }
  foreach ($process in $processes) { if (-not $process.WaitForExit(140000)) { Stop-Process -Id $process.Id; throw "$game timed out" } }
  foreach ($role in @('host','client')) { $log = Get-Content (Join-Path $OutputDirectory "$game-$role.log") -Raw; if ($log -notmatch 'NET_QA_PASS') { throw "$game $role failed: $log" }; ($log -split "`n" | Where-Object { $_ -match 'NET_QA_PASS' }) }
  $port++
 }
} finally { foreach ($key in $original.Keys) { [Environment]::SetEnvironmentVariable($key,$original[$key],'Process') } }
