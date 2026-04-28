import os
import re

controllers = ["CategoriesController.cs", "SubcategoriesController.cs", "RacksController.cs", "UnitsController.cs"]
base_path = r"c:\Projects\Decode\Microservices\Inventory\Inventory.API\Controllers"

for ctrl in controllers:
    path = os.path.join(base_path, ctrl)
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    # 1. Add [FromForm]
    content = content.replace("public async Task<IActionResult> UploadExcel(IFormFile file)", "public async Task<IActionResult> UploadExcel([FromForm] IFormFile file)")

    # 2. Extract CompanyId and BranchId from header
    pattern = r'var branchId = User\.Claims\.FirstOrDefault\(c => c\.Type\.Equals\("BranchId", StringComparison\.OrdinalIgnoreCase\)\)\?\.Value;\s*if \(\!Guid\.TryParse\(companyIdClaim, out var companyId\)\)'
    replacement = r'''var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            if (!Guid.TryParse(companyIdHeader, out var companyId) && !Guid.TryParse(companyIdClaim, out companyId))'''
    
    content = re.sub(pattern, replacement, content)
    
    # 3. Use finalBranchId in result
    pattern2 = r'var result = await _(.*?)Repository\.Upload(.*?)Async\(file, companyId, branchId\);'
    replacement2 = r'''var branchIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();
            var finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            var result = await _\1Repository.Upload\2Async(file, companyId, finalBranchId);'''
    
    content = re.sub(pattern2, replacement2, content)

    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

print("Done patching controllers!")
