using Inventory.API.Common;
using Inventory.Application.Common.Models;
using Inventory.Application.Subcategories.Commands.CreateSubcategory;
using Inventory.Application.Subcategories.Commands.Delete;
using Inventory.Application.Subcategories.Commands.UpdateSubcategory;
using Inventory.Application.Subcategories.Queries.GetSubcategories;
using Inventory.Application.Subcategories.Queries.GetSubcategoryById;
using Inventory.Application.Subcategories.Queries.Searching;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/subcategories")]
    [ApiController]
    public class SubcategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly Inventory.Application.Common.Interfaces.ISubcategoryRepository _repository;

        public SubcategoriesController(IMediator mediator, Inventory.Application.Common.Interfaces.ISubcategoryRepository repository)
        {
            _mediator = mediator;
            _repository = repository;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Create(CreateSubcategoryCommand command)
        {
            // 🚀 SMART INJECTION: Get CompanyId from Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId };
            }

            var id = await _mediator.Send(command);
            return Ok(
            ApiResponse<Guid>.Ok(
                id,
                "Sub category created successfully"
            ));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Update(
          Guid id,
          UpdateSubcategoryCommand command)
        {
            if (id != command.Id)
                return BadRequest(
                    ApiResponse<string>.Fail("Id mismatch"));

            // 🚀 SMART INJECTION: Get CompanyId from Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId };
            }

            var result = await _mediator.Send(command);

            return Ok(
                ApiResponse<Guid>.Ok(
                    result,
                    "Subcategory updated successfully"
                )
            );
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(
                new DeleteSubcategoryCommand(id));

            return Ok(
                ApiResponse<Guid>.Ok(
                    result,
                    "Subcategory deleted successfully"
                )
            );
        }


        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetSubcategoryByIdQuery(id));
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetSubcategoriesQuery());
            return Ok(result);
        }

        [HttpGet("by-category/{categoryId}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetByCategory(Guid categoryId)
        {
            var result = await _mediator.Send(
                new GetSubcategoriesByCategoryQuery(categoryId));

            return Ok(result);
        }

        [HttpPost("paged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetPaged(
            [FromBody] GridRequest request)
        {
            var result = await _mediator.Send(
                new GetSubcategoriesPagedQuery(request)
            );

            return Ok(result);
        }

        [HttpPost("bulk-delete")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids)
        {
            await _mediator.Send(new BulkDeleteSubCategoriesCommand(ids));

            return Ok(new
            {
                success = true,
                message = "Category deleted successfully"
            });
        }

        [HttpPost("upload-excel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please upload an excel file.");

            // 🚀 SMART LOGIC: Get CompanyId from Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (!Guid.TryParse(companyIdClaim, out var companyId))
            {
                return BadRequest("Invalid or missing CompanyId in your session.");
            }

            var result = await _repository.UploadSubcategoriesAsync(file, companyId);

            return Ok(new
            {
                Message = $"{result.successCount} Subcategories uploaded successfully.",
                Errors = result.errors
            });
        }

        [HttpGet("check-duplicate")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Ok(new { exists = false });
            }

            // 🚀 SMART LOGIC: Scope check by CompanyId from Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (!Guid.TryParse(companyIdClaim, out var companyId))
            {
                return BadRequest("Invalid session: CompanyId not found");
            }

            var exists = await _repository.ExistsByNameAsync(name, companyId, excludeId);

            return Ok(new
            {
                exists = exists,
                message = exists ? $"The subcategory name '{name}' is already used by another active subcategory." : string.Empty
            });
        }
        [HttpGet("download-template")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Subcategories");
                var headers = new string[] { "SubcategoryCode", "CategoryName", "SubcategoryName", "DefaultGst", "Description" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightSteelBlue;
                }

                // Original Sample Data
                var samples = new List<string[]>
                {
                    new string[] { "SUB001", "Wires & Cables", "Copper Wire", "18", "Copper electrical wire" },
                    new string[] { "SUB002", "Wires & Cables", "Aluminium Wire", "18", "Aluminium wiring" },
                    new string[] { "SUB003", "Lighting", "LED Bulb", "12", "Energy efficient bulb" }
                };

                for (int r = 0; r < samples.Count; r++)
                {
                    for (int c = 0; c < samples[r].Length; c++)
                    {
                        worksheet.Cell(r + 2, c + 1).Value = samples[r][c];
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
