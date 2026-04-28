$controllers = @("CategoriesController.cs", "SubcategoriesController.cs", "RacksController.cs", "UnitsController.cs")
$base_path = "c:\Projects\Decode\Microservices\Inventory\Inventory.API\Controllers"

foreach ($ctrl in $controllers) {
    $path = Join-Path $base_path $ctrl
    $content = Get-Content $path -Raw

    # 1. Add [FromForm]
    $content = $content -replace 'public async Task<IActionResult> UploadExcel\(IFormFile file\)', 'public async Task<IActionResult> UploadExcel([FromForm] IFormFile file)'

    # 2. Extract CompanyId and BranchId from header
    $pattern = '(?s)var branchId = User\.Claims\.FirstOrDefault\(c => c\.Type\.Equals\("BranchId", StringComparison\.OrdinalIgnoreCase\)\)\?\.Value;\s*if \(\!Guid\.TryParse\(companyIdClaim, out var companyId\)\)'
    $replacement = 'var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            if (!Guid.TryParse(companyIdHeader, out var companyId) && !Guid.TryParse(companyIdClaim, out companyId))'
    $content = $content -replace $pattern, $replacement

    # 3. Use finalBranchId in result
    $pattern2 = '(?s)var result = await _(.*?)Repository\.Upload(.*?)Async\(file, companyId, branchId\);'
    $replacement2 = 'var branchIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();
            var finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            var result = await _$1Repository.Upload$2Async(file, companyId, finalBranchId);'
    $content = $content -replace $pattern2, $replacement2

    Set-Content -Path $path -Value $content
}

Write-Host "Done patching controllers!"
