using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Application.Products.Queries.GetProducts;
using Inventory.Application.Products.Queries.GetProductById;
using Inventory.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.API.Common;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.DTOs;
using ClosedXML.Excel;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public sealed class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IProductRepository _productRepository;
        private readonly ICurrentUserService _currentUserService;

        public ProductsController(IMediator mediator, 
            Inventory.Application.Common.Interfaces.IProductRepository productRepository,
            Inventory.Application.Common.Interfaces.ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _productRepository = productRepository;
            _currentUserService = currentUserService;
        }

        [HttpPost("paged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetPaged([FromBody] GridRequest request)
        {
            var result = await _mediator.Send(new GetProductsPagedQuery(request));
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetProductsQuery());
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetProductByIdQuery(id));
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> Create(CreateProductCommand command)
        {
            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                var branchIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
                var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();
                
                var finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                    ? branchIdHeader 
                    : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

                command = command with { CompanyId = companyId, BranchId = command.BranchId ?? finalBranchId };
            }

            var id = await _mediator.Send(command);
            return Ok(ApiResponse<Guid>.Ok(id, "Product created successfully"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> Update(Guid id, UpdateProductCommand command)
        {
            if (id != command.Id)
                return BadRequest(ApiResponse<string>.Fail("Id mismatch"));

            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                var branchIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
                var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();
                
                var finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                    ? branchIdHeader 
                    : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

                command = command with { CompanyId = companyId, BranchId = command.BranchId ?? finalBranchId };
            }

            var result = await _mediator.Send(command);
            return Ok(ApiResponse<Guid>.Ok(result, "Product updated successfully"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteProductCommand(id));
            return Ok(ApiResponse<bool>.Ok(true, "Product deleted successfully"));
        }

        [HttpPost("upload-excel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
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

            var result = await _productRepository.UploadProductsAsync(file, companyId, finalBranchId);
            return Ok(new { message = $"{result.successCount} New Products saved and {result.updateCount} Products updated successfully.", errors = result.errors });
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

            var exists = await _productRepository.ExistsByNameAsync(name, companyId, excludeId);
            return Ok(new { exists });
        }

        [HttpGet("search")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> Search([FromQuery] string term)
        {
            var result = await _productRepository.SearchActiveProductsAsync(term);
            return Ok(result);
        }

        [HttpGet("rate")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetRate([FromQuery] Guid productId, [FromQuery] Guid? priceListId, [FromQuery] string? type)
        {
            var result = await _productRepository.GetProductRateAsync(productId, priceListId, type);
            return Ok(result);
        }

        [HttpGet("low-stock")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetLowStock()
        {
            var result = await _productRepository.GetLowStockProductsAsync();
            return Ok(result);
        }

        [HttpGet("export-low-stock")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> ExportLowStock()
        {
            var data = await _productRepository.GetLowStockExportDataAsync();
            return Ok(data);
        }

        [HttpGet("recent-movements")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetRecentMovements([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _productRepository.GetRecentMovementsPagedAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("download-template")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public IActionResult DownloadTemplate()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Templates", "product_template.csv");
            if (!System.IO.File.Exists(filePath)) 
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "product_template.csv");
            }
            if (!System.IO.File.Exists(filePath)) return NotFound("Template file not found.");

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Products");
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
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Product_Template.xlsx");
                }
            }
        }
    }
}
