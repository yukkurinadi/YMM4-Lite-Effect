$bytes = [System.IO.File]::ReadAllBytes("voronoinoise.cso")
$chars = [char[]]::new($bytes.Length)
for ($i = 0; $i -lt $bytes.Length; $i++) {
    $b = $bytes[$i]
    if ($b -ge 32 -and $b -le 126) { $chars[$i] = [char]$b } else { $chars[$i] = "`n" }
}
$text = -join $chars
$lines = $text -split "`n" | Where-Object { $_.Length -ge 4 }
$lines | Out-File -FilePath strings.txt -Encoding UTF8
Write-Output "Done. Found lines: $($lines.Count)"
