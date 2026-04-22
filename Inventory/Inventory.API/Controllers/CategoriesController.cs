using Inventory.API.Common;
using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Categories.Commands.DeleteCategory;
using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Categories.Queries.GetCategoryById;
using Inventory.Application.Common.Models;
using Inventory.Application.Subcategories.Queries.GetSubcategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Create(CreateCategoryCommand command)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId };
            }

            var id = await _mediator.Send(command);
            return Ok(ApiResponse<Guid>.Ok(id, "Categories created successfully"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Update(Guid id, UpdateCategoryCommand command)
        {
            if (id != command.Id)
                return BadRequest(ApiResponse<string>.Fail("Id mismatch"));

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId };
            }

            var result = await _mediator.Send(command);
            return Ok(ApiResponse<Guid>.Ok(result, "Category updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteCategoryCommand(id));
            return Ok(new { success = true, message = "Category deleted successfully" });
        }

        [HttpPost("bulk-delete")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids)
        {
            await _mediator.Send(new BulkDeleteCategoriesCommand(ids));
            return Ok(new { success = true, message = "Category deleted successfully" });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetCategoryByIdQuery(id));
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("paged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> GetCategories([FromBody] GridRequest query)
        {
            var result = await _mediator.Send(new GetCategoriesPagedQuery(query));
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetCategoriesQuery());
            return Ok(result);
        }

        [HttpPost("upload-excel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please upload an excel file.");

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (!Guid.TryParse(companyIdClaim, out var companyId))
            {
                 return BadRequest("Invalid or missing CompanyId in your session.");
            }

            var result = await _categoryRepository.UploadCategoriesAsync(file, companyId);
            return Ok(new { message = $"{result.successCount} Categories uploaded successfully.", errors = result.errors });
        }

        [HttpGet("check-duplicate")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return Ok(new { exists = false });

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (!Guid.TryParse(companyIdClaim, out var companyId)) return BadRequest("Invalid session: CompanyId not found");

            var exists = await _categoryRepository.ExistsByNameAsync(name, companyId, excludeId);
            return Ok(new { exists = exists, message = exists ? $"The category name '{name}' is already used by another active category." : string.Empty });
        }

        [HttpGet("download-template")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Categories");
                var headers = new string[] { "CategoryCode", "CategoryName", "DefaultGst", "Description" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightSteelBlue;
                }

                var samples = new List<string[]>
                {
                    new string[] { "CAT001", "Wires & Cables", "18", "Electrical wiring and cables" },
                    new string[] { "CAT002", "Switches & Sockets", "18", "Modular switches and sockets" },
                    new string[] { "CAT003", "Lighting", "12", "Indoor and outdoor lighting" },
                    new string[] { "CAT004", "Fans", "18", "Ceiling, wall and exhaust fans" },
                    new string[] { "CAT005", "MCB & Distribution", "18", "Circuit breakers and DB boxes" },
                    new string[] { "CAT006", "Smart Electrical", "18", "Smart automation and high-end electrical gadgets" },
                    new string[] { "CAT007", "Power Backup", "12", "UPS, Inverters and batteries" },
                    new string[] { "CAT008", "Appliances", "18", "Kitchen and home electrical appliances" },
                    new string[] { "CAT026", "Beverages", "18", "Soft drinks, juices and energy drinks" },
                    new string[] { "CAT027", "Snacks & Branded Foods", "12", "Chips, noodles and packed snacks" },
                    new string[] { "CAT028", "Pulses & Dals", "5", "Organic and packed pulses" },
                    new string[] { "CAT029", "Atta & Flours", "5", "Wheat flour and specialty flours" },
                    new string[] { "CAT030", "Rice & Rice Products", "5", "Basmati and non-basmati rice" },
                    new string[] { "CAT031", "Spices & Masalas", "5", "Ground and whole spices" },
                    new string[] { "CAT032", "Cooking Oils & Ghee", "12", "Refined and cold pressed oils" },
                    new string[] { "CAT033", "Salt & Sugar", "5", "Refined sugar and specialized salts" },
                    new string[] { "CAT034", "Dairy Products", "5", "Milk, paneer and curd" },
                    new string[] { "CAT035", "Personal Care", "18", "Body lotions and bath products" },
                    new string[] { "CAT038", "Biscuits & Cookies", "18", "Assorted biscuits and cookies" },
                    new string[] { "CAT039", "Breakfast Cereals", "12", "Oats, cornflakes and muesli" },
                    new string[] { "CAT045", "Beauty & Cosmetic", "28", "Premium skincare and makeup" },
                    new string[] { "CAT050", "Tea & Coffee", "12", "Premium tea leaves and coffee beans" },
                    new string[] { "CAT051", "Bread & Bakery", "5", "Fresh breads, buns and cakes" },
                    new string[] { "CAT052", "Frozen Foods", "18", "Ready to fry nuggets, peas and meals" },
                    new string[] { "CAT082", "Sweets & Mithai", "12", "Packed gulab jamun and rasgulla" },
                    new string[] { "CAT086", "Instant Mixes", "12", "Idli, dosa and gulab jamun mixes" },
                    new string[] { "CAT090", "Kirana General", "5", "Daily household grocery essentials" },
                    new string[] { "CAT091", "Cleaning Supplies", "18", "Detergents and floor cleaners" }
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
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Category_Template.xlsx");
                }
            }
        }
    }
}
