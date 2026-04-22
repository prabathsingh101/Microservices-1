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
            return Ok(new { message = $"{result.successCount} Products uploaded successfully.", errors = result.errors });
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
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Products");
                var headers = new string[] { 
                    "Category", "Subcategory", "ProductName", "SKU", "Brand", "Unit", 
                    "BasePrice", "MRP", "Discount", "SaleRate", "GST%", "HSNCode", "MinStock", 
                    "DamagedStock", "ProductType", "TrackInventory", "RequiresExpiry", "Active", 
                    "DefaultWarehouse", "DefaultRack", "Description" 
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightSteelBlue;
                }

                var samples = new List<string[]>
                {
                    new string[] { "Smart Electrical", "Fans", "Crompton Smart Fan 1200mm", "SMART101", "Crompton", "Pcs", "2800", "4500", "15", "3825", "18", "8414", "10", "0", "Finished", "TRUE", "FALSE", "TRUE", "Main Warehouse", "Rack A3", "Remote & App controlled fan" },
                    new string[] { "Smart Electrical", "Lights", "Syska Smart LED Panel 15W", "SMART102", "Syska", "Pcs", "450", "1200", "20", "960", "12", "8539", "30", "0", "Finished", "TRUE", "FALSE", "TRUE", "Main Warehouse", "Rack R7", "Voice controlled LED panel" },
                    new string[] { "Smart Electrical", "Switches", "Wipro Smart Touch Switch", "SMART103", "Wipro", "Pcs", "850", "1800", "10", "1620", "18", "8536", "40", "0", "Finished", "TRUE", "FALSE", "TRUE", "Main Warehouse", "Rack R10", "4-node modular smart switch" },
                    new string[] { "Smart Electrical", "Wires", "Finolex Smart Safety Wire 1.5mm", "SMART104", "Finolex", "Roll", "1200", "1850", "5", "1757.5", "18", "8544", "20", "0", "Finished", "TRUE", "FALSE", "TRUE", "Cable & Wire Warehouse", "Rack C2", "Special grade FR wire" },
                    new string[] { "Smart Electrical", "Appliances", "Preethi Smart Mixer Grinder", "SMART105", "Preethi", "Pcs", "3200", "6500", "15", "5525", "18", "8509", "12", "0", "Finished", "TRUE", "FALSE", "TRUE", "Main Warehouse", "Rack R2", "App linked mixer" },
                    new string[] { "Smart Electrical", "Protection", "Guard Smart Stabilizer", "SMART106", "V-Guard", "Pcs", "1800", "3200", "10", "2880", "18", "8504", "15", "0", "Finished", "TRUE", "FALSE", "TRUE", "South Storage Wing", "Cold Rack 01", "Digital LCD voltage protector" },
                    new string[] { "Smart Electrical", "Cables", "Lapp Smart Data Cable Cat6", "SMART107", "Lapp", "Roll", "2500", "4200", "12", "3696", "18", "8544", "25", "0", "Finished", "TRUE", "FALSE", "TRUE", "Cable & Wire Warehouse", "Rack C2", "High speed shielded data cable" },
                    new string[] { "Smart Electrical", "Tools", "Stanley Smart Precision Set", "SMART108", "Stanley", "Set", "1500", "2800", "15", "2380", "12", "8205", "10", "0", "Finished", "TRUE", "FALSE", "TRUE", "Main Warehouse", "Rack A3", "Digital torque measuring tools" },
                    new string[] { "Smart Electrical", "Batteries", "Exide Smart Tubular IT850", "SMART109", "Exide", "Pcs", "12500", "18500", "10", "16650", "28", "8507", "8", "0", "Finished", "TRUE", "FALSE", "TRUE", "Cable & Wire Warehouse", "Rack C1", "Smart status led battery" },
                    new string[] { "Smart Electrical", "Fittings", "Precision Smart Conduit 25mm", "SMART110", "Precision", "Bundle", "850", "1450", "10", "1305", "18", "3917", "50", "0", "Finished", "TRUE", "FALSE", "TRUE", "Cable & Wire Warehouse", "Rack C1", "FR grade smart conduit" },
                    new string[] { "Lighting", "LED Bulb", "Syska LED Bulb 9W", "ELEC101", "Syska", "Pcs", "65", "150", "10", "135", "12", "8539", "100", "0", "Finished", "TRUE", "FALSE", "TRUE", "Main Warehouse", "Rack R7", "Cool day light 9W LED" },
                    new string[] { "Lighting", "Tube Light", "Philips LED Tube 20W", "ELEC102", "Philips", "Pcs", "180", "450", "15", "380", "12", "8539", "50", "0", "Finished", "TRUE", "FALSE", "TRUE", "Main Warehouse", "Rack R7", "Energy efficient tube" },
                    new string[] { "Beverages", "Soft Drink", "Coca Cola 2L", "GROC001", "Coke", "Btl", "75", "100", "5", "95", "18", "2202", "24", "0", "Finished", "TRUE", "FALSE", "TRUE", "Grocery Central", "Kirana Row 1", "Carbonated soft drink" },
                    new string[] { "Beverages", "Fruit Juice", "Real Orange 1L", "GROC002", "Real", "Pkt", "90", "120", "10", "108", "12", "2009", "36", "0", "Finished", "TRUE", "TRUE", "TRUE", "Grocery Central", "Kirana Row 1", "Fresh fruit juice" },
                    new string[] { "Snacks & Branded Foods", "Potato Chips", "Lays Magic Masala 50g", "GROC010", "Lays", "Pkt", "16", "20", "0", "20", "12", "2106", "100", "0", "Finished", "TRUE", "TRUE", "TRUE", "Grocery Central", "Kirana Row 1", "Spicy wafers" },
                    new string[] { "Pulses & Dals", "Moong Dal", "Tata Sampann Moong 1kg", "GROC020", "Tata", "Kg", "145", "180", "5", "171", "5", "0713", "50", "0", "Finished", "TRUE", "TRUE", "TRUE", "Grocery Central", "Grains B-10", "Yellow moong split" },
                    new string[] { "Cooking Oils & Ghee", "Mustard Oil", "Fortune Kachi Ghani 1L", "GROC050", "Fortune", "Btl", "135", "180", "10", "162", "12", "1514", "48", "0", "Finished", "TRUE", "TRUE", "TRUE", "Kirana Wholesale Hub", "Traditional Herbs A", "Pure oil" },
                    new string[] { "Kirana General", "Table Salt", "Tata Salt 1kg", "GROC060", "Tata", "Pkt", "20", "28", "0", "28", "5", "2501", "200", "0", "Finished", "TRUE", "FALSE", "TRUE", "Grocery Central", "Kirana Row 1", "Iodized salt" },
                    new string[] { "Cleaning Supplies", "Detergent", "Surf Excel 1kg", "CLEAN01", "HUL", "Pkt", "110", "180", "5", "171", "18", "3402", "60", "0", "Finished", "TRUE", "FALSE", "TRUE", "Grocery Central", "Kirana Row 1", "Wash detergent" },
                    new string[] { "Dairy Products", "Fresh Paneer", "Amul Fresh Paneer 200g", "DAIRY01", "Amul", "Pkt", "75", "90", "2", "88.2", "5", "0406", "20", "0", "Finished", "TRUE", "TRUE", "TRUE", "South Storage Wing", "Cold Rack 01", "Fresh cottage cheese" }
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
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Product_Template.xlsx");
                }
            }
        }
    }
}
