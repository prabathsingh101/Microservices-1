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
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId };
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

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId };
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
            return Ok(new { success = true, message = "Subcategories deleted successfully" });
        }

        [HttpPost("upload-excel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please upload an excel file.");

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (!Guid.TryParse(companyIdClaim, out var companyId))
            {
                return BadRequest("Invalid or missing CompanyId in your session.");
            }

            var result = await _repository.UploadSubcategoriesAsync(file, companyId);
            return Ok(new { message = $"{result.successCount} Subcategories uploaded successfully.", errors = result.errors });
        }

        [HttpGet("check-duplicate")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return Ok(new { exists = false });

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (!Guid.TryParse(companyIdClaim, out var companyId)) return BadRequest("Invalid session: CompanyId not found");

            var exists = await _repository.ExistsByNameAsync(name, companyId, excludeId);
            return Ok(new { exists = exists, message = exists ? $"The subcategory name '{name}' is already used by another active subcategory." : string.Empty });
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

                var samples = new List<string[]>
                {
                    new string[] { "SUB001", "Wires & Cables", "Copper Wire", "18", "Copper electrical wire" },
                    new string[] { "SUB002", "Wires & Cables", "Aluminium Wire", "18", "Aluminium wiring" },
                    new string[] { "SUB004", "Switches & Sockets", "Modular Switch", "18", "Designer switches" },
                    new string[] { "SUB007", "Lighting", "LED Bulb", "12", "Energy efficient bulb" },
                    new string[] { "SUB008", "Lighting", "Tube Light", "12", "Energy efficient tube" },
                    new string[] { "SUB011", "Fans", "Ceiling Fan", "18", "Ceiling mounted fan" },
                    new string[] { "SUB015", "Smart Electrical", "Fans", "18", "Smart high-speed fans" },
                    new string[] { "SUB016", "Smart Electrical", "Lights", "12", "Smart LED lighting solutions" },
                    new string[] { "SUB017", "Smart Electrical", "Switches", "18", "WiFi modular switches" },
                    new string[] { "SUB018", "Smart Electrical", "Wires", "18", "Premium grade smart wiring" },
                    new string[] { "SUB019", "Smart Electrical", "Appliances", "18", "Smart kitchen appliances" },
                    new string[] { "SUB020", "Smart Electrical", "Protection", "18", "Voltage protectors and smart MCBs" },
                    new string[] { "SUB021", "Smart Electrical", "Cables", "18", "Heavy duty data and power cables" },
                    new string[] { "SUB022", "Smart Electrical", "Tools", "12", "Smart precision tools" },
                    new string[] { "SUB023", "Smart Electrical", "Batteries", "18", "Deep cycle and lithium batteries" },
                    new string[] { "SUB024", "Smart Electrical", "Fittings", "18", "Smart conduit and lighting fittings" },
                    new string[] { "SUB031", "Beverages", "Soft Drink", "18", "Cola and lemon drinks" },
                    new string[] { "SUB032", "Beverages", "Fruit Juice", "12", "Fresh and packed fruit juices" },
                    new string[] { "SUB034", "Snacks & Branded Foods", "Potato Chips", "12", "Crispy wafers" },
                    new string[] { "SUB037", "Pulses & Dals", "Moong Dal", "5", "Yellow moong split" },
                    new string[] { "SUB040", "Atta & Flours", "Wheat Atta", "5", "Chakki fresh flour" },
                    new string[] { "SUB043", "Rice & Rice Products", "Basmati Rice", "5", "Premium long grain" },
                    new string[] { "SUB046", "Spices & Masalas", "Turmeric Powder", "5", "Ground haldi" },
                    new string[] { "SUB049", "Cooking Oils & Ghee", "Mustard Oil", "12", "Pure kachi ghani" },
                    new string[] { "SUB052", "Salt & Sugar", "White Sugar", "5", "Refined crystalline sugar" },
                    new string[] { "SUB055", "Dairy Products", "Fresh Paneer", "5", "Fresh cottage cheese" },
                    new string[] { "SUB090", "Kirana General", "Table Salt", "5", "Refined iodized salt" },
                    new string[] { "SUB091", "Cleaning Supplies", "Detergent", "18", "Wash detergent" }
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
