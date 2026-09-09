$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = 'Research/Nadybot/src/Modules/WHEREIS_MODULE/whereis.csv'
$rows = @(Import-Csv (Join-Path $root $source) | Where-Object { $_.playfield_id -eq '800' -and $_.id -ne '84' })
$points = @($rows | ForEach-Object {
    [pscustomobject]@{ name=$_.name; x=[int]$_.xcoord; y=[int]$_.ycoord; source=$source; sourceRecord=[int]$_.id; status='community coordinate; client geometry unverified' }
})
# This coordinate is embedded in record 84's prose, not its generic city position.
$city = Import-Csv (Join-Path $root $source) | Where-Object id -eq '84'
if ($city.answer -notmatch 'Whompa to Stret West Bank at (\d+) x (\d+)') { throw 'Whompah coordinate source changed' }
$points += [pscustomobject]@{name='Stret West Bank whompah';x=[int]$Matches[1];y=[int]$Matches[2];source=$source;sourceRecord=84;status='coordinate extracted from prose; adjacent Newland portal position not specified'}
$out = Join-Path $root 'Research/Borealis'
New-Item -ItemType Directory -Path $out -Force | Out-Null
[pscustomobject]@{
    playfieldId=800
    coordinateSystem='AO x/y horizontal coordinates; no terrain heights or Unity transform established'
    scope='Landmark survey only. No building footprints, roads, terrain or travel implementation.'
    landmarks=$points
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'landmarks.json')
$svg = [System.Collections.Generic.List[string]]::new()
$svg.Add('<svg xmlns="http://www.w3.org/2000/svg" width="1000" height="820" viewBox="0 0 1000 820"><rect width="1000" height="820" fill="#111b23"/><g font-family="Arial" fill="#d9e7ed"><text x="35" y="38" font-size="24">Borealis — landmark coordinate survey</text><text x="35" y="65" font-size="14">Playfield 800 · community coordinates · not a terrain or street map</text>')
for($v=300;$v -le 800;$v+=50){
    $x=80+($v-300)*1.2;$y=720-($v-300)*1.2
    $svg.Add("<path d='M $x 120 V 720 M 80 $y H 680' stroke='#2b3c47'/><text x='$x' y='745' font-size='12'>$v</text><text x='43' y='$y' font-size='12'>$v</text>")
}
$index=0
foreach($p in $points){
    $index++
    $x=80+($p.x-300)*1.2;$y=720-($p.y-300)*1.2
    $label=[System.Security.SecurityElement]::Escape($p.name)
    $legendY=145+($index-1)*75
    $svg.Add("<circle cx='$x' cy='$y' r='5' fill='#60d0e8'/><text x='$($x+10)' y='$($y-8)' font-size='14'>$index</text><text x='710' y='$legendY' font-size='14'>$index. $label</text><text x='730' y='$($legendY+20)' font-size='12' fill='#9cb0ba'>$($p.x), $($p.y)</text>")
}
$svg.Add('<text x="80" y="785" font-size="13">Source: local Nadybot WHEREIS records; per-point provenance in landmarks.json.</text></g></svg>')
$svg -join "`n" | Set-Content -LiteralPath (Join-Path $out 'landmark-survey.svg')
Write-Output "Survey generated: $($points.Count) sourced landmarks; no scene coordinates modified."
