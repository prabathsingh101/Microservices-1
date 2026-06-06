$sourcePath = "C:\Projects\Decode\Microservices"
$stagingPath = "C:\Projects\Decode\Microservices_staging"
$zipPath = "C:\Projects\Decode\Microservices\Microservices.zip"

# 1. Clean existing zip and staging if exists
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
if (Test-Path $stagingPath) { Remove-Item $stagingPath -Recurse -Force }

# 2. Create staging directory
New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null

Write-Host "Gathering and copying optimized backend files to staging..."
# 3. Get all files but exclude heavy folders and copy preserving structure
Get-ChildItem -Path $sourcePath -Recurse -File | Where-Object {
    $_.FullName -notmatch "\\bin\\" -and 
    $_.FullName -notmatch "\\obj\\" -and 
    $_.FullName -notmatch "\\publish\\" -and 
    $_.FullName -notmatch "\\\.git\\" -and 
    $_.FullName -notmatch "\\\.vs\\" -and 
    $_.FullName -notmatch "\\\.github\\" -and
    $_.FullName -notmatch "\\node_modules\\" -and
    $_.FullName -notmatch "\\\.zip$" -and
    $_.FullName -notmatch "\\\.dll$" -and
    $_.FullName -notmatch "\\\.exe$" -and
    $_.FullName -notmatch "\\\.pdb$" -and
    $_.FullName -notmatch "\\\.so$" -and
    $_.FullName -notmatch "\\optimize_zip\.ps1$" -and
    $_.FullName -notmatch "\\optimize_frontend_zip\.ps1$"
} | ForEach-Object {
    $relPath = $_.FullName.Substring($sourcePath.Length + 1)
    $destFile = Join-Path $stagingPath $relPath
    $destDir = Split-Path $destFile -Parent
    if (!(Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item $_.FullName -Destination $destFile -Force
}

# 4. Create zip of the staging folder (this preserves folder structure perfectly!)
Write-Host "Creating optimized zip file... This might take a minute."
Compress-Archive -Path "$stagingPath\*" -DestinationPath $zipPath -Force

# 5. Clean staging directory
Write-Host "Cleaning up staging files..."
Remove-Item $stagingPath -Recurse -Force

Write-Host "Zip created successfully at $zipPath"
