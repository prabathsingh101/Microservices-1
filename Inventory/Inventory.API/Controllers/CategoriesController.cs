using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Categories.Commands.DeleteCategory;
using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Categories.Queries.GetCategoryById;
using Inventory.Application.Categories.DTOs;
using Inventory.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.API.Common;
using Inventory.Application.Common.Interfaces;
using ClosedXML.Excel;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public sealed class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesController(IMediator mediator, ICategoryRepository categoryRepository)
        {
            _mediator = mediator;
            _categoryRepository = categoryRepository;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin, Salesman")]
        public async Task<IActionResult> Create(CreateCategoryCommand command)
        {
            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
            var branchId = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId, BranchId = command.BranchId ?? branchId };
            }

            var id = await _mediator.Send(command);
            return Ok(ApiResponse<Guid>.Ok(id, "Categories created successfully"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> Update(Guid id, UpdateCategoryCommand command)
        {
            if (id != command.Id)
                return BadRequest(ApiResponse<string>.Fail("Id mismatch"));

            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
            var branchId = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId, BranchId = command.BranchId ?? branchId };
            }

            var result = await _mediator.Send(command);
            return Ok(ApiResponse<Guid>.Ok(result, "Category updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteCategoryCommand(id));
            return Ok(ApiResponse<bool>.Ok(true, "Category deleted successfully"));
        }

        [HttpPost("paged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin, Salesman")]
        public async Task<IActionResult> GetCategories([FromBody] GridRequest query)
        {
            var result = await _mediator.Send(new GetCategoriesPagedQuery(query));
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetCategoriesQuery());
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetCategoryByIdQuery(id));
            return Ok(result);
        }

        [HttpPost("upload-excel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin, Salesman")]
        public async Task<IActionResult> UploadExcel([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(ApiResponse<string>.Fail("Please upload an excel file."));

            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (!Guid.TryParse(companyIdHeader, out var companyId) && !Guid.TryParse(companyIdClaim, out companyId))
            {
                 return BadRequest(ApiResponse<string>.Fail("Invalid or missing CompanyId in your session."));
            }

            var branchIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();
            
            var finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            var result = await _categoryRepository.UploadCategoriesAsync(file, companyId, finalBranchId);
            return Ok(new { message = $"{result.successCount} New Categories saved and {result.updateCount} Categories updated successfully.", errors = result.errors });
        }

        [HttpGet("check-duplicate")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return Ok(new { exists = false });
            
            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (!Guid.TryParse(companyIdHeader, out var companyId) && !Guid.TryParse(companyIdClaim, out companyId)) 
                return BadRequest(ApiResponse<string>.Fail("Invalid session"));

            var exists = await _categoryRepository.ExistsByNameAsync(name, companyId, excludeId);
            return Ok(new { exists });
        }

        [HttpGet("download-template")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public IActionResult DownloadTemplate()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Templates", "category_template.csv");
            if (!System.IO.File.Exists(filePath)) 
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "category_template.csv");
            }
            if (!System.IO.File.Exists(filePath)) return NotFound("Template file not found.");

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Categories");
                var csvLines = System.IO.File.ReadAllLines(filePath);
                
                if (csvLines.Length > 0)
                {
                    var headers = csvLines[0].Split(',');
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightCyan;
                    }

                    for (int r = 1; r < csvLines.Length; r++)
                    {
                        if (string.IsNullOrWhiteSpace(csvLines[r])) continue;
                        var cells = csvLines[r].Split(',');
                        for (int c = 0; c < cells.Length; c++)
                        {
                            worksheet.Cell(r + 1, c + 1).Value = cells[c];
                        }
                    }
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Category_Template.xlsx");
                }
            }
        }
    }
}
