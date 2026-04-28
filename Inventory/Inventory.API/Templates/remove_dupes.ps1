$filePath = 'c:\Projects\Decode\Microservices\Inventory\Inventory.API\Templates\product_template.csv'
$lines = Get-Content $filePath
$headers = $lines[0]
$rows = $lines | Select-Object -Skip 1 | Where-Object { $_.Trim() -ne '' }
$products = @{}
$uniqueRows = @($headers)

foreach ($r in $rows) {
    $cols = $r -split ','
    if ($cols.Length -gt 2) {
        $pName = $cols[2].Trim()
        if (-not $products.ContainsKey($pName)) {
            $products[$pName] = $true
            $uniqueRows += $r
        } else {
            Write-Host "Duplicate found and removed: $pName"
        }
    }
}

$uniqueRows | Set-Content $filePath
Write-Host "Done!"
