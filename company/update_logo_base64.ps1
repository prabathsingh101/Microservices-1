$imagePath = "c:\Projects\ElectricApps\src\assets\images\raja_pharmacy_logo.png"
if (-not (Test-Path $imagePath)) {
    Write-Error "Logo file not found at $imagePath"
    exit 1
}

$bytes = [System.IO.File]::ReadAllBytes($imagePath)
$base64 = [System.Convert]::ToBase64String($bytes)
$dataUri = "data:image/png;base64," + $base64

$connectionString = "Server=187.127.146.1;Database=CompanyDb;user id=sa;password=Anand@raj12345;TrustServerCertificate=True"
$query = "UPDATE CompanyProfiles SET LogoUrl = @LogoUrl WHERE Id = 'ABC1BF71-71E1-4AE3-B3A4-E0E6EDABEF43'"

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
$null = $command.Parameters.AddWithValue("@LogoUrl", $dataUri)

try {
    $connection.Open()
    $rowsAffected = $command.ExecuteNonQuery()
    $connection.Close()
    Write-Output "Logo updated successfully in production DB. Rows affected: $rowsAffected"
}
catch {
    Write-Error $_.Exception.Message
    if ($connection.State -eq "Open") {
        $connection.Close()
    }
    exit 1
}
