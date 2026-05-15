$sourcePath = "C:\Projects\Decode\Microservices"
$zipPath = "C:\Projects\Decode\Microservices\Microservices.zip"

# 1. Clean existing zip if exists
if (Test-Path $zipPath) { Remove-Item $zipPath }

# 2. Get all files but exclude heavy folders
$files = Get-ChildItem -Path $sourcePath -Recurse | Where-Object {
    $_.FullName -notmatch "\\bin\\" -and 
    $_.FullName -notmatch "\\obj\\" -and 
    $_.FullName -notmatch "\\\.git\\" -and 
    $_.FullName -notmatch "\\\.vs\\" -and 
    $_.FullName -notmatch "\\\.github\\" -and
    $_.FullName -notmatch "\\node_modules\\" -and
    $_.FullName -notmatch "\\Microservices\.zip$" -and
    $_.FullName -notmatch "\\optimize_zip\.ps1$"
}

# 3. Create zip
Write-Host "Creating optimized zip... This might take a minute."
$files | Compress-Archive -DestinationPath $zipPath -Update

Write-Host "Zip created successfully at $zipPath"
