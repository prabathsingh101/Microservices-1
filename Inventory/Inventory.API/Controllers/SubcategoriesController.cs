using Inventory.Application.Subcategories.Commands.CreateSubcategory;
using Inventory.Application.Subcategories.Commands.Delete;
using Inventory.Application.Subcategories.Commands.UpdateSubcategory;
using Inventory.Application.Subcategories.Queries.GetSubcategories;
using Inventory.Application.Subcategories.Queries.GetSubcategoriesByCategory;
using Inventory.Application.Subcategories.Queries.GetSubcategoryById;
using Inventory.Application.Subcategories.Queries.Searching;
using Inventory.Application.Subcategories.DTOs;
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
    [Route("api/subcategories")]
    public sealed class SubcategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ISubcategoryRepository _repository;

        public SubcategoriesController(IMediator mediator, ISubcategoryRepository repository)
        {
            _mediator = mediator;
            _repository = repository;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Create(CreateSubcategoryCommand command)
        {
            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
            var branchId = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId, BranchId = command.BranchId ?? branchId };
            }

            var id = await _mediator.Send(command);
            return Ok(ApiResponse<Guid>.Ok(id, "Sub category created successfully"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Update(Guid id, UpdateSubcategoryCommand command)
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
            return Ok(ApiResponse<Guid>.Ok(result, "Subcategory updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(new DeleteSubcategoryCommand(id));
            return Ok(ApiResponse<Guid>.Ok(result, "Subcategory deleted successfully"));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetSubcategoryByIdQuery(id));
            if (result == null) 
            {
                // Lenient check if not found with branch filter
                var all = await _mediator.Send(new GetSubcategoriesQuery());
                result = all.FirstOrDefault(x => x.Id == id);
            }
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetSubcategoriesQuery());
            return Ok(result);
        }

        [HttpGet("count")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetCount()
        {
            var result = await _mediator.Send(new GetSubcategoriesQuery());
            return Ok(result.Count());
        }

        [HttpGet("by-category/{categoryId:guid}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetByCategory(Guid categoryId)
        {
            var result = await _mediator.Send(new GetSubcategoriesByCategoryQuery(categoryId));
            return Ok(result);
        }

        [HttpPost("paged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetPaged([FromBody] GridRequest request)
        {
            var result = await _mediator.Send(new GetSubcategoriesPagedQuery(request));
            return Ok(result);
        }

        [HttpPost("bulk-delete")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids)
        {
            await _mediator.Send(new BulkDeleteSubCategoriesCommand(ids));
            return Ok(ApiResponse<bool>.Ok(true, "Subcategories deleted successfully"));
        }

        [HttpPost("upload-excel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(ApiResponse<string>.Fail("Please upload an excel file."));

            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
            var branchId = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
            if (!Guid.TryParse(companyIdClaim, out var companyId))
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid or missing CompanyId in your session."));
            }

            var result = await _repository.UploadSubcategoriesAsync(file, companyId, branchId);
            return Ok(new { message = $"{result.successCount} New Subcategories saved and {result.updateCount} Subcategories updated successfully.", errors = result.errors });
        }

        [HttpGet("check-duplicate")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return Ok(new { exists = false });
            
            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
            if (!Guid.TryParse(companyIdClaim, out var companyId)) return BadRequest(ApiResponse<string>.Fail("Invalid session"));

            var exists = await _repository.ExistsByNameAsync(name, companyId, excludeId);
            return Ok(new { exists });
        }

        [HttpGet("download-template")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public IActionResult DownloadTemplate()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Templates", "subcategory_template.csv");
            if (!System.IO.File.Exists(filePath)) 
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "subcategory_template.csv");
            }
            if (!System.IO.File.Exists(filePath)) return NotFound("Template file not found.");

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Subcategories");
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
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Subcategory_Template.xlsx");
                }
            }
        }
    }
}
