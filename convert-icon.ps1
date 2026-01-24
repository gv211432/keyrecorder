Add-Type -AssemblyName System.Drawing

# Load the PNG
$png = [System.Drawing.Image]::FromFile("$PSScriptRoot\KeyRecorder.UI\Assets\logo.png")

# Create multiple sizes for ICO (16x16, 32x32, 48x48, 256x256)
$sizes = @(16, 32, 48, 256)
$icons = @()

foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawImage($png, 0, 0, $size, $size)
    $graphics.Dispose()
    $icons += $bitmap
}

# Write ICO file manually
$icoPath = "$PSScriptRoot\KeyRecorder.UI\Assets\logo.ico"
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)

# ICO header
$bw.Write([Int16]0)        # Reserved
$bw.Write([Int16]1)        # Type (1 = ICO)
$bw.Write([Int16]$icons.Count) # Number of images

# Calculate offsets
$headerSize = 6 + (16 * $icons.Count)
$offset = $headerSize
$imageData = @()

foreach ($icon in $icons) {
    $ms = New-Object System.IO.MemoryStream
    $icon.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $data = $ms.ToArray()
    $imageData += ,$data
    $ms.Dispose()
}

# Write directory entries
for ($i = 0; $i -lt $icons.Count; $i++) {
    $size = $sizes[$i]
    $width = if ($size -ge 256) { 0 } else { $size }
    $height = if ($size -ge 256) { 0 } else { $size }

    $bw.Write([Byte]$width)     # Width
    $bw.Write([Byte]$height)    # Height
    $bw.Write([Byte]0)          # Color palette
    $bw.Write([Byte]0)          # Reserved
    $bw.Write([Int16]1)         # Color planes
    $bw.Write([Int16]32)        # Bits per pixel
    $bw.Write([Int32]$imageData[$i].Length) # Size
    $bw.Write([Int32]$offset)   # Offset

    $offset += $imageData[$i].Length
}

# Write image data
foreach ($data in $imageData) {
    $bw.Write($data)
}

$bw.Close()
$fs.Close()
$png.Dispose()
foreach ($icon in $icons) { $icon.Dispose() }

Write-Host "Created logo.ico successfully at: $icoPath"
Get-Item $icoPath | Select-Object Name, Length
