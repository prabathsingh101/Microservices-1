using Inventory.API.Common;
using Inventory.Application.Common.Models;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Application.Products.Queries.GetProductById;
using Inventory.Application.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly Inventory.Application.Common.Interfaces.IProductRepository _productRepository;
        private readonly Inventory.Application.Common.Interfaces.ICurrentUserService _currentUserService;

        [HttpGet("debug-company")]
        public IActionResult GetDebugCompany()
        {
            var companyId = _currentUserService.CompanyId;
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(new { companyId, claims });
        }

        [HttpGet]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetProductsQuery());
            return Ok(result);
        }

        public ProductsController(IMediator mediator, 
            Inventory.Application.Common.Interfaces.IProductRepository productRepository,
            Inventory.Application.Common.Interfaces.ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _productRepository = productRepository;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Create(CreateProductCommand command)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId };
            }

            var id = await _mediator.Send(command);
            return Ok(ApiResponse<Guid>.Ok(id, "Product created successfully"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Update(Guid id, UpdateProductCommand command)
        {
            if (id != command.Id)
                return BadRequest(ApiResponse<string>.Fail("Id mismatch"));

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId };
            }

            var result = await _mediator.Send(command);
            return Ok(ApiResponse<Guid>.Ok(result, "Product updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteProductCommand(id));
            return Ok(new { success = true, message = "Product deleted successfully" });
        }

        [HttpPost("bulk-delete")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids)
        {
            // SMART FIX: Using correct singular command name
            await _mediator.Send(new BulkDeleteProductCommand(ids));
            return Ok(new { success = true, message = "Products deleted successfully" });
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetProductByIdQuery(id));
            return Ok(result);
        }

        [HttpPost("paged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetPaged([FromBody] GridRequest request)
        {
            var result = await _mediator.Send(new GetProductsPagedQuery(request));
            return Ok(result);
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

            var result = await _productRepository.UploadProductsAsync(file, companyId);
            int totalAffected = result.successCount + result.updateCount;
            return Ok(new { message = $"{totalAffected} Products processed successfully.", errors = result.errors });
        }

        [HttpGet("check-duplicate")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return Ok(new { exists = false });

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (!Guid.TryParse(companyIdClaim, out var companyId)) return BadRequest("Invalid session: CompanyId not found");

            var exists = await _productRepository.ExistsByNameAsync(name, companyId, excludeId);
            return Ok(new { exists = exists, message = exists ? $"The product name '{name}' is already used by another active product." : string.Empty });
        }

        [HttpGet("download-template")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public IActionResult DownloadTemplate()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "product_template.csv");
            if (!System.IO.File.Exists(filePath)) return NotFound("Template file not found.");

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Products");
                var csvLines = System.IO.File.ReadAllLines(filePath);
                
                if (csvLines.Length > 0)
                {
                    // 1. Process Header
                    var headers = csvLines[0].Split(',');
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightSteelBlue;
                    }

                    // 2. Process Data Rows
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
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Product_Template.xlsx");
                }
            }
        }
    }
}
