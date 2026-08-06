# Script to automate Azure VM deployment in South India
$rg = "rg-microservices-southindia"
$vmName = "vm-microservices-prod"
$location = "southindia"
$azCmd = "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd"

$skus = @("Standard_B2ats_v2", "Standard_B2s", "Standard_B2ms", "Standard_D2s_v3", "Standard_B1ms")
$vmCreated = $false

foreach ($sku in $skus) {
    Write-Host "Attempting to create VM with size $sku in $location..."
    $result = & $azCmd vm create --resource-group $rg --name $vmName --image Ubuntu2204 --size $sku --admin-username azureuser --generate-ssh-keys --location $location 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Successfully created VM with size $sku!"
        $vmCreated = $true
        break
    } else {
        Write-Host "SKU $sku failed: $result"
    }
}

if (-not $vmCreated) {
    Write-Error "Failed to create VM with all attempted SKUs in $location."
    exit 1
}

Write-Host "Fetching VM Public IP..."
$ipJson = & $azCmd vm list-ip-addresses --resource-group $rg --name $vmName --output json | ConvertFrom-Json
$publicIp = $ipJson[0].virtualMachine.network.publicIpAddresses[0].ipAddress

Write-Host "=========================================="
Write-Host "Azure VM Created Successfully!"
Write-Host "Public IP Address: $publicIp"
Write-Host "=========================================="

Write-Host "Opening Ports 80, 4201, 5000, 1433, 15672..."
& $azCmd vm open-port --resource-group $rg --name $vmName --port 80 4201 5000 1433 15672 --priority 1000

Write-Host "VM Setup & Networking Completed Successfully!"
