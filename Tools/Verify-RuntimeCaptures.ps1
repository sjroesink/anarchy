param([string]$CaptureDirectory)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $CaptureDirectory) { $CaptureDirectory = Join-Path $projectRoot 'Artifacts' }
$CaptureDirectory = (Resolve-Path -LiteralPath $CaptureDirectory).Path
$reportPath = Join-Path $CaptureDirectory 'runtime-capture-validation.json'
# Invalidate the previous run before reading files: failures must not leave stale success.
[pscustomobject]@{ status = 'incomplete'; checkedAtUtc = [DateTime]::UtcNow.ToString('o'); scope = 'Nonblank image check only'; passed = 0 } | ConvertTo-Json | Set-Content -LiteralPath $reportPath
$captureSource = Get-Content -LiteralPath (Join-Path $projectRoot 'Unity/Assets/Scripts/DistrictGame.cs') -Raw
$captureNames = [regex]::Matches($captureSource, 'QaArtifactDirectory,"([^"/]+\.png)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
if ($captureNames.Count -eq 0) { throw 'No runtime capture names found' }
$results = foreach ($captureName in $captureNames) {
    $capturePath = Join-Path $CaptureDirectory $captureName
    $bitmap = [System.Drawing.Bitmap]::new($capturePath)
    try {
        $colors = [System.Collections.Generic.HashSet[int]]::new()
        $lit = 0
        $samples = 0
        for ($y = 0; $y -lt $bitmap.Height; $y += 20) {
            for ($x = 0; $x -lt $bitmap.Width; $x += 20) {
                $pixel = $bitmap.GetPixel($x, $y)
                [void]$colors.Add($pixel.ToArgb())
                if (($pixel.R + $pixel.G + $pixel.B) -gt 30) { $lit++ }
                $samples++
            }
        }
        if ($bitmap.Width -lt 800 -or $bitmap.Height -lt 450 -or $colors.Count -lt 16 -or $lit / $samples -lt 0.25) {
            [pscustomobject]@{ status = 'failed'; checkedAtUtc = [DateTime]::UtcNow.ToString('o'); failedFile = $captureName; width = $bitmap.Width; height = $bitmap.Height; sampledColors = $colors.Count; visibleFraction = $lit / $samples; scope = 'Nonblank image check only'; passed = 0 } | ConvertTo-Json | Set-Content -LiteralPath $reportPath
            throw "Blank or undersized runtime capture: $captureName"
        }
        [pscustomobject]@{ file = $captureName; width = $bitmap.Width; height = $bitmap.Height; sampledColors = $colors.Count; visibleFraction = $lit / $samples }
    }
    finally { $bitmap.Dispose() }
}
[pscustomobject]@{ status = 'passed'; checkedAtUtc = [DateTime]::UtcNow.ToString('o'); scope = 'Nonblank image check only; does not prove correct scene rendering or AO fidelity'; passed = $results.Count; results = @($results) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $reportPath
Write-Output "PASS: $($results.Count) nonblank runtime captures"
