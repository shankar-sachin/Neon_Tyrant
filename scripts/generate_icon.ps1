# Generate a multi-size .ico file for Neon Tyrant
# Produces game_cs/icon.ico with 16x16 and 32x32 images

$outPath = Join-Path $PSScriptRoot "..\game_cs\icon.ico"

function Write-BitmapData([System.IO.BinaryWriter]$bw, [int]$size, [string[]]$pattern) {
    # Colors in BGRA order
    $colors = @{
        '.' = @(26, 10, 10, 255)      # bg dark blue-black
        'C' = @(255, 200, 0, 255)     # cyan (BGR)
        'W' = @(255, 255, 255, 255)   # white
        'G' = @(180, 120, 0, 200)     # dim cyan glow
        'M' = @(255, 0, 255, 255)     # magenta
    }

    $andRowBytes = [Math]::Ceiling($size / 32) * 4

    # BITMAPINFOHEADER (40 bytes)
    $bw.Write([int32]40)
    $bw.Write([int32]$size)
    $bw.Write([int32]($size * 2))
    $bw.Write([int16]1)
    $bw.Write([int16]32)
    $bw.Write([int32]0)
    $bw.Write([int32]0)
    $bw.Write([int32]0)
    $bw.Write([int32]0)
    $bw.Write([int32]0)
    $bw.Write([int32]0)

    # Pixel data bottom-up
    for ($y = $size - 1; $y -ge 0; $y--) {
        $row = $pattern[$y]
        for ($x = 0; $x -lt $size; $x++) {
            if ($x -lt $row.Length) {
                $ch = [string]$row[$x]
            } else {
                $ch = '.'
            }
            if ($colors.ContainsKey($ch)) {
                $c = $colors[$ch]
            } else {
                $c = $colors['.']
            }
            $bw.Write([byte]$c[0])
            $bw.Write([byte]$c[1])
            $bw.Write([byte]$c[2])
            $bw.Write([byte]$c[3])
        }
    }

    # AND mask
    for ($y = 0; $y -lt $size; $y++) {
        for ($b = 0; $b -lt $andRowBytes; $b++) {
            $bw.Write([byte]0)
        }
    }
}

function Get-ImageSize([int]$size, [string[]]$pattern) {
    $andRowBytes = [Math]::Ceiling($size / 32) * 4
    return 40 + ($size * $size * 4) + ($andRowBytes * $size)
}

# 16x16 pattern
$p16 = @(
    "................",
    ".CCCC....CCCCC..",
    ".CWCC....CCWCC..",
    ".CWWC....CCWCC..",
    ".CWCW....CCWCC..",
    ".CWCCW...CCWCC..",
    ".CWCCC...CCCWC..",
    ".CWCCC...CCWCC..",
    ".CWCCC...CWCCC..",
    ".CCCCC...CCCCC..",
    "................",
    "...MMMMMMMM....",
    "...MMMMMMMM....",
    "................",
    "................",
    "................"
)

# 32x32 pattern
$p32 = @(
    "................................",
    "................................",
    "..GGGGGG..........GGGGGGGG....",
    "..GCCCCG..........GCCCCCCG....",
    "..GCWCCG..........GCCWCCCG....",
    "..GCWWCG..........GCCWCCCG....",
    "..GCWCWG..........GCCWCCCG....",
    "..GCWCCWG.........GCCWCCCG....",
    "..GCWCCCWG........GCCWCCCG....",
    "..GCWCCCCG........GCCCCCWG....",
    "..GCWCCCCG........GCCCCWCG....",
    "..GCWCCCCG........GCCCWCCG....",
    "..GCWCCCCG........GCCWCCCG....",
    "..GCWCCCCG........GCWCCCCG....",
    "..GCWCCCCG........GWCCCCCG....",
    "..GCCCCCCG........GCCCCCCG....",
    "..GGGGGGGG........GGGGGGGG....",
    "................................",
    "................................",
    "................................",
    "......MMMMMMMMMMMMMMMM........",
    "......MMMMMMMMMMMMMMMM........",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................"
)

$sizes = @(16, 32)
$patterns = @($p16, $p32)
$imgSizes = @()
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $imgSizes += (Get-ImageSize $sizes[$i] $patterns[$i])
}

# Write ICO
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)

# Header
$bw.Write([int16]0)
$bw.Write([int16]1)
$bw.Write([int16]$sizes.Count)

# Directory entries
$dataOffset = 6 + (16 * $sizes.Count)
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $bw.Write([byte]$(if ($s -ge 256) { 0 } else { $s }))
    $bw.Write([byte]$(if ($s -ge 256) { 0 } else { $s }))
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([int16]1)
    $bw.Write([int16]32)
    $bw.Write([int32]$imgSizes[$i])
    $bw.Write([int32]$dataOffset)
    $dataOffset += $imgSizes[$i]
}

# Image data
for ($i = 0; $i -lt $sizes.Count; $i++) {
    Write-BitmapData $bw $sizes[$i] $patterns[$i]
}

$bw.Flush()
[System.IO.File]::WriteAllBytes($outPath, $ms.ToArray())
$bw.Close()
$ms.Close()

$fileInfo = Get-Item $outPath
Write-Host "Icon generated at: $outPath ($($fileInfo.Length) bytes)"
Write-Host "Sizes: 16x16, 32x32"
