# PowerShell script - compact deploy package for Azure (fresh clean build)
$targetZip = "C:\Projects\deploy_package.zip"
$tempDir   = "C:\Projects\deploy_temp"

# Cleanup old files
if (Test-Path $tempDir) { Remove-Item -Recurse -Force $tempDir }
if (Test-Path $targetZip) { Remove-Item -Force $targetZip }

# ── ElectricApps: only dist browser + nginx.conf + Dockerfile.prod ──
Write-Host "Packaging Angular dist..."
$uiDest = "$tempDir\ElectricApps"
New-Item -ItemType Directory -Path "$uiDest\dist\EnterpriseERP" -Force | Out-Null
Copy-Item "C:\Projects\ElectricApps\dist\EnterpriseERP\browser" -Destination "$uiDest\dist\EnterpriseERP\browser" -Recurse -Force
Copy-Item "C:\Projects\ElectricApps\nginx.conf"      -Destination "$uiDest\nginx.conf"      -Force
Copy-Item "C:\Projects\ElectricApps\Dockerfile.prod" -Destination "$uiDest\Dockerfile.prod" -Force

# ── Microservices: source only (no bin/obj/git/node_modules/.vs/.angular) ──
Write-Host "Packaging Microservices..."
$msDest = "$tempDir\Decode\Microservices"
New-Item -ItemType Directory -Path $msDest -Force | Out-Null

$excludeDirs = @("bin","obj",".git",".vs","node_modules",".angular","deploy_temp","logs","TestResults")
robocopy "C:\Projects\Decode\Microservices" $msDest /E /XD $excludeDirs /XF "*.log" "deploy_package.zip" "*.user" /NFL /NDL /NJH /NJS | Out-Null

# ── Compress ─────────────────────────────────────────────────────────
Write-Host "Compressing..."
Compress-Archive -Path "$tempDir\*" -DestinationPath $targetZip -CompressionLevel Optimal -Force

Remove-Item -Recurse -Force $tempDir

$sizeMB = [math]::Round((Get-Item $targetZip).Length / 1MB, 1)
Write-Host "DONE! Package: $targetZip ($sizeMB MB)"
