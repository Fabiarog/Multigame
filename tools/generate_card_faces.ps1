# High-definition card texture art; meshes and motion are rendered in Godot.
Add-Type -AssemblyName System.Drawing
$taskOut = Join-Path $PSScriptRoot '..\assets\models\cards'
New-Item -ItemType Directory -Force -Path $taskOut | Out-Null
$taskRanks = @('A','2','3','4','5','6','7','8','9','10','J','Q','K')
$taskSuits = @{ spades = [char]0x2660; hearts = [char]0x2665; diamonds = [char]0x2666; clubs = [char]0x2663 }

# High-resolution (512x720) typography and pens
$taskFont = [Drawing.Font]::new('Georgia', 98, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$taskSmall = [Drawing.Font]::new('Segoe UI Symbol', 84, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel)
$taskLarge = [Drawing.Font]::new('Segoe UI Symbol', 280, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel)
$taskPaper = [Drawing.ColorTranslator]::FromHtml('#faf5e8')
$taskBorderOuter = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#c49e58'), 4)
$taskBorderInner = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#dfcca4'), 2)

foreach ($taskSuit in $taskSuits.Keys) {
    $taskColor = if ($taskSuit -in @('hearts','diamonds')) { '#b81d18' } else { '#0f171a' }
    $taskInk = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($taskColor))
    foreach ($taskRank in $taskRanks) {
        $taskImage = [Drawing.Bitmap]::new(512, 720)
        $taskGraphics = [Drawing.Graphics]::FromImage($taskImage)
        $taskGraphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $taskGraphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $taskGraphics.Clear($taskPaper)

        # Double border for luxury card feel
        $taskGraphics.DrawRectangle($taskBorderOuter, 16, 16, 479, 687)
        $taskGraphics.DrawRectangle($taskBorderInner, 24, 24, 463, 671)

        # Top-left index
        $taskGraphics.DrawString($taskRank, $taskFont, $taskInk, 32, 26)
        $rankOffset = if ($taskRank -eq '10') { 138 } else { 128 }
        $taskGraphics.DrawString([string]$taskSuits[$taskSuit], $taskSmall, $taskInk, 38, $rankOffset)

        # Center suit symbol
        $taskFormat = [Drawing.StringFormat]::new()
        $taskFormat.Alignment = [Drawing.StringAlignment]::Center
        $taskFormat.LineAlignment = [Drawing.StringAlignment]::Center
        $taskGraphics.DrawString([string]$taskSuits[$taskSuit], $taskLarge, $taskInk, [Drawing.RectangleF]::new(0, 185, 512, 350), $taskFormat)

        # Bottom-right index (rotated 180 degrees)
        $taskGraphics.TranslateTransform(512, 720)
        $taskGraphics.RotateTransform(180)
        $taskGraphics.DrawString($taskRank, $taskFont, $taskInk, 32, 26)
        $taskGraphics.DrawString([string]$taskSuits[$taskSuit], $taskSmall, $taskInk, 38, $rankOffset)

        $taskImage.Save((Join-Path $taskOut ($taskRank + '-' + $taskSuit + '.png')), [Drawing.Imaging.ImageFormat]::Png)
        $taskFormat.Dispose()
        $taskGraphics.Dispose()
        $taskImage.Dispose()
    }
    $taskInk.Dispose()
}

$taskFont.Dispose()
$taskSmall.Dispose()
$taskLarge.Dispose()
$taskBorderOuter.Dispose()
$taskBorderInner.Dispose()
Write-Output '52 high-definition card faces generated.'
