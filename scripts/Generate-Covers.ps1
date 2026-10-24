$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$dir = Join-Path $root 'backend\BookNest.Api\wwwroot\images'; New-Item -ItemType Directory -Force $dir | Out-Null
$colors = @('#557D69','#C59B6D','#667CA2','#8C789A','#C1907E')
$titles = @('NHUNG NGAY','TU DUY','SONG CHAM','KHAM PHA','KHU VUON')
$subtitles = @('nang trong','kinh doanh','giua pho dong','cong nghe','cua May')
for ($i = 0; $i -lt 5; $i++) {
    $bmp = New-Object System.Drawing.Bitmap 400,560; $g = [System.Drawing.Graphics]::FromImage($bmp); $g.SmoothingMode = 'AntiAlias'
    $bg = [System.Drawing.ColorTranslator]::FromHtml($colors[$i]); $g.Clear($bg)
    $cream = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#F4EAD2'))
    $soft = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(45,255,255,255))
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(100,255,255,255)),1
    $g.DrawRectangle($pen,20,20,360,520); $g.DrawLine($pen,30,85,370,85)
    $g.FillEllipse($soft,110,275,260,260); $g.FillEllipse($cream,-110,385,430,230)
    $dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(100,20,60,50)); $g.FillEllipse($dark,120,435,390,220)
    $font = New-Object System.Drawing.Font 'Georgia',31; $small = New-Object System.Drawing.Font 'Segoe UI',10; $sub = New-Object System.Drawing.Font 'Georgia',26
    $format = New-Object System.Drawing.StringFormat; $format.Alignment = 'Center'
    $g.DrawString('B O O K N E S T   E D I T I O N S',$small,$cream,[System.Drawing.RectangleF]::new(25,47,350,30),$format)
    $g.DrawString($titles[$i],$font,$cream,[System.Drawing.RectangleF]::new(20,125,360,100),$format)
    $g.DrawString($subtitles[$i],$sub,$cream,[System.Drawing.RectangleF]::new(20,220,360,100),$format)
    $g.DrawString('SACH MINH HOA - DO AN E-COMMERCE',$small,$cream,[System.Drawing.RectangleF]::new(20,510,360,30),$format)
    $bmp.Save((Join-Path $dir "cover-$($i+1).png"),[System.Drawing.Imaging.ImageFormat]::Png)
    $font.Dispose(); $small.Dispose(); $sub.Dispose(); $format.Dispose(); $cream.Dispose(); $soft.Dispose(); $dark.Dispose(); $pen.Dispose(); $g.Dispose(); $bmp.Dispose()
}
Write-Host 'Da tao 5 bia sach PNG local.'