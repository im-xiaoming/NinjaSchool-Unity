param(
    [int]$IconId = 1131,
    [ValidateRange(0, 255)][int]$WhiteThreshold = 235,
    [switch]$UseZoom1Silhouette,
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

# Offline art maintenance: remove only white/near-white background connected to an image edge.
# Border flood fill protects enclosed white highlights. Default mode preserves RGB and canvas size.
# Optional zoom-1 silhouette mode also recovers contaminated edge colors from the transparent reference.
# Use 255 for pure white only; 235 includes the original icon's near-white resampling fringe.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$backupRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) ('.codex/backups/Small' + $IconId + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
[IO.Directory]::CreateDirectory($backupRoot) | Out-Null

foreach ($zoom in 1..4) {
    $path = Join-Path $ProjectRoot "Assets/StreamingAssets/NsoStatic/Data/Img/Small/$zoom/Small$IconId.png"
    if (!(Test-Path -LiteralPath $path)) { Write-Warning "Missing zoom $zoom : $path"; continue }
    $before = [IO.File]::ReadAllBytes($path)
    [IO.File]::WriteAllBytes((Join-Path $backupRoot "zoom$zoom-original.png"), $before)
    $metaPath = "$path.meta"
    $metaBefore = if (Test-Path -LiteralPath $metaPath) { [IO.File]::ReadAllBytes($metaPath) } else { $null }
    $source = [Drawing.Bitmap]::new($path)
    $output = $null
    try {
        $width = $source.Width; $height = $source.Height
        $visited = [bool[]]::new($width * $height)
        $removed = [bool[]]::new($width * $height)
        $recolored = @{}
        $queue = [Collections.Generic.Queue[int]]::new()
        for ($x = 0; $x -lt $width; $x++) { $queue.Enqueue($x); $queue.Enqueue(($height - 1) * $width + $x) }
        for ($y = 0; $y -lt $height; $y++) { $queue.Enqueue($y * $width); $queue.Enqueue($y * $width + $width - 1) }
        $count = 0
        while ($queue.Count -gt 0) {
            $index = $queue.Dequeue()
            if ($visited[$index]) { continue }
            $visited[$index] = $true
            $x = $index % $width; $y = [int][Math]::Floor($index / $width)
            $pixel = $source.GetPixel($x, $y)
            $white = $pixel.R -ge $WhiteThreshold -and $pixel.G -ge $WhiteThreshold -and $pixel.B -ge $WhiteThreshold
            if ($pixel.A -ne 0 -and !$white) { continue }
            if ($pixel.A -ne 0) { $removed[$index] = $true; $count++ }
            if ($x -gt 0) { $queue.Enqueue($index - 1) }
            if ($x + 1 -lt $width) { $queue.Enqueue($index + 1) }
            if ($y -gt 0) { $queue.Enqueue($index - $width) }
            if ($y + 1 -lt $height) { $queue.Enqueue($index + $width) }
        }
        # Optional exact silhouette from the original transparent pixel-art variant.
        # Removes darker white-matte fringe outside that silhouette without changing inner RGB.
        if ($UseZoom1Silhouette -and $zoom -gt 1) {
            $reference = [Drawing.Bitmap]::new((Join-Path $ProjectRoot "Assets/StreamingAssets/NsoStatic/Data/Img/Small/1/Small$IconId.png"))
            try {
                if ($width -ne $reference.Width * $zoom -or $height -ne $reference.Height * $zoom) { throw 'Zoom dimensions do not match the reference silhouette' }
                for ($y = 0; $y -lt $height; $y++) {
                    for ($x = 0; $x -lt $width; $x++) {
                        $index = $y * $width + $x
                        $rx = [int][Math]::Floor($x / $zoom); $ry = [int][Math]::Floor($y / $zoom)
                        if (!$removed[$index] -and $source.GetPixel($x, $y).A -gt 0 -and $reference.GetPixel($rx, $ry).A -eq 0) {
                            $removed[$index] = $true; $count++
                        } elseif (!$removed[$index] -and $source.GetPixel($x, $y).A -gt 0) {
                            $ref = $reference.GetPixel($rx, $ry)
                            $edge = $rx -eq 0 -or $ry -eq 0 -or $rx -eq $reference.Width - 1 -or $ry -eq $reference.Height - 1
                            if (!$edge) { $edge = $reference.GetPixel($rx - 1, $ry).A -eq 0 -or $reference.GetPixel($rx + 1, $ry).A -eq 0 -or $reference.GetPixel($rx, $ry - 1).A -eq 0 -or $reference.GetPixel($rx, $ry + 1).A -eq 0 }
                            $old = $source.GetPixel($x, $y)
                            # White matte brightens the original dark contour. Recover its exact
                            # original color only in reference edge cells; inner shading stays intact.
                            if ($edge -and $ref.A -gt 0 -and ($old.R + $old.G + $old.B) -gt ($ref.R + $ref.G + $ref.B + 72)) {
                                $recolored[$index] = [Drawing.Color]::FromArgb($old.A, $ref.R, $ref.G, $ref.B)
                            }
                        }
                    }
                }
            } finally { $reference.Dispose() }
        }
        if ($count -eq 0 -and $recolored.Count -eq 0) { Write-Output "Zoom $zoom : ${width}x${height}, already transparent; unchanged"; continue }
        $output = [Drawing.Bitmap]::new($width, $height, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        for ($y = 0; $y -lt $height; $y++) {
            for ($x = 0; $x -lt $width; $x++) {
                $pixel = $source.GetPixel($x, $y)
                if ($recolored.ContainsKey($y * $width + $x)) { $pixel = $recolored[$y * $width + $x] }
                if ($removed[$y * $width + $x]) { $pixel = [Drawing.Color]::FromArgb(0, $pixel.R, $pixel.G, $pixel.B) }
                $output.SetPixel($x, $y, $pixel)
            }
        }
        $cleanPath = Join-Path $backupRoot "zoom$zoom-clean.png"
        $output.Save($cleanPath, [Drawing.Imaging.ImageFormat]::Png)
        $verified = [Drawing.Bitmap]::new($cleanPath)
        try {
            if ($verified.Width -ne $width -or $verified.Height -ne $height) { throw 'Canvas dimensions changed' }
            for ($y = 0; $y -lt $height; $y++) {
                for ($x = 0; $x -lt $width; $x++) {
                    $old = $source.GetPixel($x, $y); $new = $verified.GetPixel($x, $y)
                    if ($removed[$y * $width + $x]) {
                        if ($new.A -ne 0 -or $old.R -ne $new.R -or $old.G -ne $new.G -or $old.B -ne $new.B) { throw 'Background alpha verification failed' }
                    } elseif ($recolored.ContainsKey($y * $width + $x)) {
                        if ($new.ToArgb() -ne $recolored[$y * $width + $x].ToArgb()) { throw 'Contour color verification failed' }
                    } elseif ($old.ToArgb() -ne $new.ToArgb()) { throw 'Foreground pixel changed' }
                }
            }
        } finally { $verified.Dispose() }
        $source.Dispose(); $source = $null
        # Atomic replacement avoids truncating a PNG that the Unity importer has memory-mapped.
        $staged = Join-Path ([IO.Path]::GetDirectoryName($path)) ('.Small' + $IconId + '-' + [Guid]::NewGuid().ToString('N') + '.tmp')
        try {
            [IO.File]::WriteAllBytes($staged, [IO.File]::ReadAllBytes($cleanPath))
            [IO.File]::Replace($staged, $path, (Join-Path $backupRoot "zoom$zoom-before-replace.png"))
        } finally { if (Test-Path -LiteralPath $staged) { Remove-Item -LiteralPath $staged } }
        if ($null -ne $metaBefore -and [Convert]::ToBase64String($metaBefore) -ne [Convert]::ToBase64String([IO.File]::ReadAllBytes($metaPath))) { throw 'Meta file changed' }
        Write-Output "Zoom $zoom : ${width}x${height}, removed $count background pixels, corrected $($recolored.Count) matte contour pixels; inner foreground and meta verified"
    } finally {
        if ($null -ne $source) { $source.Dispose() }
        if ($null -ne $output) { $output.Dispose() }
    }
}
Write-Output "Backups: $backupRoot"
