$files = Get-ChildItem -Path "c:\Projects\Decode\Microservices\Inventory\Inventory.Infrastructure\Repositories\*Repository.cs"

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    # Look for patterns like: dbWarehouses.ToDictionary(w => w.Name.ToLower().Trim(), w => w)
    # Replace with: dbWarehouses.GroupBy(w => w.Name.ToLower().Trim()).ToDictionary(g => g.Key, g => g.First())
    $pattern = '\.ToDictionary\((.*?)\s*=>\s*(.*?)\.ToLower\(\)\.Trim\(\),\s*(.*?)\s*=>\s*(.*?)\)'
    $replacement = '.GroupBy($1 => $2.ToLower().Trim()).ToDictionary(g => g.Key, g => g.First())'
    
    $newContent = $content -replace $pattern, $replacement

    # Also look for ToDictionaryAsync for Category, Rack, Warehouse in case they exist:
    $pattern2 = '\.ToDictionaryAsync\((.*?)\s*=>\s*(.*?)\.ToLower\(\)\.Trim\(\),\s*(.*?)\s*=>\s*(.*?)\)'
    $replacement2 = '.ToListAsync(); $newVar = xyz.GroupBy($1 => $2.ToLower().Trim()).ToDictionary(g => g.Key, g => g.First())'
    # Actually I shouldn't automate ToDictionaryAsync to ToListAsync because it's a bit more complex. 
    # Let's just fix ToDictionary for now, because the error trace specifically showed:
    # `at System.Linq.Enumerable.ToDictionary` NOT `ToDictionaryAsync`!

    if ($content -ne $newContent) {
        Set-Content -Path $file.FullName -Value $newContent
        Write-Host "Patched $($file.Name)"
    }
}
