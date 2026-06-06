$sourcePath = "C:\Projects\ElectricApps"
$stagingPath = "C:\Projects\ElectricApps_staging"
$zipPath = "C:\Projects\ElectricApps\ElectricApps.zip"

# 1. Clean existing zip and staging if exists
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
if (Test-Path $stagingPath) { Remove-Item $stagingPath -Recurse -Force }

# 2. Create staging directory
New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null

Write-Host "Gathering and copying optimized frontend files to staging..."
# 3. Get all files but exclude heavy folders and copy preserving structure
Get-ChildItem -Path $sourcePath -Recurse -File | Where-Object {
    $_.FullName -notmatch "\\node_modules\\" -and 
    $_.FullName -notmatch "\\dist\\" -and 
    $_.FullName -notmatch "\\\.git\\" -and 
    $_.FullName -notmatch "\\\.vs\\" -and 
    $_.FullName -notmatch "\\\.angular\\" -and
    $_.FullName -notmatch "\\\.zip$" -and
    $_.FullName -notmatch "\\\.png$" -and
    $_.FullName -notmatch "\\\.jpg$" -and
    $_.FullName -notmatch "\\\.jpeg$" -and
    $_.FullName -notmatch "\\\.gif$"
} | ForEach-Object {
    $relPath = $_.FullName.Substring($sourcePath.Length + 1)
    $destFile = Join-Path $stagingPath $relPath
    $destDir = Split-Path $destFile -Parent
    if (!(Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item $_.FullName -Destination $destFile -Force
}

# 4. Create zip of the staging folder
Write-Host "Creating optimized frontend zip file... This might take a minute."
Compress-Archive -Path "$stagingPath\*" -DestinationPath $zipPath -Force

# 5. Clean staging directory
Write-Host "Cleaning up staging files..."
Remove-Item $stagingPath -Recurse -Force

Write-Host "Frontend zip created successfully at $zipPath"
