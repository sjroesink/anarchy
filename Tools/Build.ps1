param(
    [switch]$RebuildModels,
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe',
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if ($RebuildModels) {
    & $Blender -b --python-exit-code 1 -P (Join-Path $PSScriptRoot 'create_models.py')
    if ($LASTEXITCODE -ne 0) { throw 'Blender export failed.' }
}
$unityArgs = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + (Join-Path $projectRoot 'Unity') + '"'), '-executeMethod', 'DistrictBuilder.BuildWindows', '-logFile', ('"' + (Join-Path $projectRoot 'unity-build.log') + '"'))
$buildProcess = Start-Process -FilePath $UnityEditor -ArgumentList $unityArgs -WindowStyle Hidden -Wait -PassThru
if ($buildProcess.ExitCode -ne 0) { throw 'Unity build failed. See unity-build.log.' }
Write-Output ('Build ready: ' + (Join-Path $projectRoot 'Builds\AnarchyReborn.exe'))
