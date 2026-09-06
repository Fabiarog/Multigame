Add-Type -AssemblyName System.Drawing

$size = 2048
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

# Clear base
$brushDark = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 14, 18, 16))
$g.FillRectangle($brushDark, 0, 0, $size, $size)
$brushDark.Dispose()

function Fill-Box($x, $y, $w, $h, $color) {
    $b = New-Object System.Drawing.SolidBrush($color)
    $g.FillRectangle($b, [int]$x, [int]$y, [int]$w, [int]$h)
    $b.Dispose()
}

# --- QUADRANT 1 (0,0 to 1024,1024): CLUBE CLÁSSICO ---
# Parquet Floor (0..1024, 0..512)
for ($y = 0; $y -lt 512; $y += 32) {
    for ($x = 0; $x -lt 1024; $x += 64) {
        $cval = 38 + (($x / 64 + $y / 32) % 3) * 8
        $col = [System.Drawing.Color]::FromArgb(255, $cval, [Math]::Max(0, $cval - 16), [Math]::Max(0, $cval - 26))
        Fill-Box $x $y 64 32 $col
        $penBorder = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 20, 10, 5), 1)
        $g.DrawRectangle($penBorder, [int]$x, [int]$y, 63, 31)
        $penBorder.Dispose()
        $penGrain = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, [Math]::Max(0, $cval - 12), [Math]::Max(0, $cval - 22), [Math]::Max(0, $cval - 30)), 1)
        $g.DrawLine($penGrain, [int]($x + 2), [int]($y + 8), [int]($x + 60), [int]($y + 8))
        $g.DrawLine($penGrain, [int]($x + 4), [int]($y + 20), [int]($x + 58), [int]($y + 20))
        $penGrain.Dispose()
    }
}

# Emerald Damask Wallpaper (0..1024, 512..1024)
Fill-Box 0 512 1024 512 ([System.Drawing.Color]::FromArgb(255, 18, 52, 38))
$penGold = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 215, 175, 75), 2)
$brushGold = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(220, 215, 175, 75))
for ($y = 512; $y -lt 1024; $y += 64) {
    for ($x = 0; $x -lt 1024; $x += 64) {
        $pts = [System.Drawing.Point[]]@(
            [System.Drawing.Point]::new(($x + 32), ($y + 8)),
            [System.Drawing.Point]::new(($x + 56), ($y + 32)),
            [System.Drawing.Point]::new(($x + 32), ($y + 56)),
            [System.Drawing.Point]::new(($x + 8), ($y + 32))
        )
        $g.DrawPolygon($penGold, $pts)
        $g.FillEllipse($brushGold, ($x + 27), ($y + 27), 10, 10)
    }
}
$penGold.Dispose(); $brushGold.Dispose()

# Boiserie Walnut Lower Molding (0..1024, 500..524)
Fill-Box 0 500 1024 24 ([System.Drawing.Color]::FromArgb(255, 46, 24, 16))
Fill-Box 0 508 1024 4 ([System.Drawing.Color]::FromArgb(255, 220, 180, 80))

# --- QUADRANT 2 (1024,0 to 2048,1024): SALÃO DO BARÃO (MEIA-NOITE) ---
# Dark Polished Marble Floor (1024..2048, 0..512)
Fill-Box 1024 0 1024 512 ([System.Drawing.Color]::FromArgb(255, 18, 16, 26))
$penVein1 = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(80, 50, 42, 70), 3)
$penVein2 = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(110, 200, 160, 60), 1)
for ($i = 1024; $i -lt 2048; $i += 128) {
    $g.DrawLine($penVein1, [int]$i, 0, [int]($i + 512), 512)
    $g.DrawLine($penVein2, [int]$i, 512, [int]($i + 512), 0)
}
$penVein1.Dispose(); $penVein2.Dispose()

# Midnight Purple & Gold Damask (1024..2048, 512..1024)
Fill-Box 1024 512 1024 512 ([System.Drawing.Color]::FromArgb(255, 24, 18, 42))
$penGold2 = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 220, 180, 80), 2)
$brushGold2 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(220, 220, 180, 80))
for ($y = 512; $y -lt 1024; $y += 64) {
    for ($x = 1024; $x -lt 2048; $x += 64) {
        $pts = [System.Drawing.Point[]]@(
            [System.Drawing.Point]::new(($x + 32), ($y + 8)),
            [System.Drawing.Point]::new(($x + 56), ($y + 32)),
            [System.Drawing.Point]::new(($x + 32), ($y + 56)),
            [System.Drawing.Point]::new(($x + 8), ($y + 32))
        )
        $g.DrawPolygon($penGold2, $pts)
        $g.FillEllipse($brushGold2, ($x + 28), ($y + 28), 8, 8)
    }
}
$penGold2.Dispose(); $brushGold2.Dispose()
Fill-Box 1024 500 1024 24 ([System.Drawing.Color]::FromArgb(255, 30, 20, 48))
Fill-Box 1024 508 1024 4 ([System.Drawing.Color]::FromArgb(255, 220, 180, 80))

# --- QUADRANT 3 (0,1024 to 1024,2048): GABINETE DA DAMA (RUBI / CARMIM) ---
# Rosewood Parquet (0..1024, 1024..1536)
for ($y = 1024; $y -lt 1536; $y += 32) {
    for ($x = 0; $x -lt 1024; $x += 64) {
        $cval = 55 + (($x / 64 + $y / 32) % 3) * 10
        $col = [System.Drawing.Color]::FromArgb(255, $cval, [Math]::Max(0, $cval - 35), [Math]::Max(0, $cval - 32))
        Fill-Box $x $y 64 32 $col
        $penBorder = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 25, 8, 12), 1)
        $g.DrawRectangle($penBorder, [int]$x, [int]$y, 63, 31)
        $penBorder.Dispose()
    }
}

# Crimson & Gold Wallpaper (0..1024, 1536..2048)
Fill-Box 0 1536 1024 512 ([System.Drawing.Color]::FromArgb(255, 62, 16, 24))
$penRubyGold = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 225, 175, 70), 2)
$brushRubyGold = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(220, 225, 175, 70))
for ($y = 1536; $y -lt 2048; $y += 64) {
    for ($x = 0; $x -lt 1024; $x += 64) {
        $pts = [System.Drawing.Point[]]@(
            [System.Drawing.Point]::new(($x + 32), ($y + 8)),
            [System.Drawing.Point]::new(($x + 56), ($y + 32)),
            [System.Drawing.Point]::new(($x + 32), ($y + 56)),
            [System.Drawing.Point]::new(($x + 8), ($y + 32))
        )
        $g.DrawPolygon($penRubyGold, $pts)
        $g.FillEllipse($brushRubyGold, ($x + 24), ($y + 24), 8, 8)
        $g.FillEllipse($brushRubyGold, ($x + 32), ($y + 24), 8, 8)
    }
}
$penRubyGold.Dispose(); $brushRubyGold.Dispose()
Fill-Box 0 1524 1024 24 ([System.Drawing.Color]::FromArgb(255, 45, 12, 18))
Fill-Box 0 1532 1024 4 ([System.Drawing.Color]::FromArgb(255, 225, 175, 70))

# --- QUADRANT 4 (1024,1024 to 2048,2048): CASSINO NEON ---
# Polished Black Granite Tile (1024..2048, 1024..1536)
Fill-Box 1024 1024 1024 512 ([System.Drawing.Color]::FromArgb(255, 12, 14, 18))
$penGrid = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(60, 40, 55, 75), 2)
for ($y = 1024; $y -lt 1536; $y += 64) { $g.DrawLine($penGrid, 1024, [int]$y, 2048, [int]$y) }
for ($x = 1024; $x -lt 2048; $x += 64) { $g.DrawLine($penGrid, [int]$x, 1024, [int]$x, 1536) }
$penGrid.Dispose()

# Dark Slate with Neon Strips (1024..2048, 1536..2048)
Fill-Box 1024 1536 1024 512 ([System.Drawing.Color]::FromArgb(255, 16, 20, 24))
for ($y = 1560; $y -lt 2048; $y += 96) {
    Fill-Box 1024 $y 1024 6 ([System.Drawing.Color]::FromArgb(255, 40, 210, 235))
    Fill-Box 1024 ($y + 10) 1024 4 ([System.Drawing.Color]::FromArgb(255, 220, 185, 70))
}

# --- FRAMED ARTWORK / PICTURES FOR WALLS ---
# Picture 1: Antique Ace of Spades Oil Painting in Ornate Gold Frame (at 780, 680, 240, 320)
Fill-Box 780 680 240 320 ([System.Drawing.Color]::FromArgb(255, 220, 175, 60))
Fill-Box 796 696 208 288 ([System.Drawing.Color]::FromArgb(255, 242, 238, 225))
$fontSpade = New-Object System.Drawing.Font("Georgia", [float]64, [System.Drawing.FontStyle]::Bold)
$brushSpade = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 20, 24, 22))
$g.DrawString("A", $fontSpade, $brushSpade, 860, 780)
$fontSpade.Dispose(); $brushSpade.Dispose()

# Picture 2: Barão's Golden Crown Portrait (at 1780, 680, 240, 320)
Fill-Box 1780 680 240 320 ([System.Drawing.Color]::FromArgb(255, 225, 180, 70))
Fill-Box 1796 696 208 288 ([System.Drawing.Color]::FromArgb(255, 22, 16, 36))
$fontCrown = New-Object System.Drawing.Font("Georgia", [float]64, [System.Drawing.FontStyle]::Bold)
$brushCrown = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 225, 180, 70))
$g.DrawString("K", $fontCrown, $brushCrown, 1860, 780)
$fontCrown.Dispose(); $brushCrown.Dispose()

# Save
$outDir = "c:\workspace\multigame\assets\models\club"
if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force }
$outPath = Join-Path $outDir "club_environment_palette.png"
$bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)

$g.Dispose()
$bmp.Dispose()
Write-Output "Successfully generated club environment palette at $outPath"
