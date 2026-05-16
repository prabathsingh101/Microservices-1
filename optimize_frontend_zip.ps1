$sourcePath = "C:\Projects\ElectricApps"
$zipPath = "C:\Projects\Decode\ElectricApps.zip"

# 1. Clean existing zip if exists
if (Test-Path $zipPath) { Remove-Item $zipPath }

# 2. Get all files but exclude heavy folders
Write-Host "Gathering files for ElectricApps... (Excluding node_modules, dist, etc.)"
$files = Get-ChildItem -Path $sourcePath -Recurse | Where-Object {
    $_.FullName -notmatch "\\node_modules\\" -and 
    $_.FullName -notmatch "\\dist\\" -and 
    $_.FullName -notmatch "\\\.git\\" -and 
    $_.FullName -notmatch "\\\.vs\\" -and 
    $_.FullName -notmatch "\\\.angular\\"
}

# 3. Create zip
Write-Host "Creating optimized frontend zip... This might take a minute."
$files | Compress-Archive -DestinationPath $zipPath -Update

Write-Host "Frontend zip created successfully at $zipPath"
