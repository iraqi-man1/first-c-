$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repository = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$brandingDirectory = Join-Path $repository 'installer\Branding'
$logoPath = Join-Path $repository 'Fitlog\Assets\fitlog-logo.png'
New-Item -ItemType Directory -Path $brandingDirectory -Force | Out-Null

function New-Canvas([int]$width, [int]$height) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    return @{ Bitmap = $bitmap; Graphics = $graphics }
}

function Add-RoundedRectangle($graphics, [System.Drawing.Color]$color, [float]$x, [float]$y, [float]$width, [float]$height, [float]$radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    $brush = [System.Drawing.SolidBrush]::new($color)
    $graphics.FillPath($brush, $path)
    $brush.Dispose()
    $path.Dispose()
}

$canvas = New-Canvas 492 942
$bitmap = $canvas.Bitmap
$graphics = $canvas.Graphics
$rect = [System.Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height)
$gradient = [System.Drawing.Drawing2D.LinearGradientBrush]::new($rect, [System.Drawing.Color]::FromArgb(16, 24, 32), [System.Drawing.Color]::FromArgb(31, 52, 66), 48)
$graphics.FillRectangle($gradient, $rect)
$gradient.Dispose()

$softBlue = [System.Drawing.Color]::FromArgb(18, 163, 197, 213)
$softBlue2 = [System.Drawing.Color]::FromArgb(12, 163, 197, 213)
$brush = [System.Drawing.SolidBrush]::new($softBlue)
$graphics.FillEllipse($brush, 286, 40, 310, 310)
$brush.Dispose()
$brush = [System.Drawing.SolidBrush]::new($softBlue2)
$graphics.FillEllipse($brush, -164, 595, 390, 390)
$brush.Dispose()

$accent = [System.Drawing.Color]::FromArgb(163, 197, 213)
$line = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(70, 163, 197, 213), 2)
$graphics.DrawLine($line, 64, 76, 142, 76)
$line.Dispose()

$logo = [System.Drawing.Image]::FromFile($logoPath)
$graphics.DrawImage($logo, [System.Drawing.Rectangle]::new(180, 264, 132, 132))
$logo.Dispose()

$titleFont = [System.Drawing.Font]::new('Segoe UI', 40, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$taglineFont = [System.Drawing.Font]::new('Segoe UI', 16, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$eyebrowFont = [System.Drawing.Font]::new('Segoe UI', 14, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$bright = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(240, 244, 247))
$muted = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(166, 190, 204))
$accentBrush = [System.Drawing.SolidBrush]::new($accent)

$title = 'fitlog'
$titleSize = $graphics.MeasureString($title, $titleFont)
$graphics.DrawString($title, $titleFont, $bright, (492 - $titleSize.Width) / 2, 416)
$tagline = 'YOUR FITNESS, DAY BY DAY'
$taglineSize = $graphics.MeasureString($tagline, $taglineFont)
$graphics.DrawString($tagline, $taglineFont, $muted, (492 - $taglineSize.Width) / 2, 482)

Add-RoundedRectangle $graphics ([System.Drawing.Color]::FromArgb(28, 255, 255, 255)) 64 632 364 172 20
$graphics.DrawString('A clear plan for every day.', $eyebrowFont, $bright, 88, 662)
$graphics.DrawString('Training / Nutrition / Progress', $taglineFont, $muted, 88, 706)
$graphics.DrawString('Private by design. Saved on this PC.', $taglineFont, $muted, 88, 748)

$small = New-Canvas 192 192
$smallBitmap = $small.Bitmap
$smallGraphics = $small.Graphics
$smallBackground = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(27, 40, 50))
$smallGraphics.FillRectangle($smallBackground, 0, 0, 192, 192)
$smallBackground.Dispose()
$smallLogo = [System.Drawing.Image]::FromFile($logoPath)
$smallGraphics.DrawImage($smallLogo, [System.Drawing.Rectangle]::new(38, 38, 116, 116))
$smallLogo.Dispose()

try {
    $bitmap.Save((Join-Path $brandingDirectory 'FitlogWizard.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    $smallBitmap.Save((Join-Path $brandingDirectory 'FitlogWizardSmall.png'), [System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $bright.Dispose(); $muted.Dispose(); $accentBrush.Dispose(); $titleFont.Dispose(); $taglineFont.Dispose(); $eyebrowFont.Dispose()
    $graphics.Dispose(); $bitmap.Dispose(); $smallGraphics.Dispose(); $smallBitmap.Dispose()
}

Get-Item (Join-Path $brandingDirectory 'FitlogWizard.png'), (Join-Path $brandingDirectory 'FitlogWizardSmall.png') | Select-Object FullName,Length
