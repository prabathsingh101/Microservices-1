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

        public ProductsController(IMediator mediator, Inventory.Application.Common.Interfaces.IProductRepository productRepository)
        {
            _mediator = mediator;
            _productRepository = productRepository;
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
                    new string[] { "Smart Electrical", "Smart Plug", "Oakter Smart Plug 16A", "SMART001", "Oakter", "PIECE", "450", "990", "10", "891", "18", "8537", "20", "0", "finished", "TRUE", "FALSE", "TRUE", "Main Hub", "Rack A-01", "WiFi enabled smart plug" },
                    new string[] { "Smart Electrical", "Smart Bulb", "Wipro Smart RGB 9W", "SMART002", "Wipro", "PIECE", "320", "699", "5", "664", "12", "8539", "50", "0", "finished", "TRUE", "FALSE", "TRUE", "Main Hub", "Rack A-01", "App controlled color lighting" },
                    new string[] { "Lighting", "LED Bulb", "Syska LED Bulb 9W", "ELEC101", "Syska", "PIECE", "65", "150", "10", "135", "12", "8539", "100", "0", "finished", "TRUE", "FALSE", "TRUE", "Main Hub", "Rack A-01", "Cool day light 9W LED" },
                    new string[] { "Lighting", "Tube Light", "Philips LED Tube 20W", "ELEC102", "Philips", "PIECE", "180", "450", "15", "380", "12", "8539", "50", "0", "finished", "TRUE", "FALSE", "TRUE", "North Warehouse", "Bulb & Tube Section", "Energy efficient tube" },
                    new string[] { "Wires & Cables", "Copper Wire", "Finolex 1.5sqmm 90m", "ELEC201", "Finolex", "ROLL", "850", "1450", "10", "1305", "18", "8544", "20", "0", "finished", "TRUE", "FALSE", "TRUE", "North Warehouse", "Wire Spool Rack", "Fire retardant wire" },
                    new string[] { "Switches & Sockets", "Modular Switch", "Anchor Roma 6A", "ELEC301", "Anchor", "PIECE", "18", "45", "5", "42", "18", "8536", "500", "0", "finished", "TRUE", "FALSE", "TRUE", "Main Hub", "Rack A-01", "Modular 1-way switch" },
                    new string[] { "Fans", "Ceiling Fan", "Havells Nicola 1200mm", "ELEC501", "Havells", "PIECE", "2100", "3800", "15", "3230", "18", "8414", "25", "0", "finished", "TRUE", "FALSE", "TRUE", "Main Hub", "Rack A-01", "Fast high speed fan" },
                    new string[] { "Power Backup", "Inverter", "Luminous Zelio 1100", "POW001", "Luminous", "PIECE", "5200", "8500", "10", "7650", "18", "8504", "10", "0", "finished", "TRUE", "FALSE", "TRUE", "North Warehouse", "Rack A-01", "Pure sine wave inverter" },
                    new string[] { "Beverages", "Soft Drink", "Coca Cola 2L", "GROC001", "Coke", "BOTTLE", "75", "100", "5", "95", "18", "2202", "24", "0", "finished", "TRUE", "FALSE", "TRUE", "Grocery Central", "Kirana Row 1", "Carbonated soft drink" },
                    new string[] { "Beverages", "Fruit Juice", "Real Orange 1L", "GROC002", "Real", "PACK", "90", "120", "10", "108", "12", "2009", "36", "0", "finished", "TRUE", "TRUE", "TRUE", "Grocery Central", "Kirana Row 1", "Fresh fruit juice" },
                    new string[] { "Snacks & Branded Foods", "Potato Chips", "Lays Magic Masala 50g", "GROC010", "Lays", "PACK", "16", "20", "0", "20", "12", "2106", "100", "0", "finished", "TRUE", "TRUE", "TRUE", "Grocery Central", "Kirana Row 1", "Spicy wafers" },
                    new string[] { "Pulses & Dals", "Moong Dal", "Tata Sampann Moong 1kg", "GROC020", "Tata", "KG", "145", "180", "5", "171", "5", "0713", "50", "0", "finished", "TRUE", "TRUE", "TRUE", "Grocery Central", "Grains B-10", "Yellow moong split" },
                    new string[] { "Cooking Oils & Ghee", "Mustard Oil", "Fortune Kachi Ghani 1L", "GROC050", "Fortune", "BOTTLE", "135", "180", "10", "162", "12", "1514", "48", "0", "finished", "TRUE", "TRUE", "TRUE", "Kirana Wholesale Hub", "Oil Container Row", "Pure oil" },
                    new string[] { "Kirana General", "Table Salt", "Tata Salt 1kg", "GROC060", "Tata", "PACK", "20", "28", "0", "28", "5", "2501", "200", "0", "finished", "TRUE", "FALSE", "TRUE", "Grocery Central", "Kirana Row 1", "Iodized salt" },
                    new string[] { "Cleaning Supplies", "Detergent", "Surf Excel 1kg", "CLEAN01", "HUL", "PACK", "110", "180", "5", "171", "18", "3402", "60", "0", "finished", "TRUE", "FALSE", "TRUE", "Grocery Central", "Kirana Row 1", "Wash detergent" },
                    new string[] { "Dairy Products", "Paneer", "Amul Fresh Paneer 200g", "DAIRY01", "Amul", "PACK", "75", "90", "2", "88.2", "5", "0406", "20", "0", "finished", "TRUE", "TRUE", "TRUE", "South Storage Wing", "Cold Rack 01", "Fresh cottage cheese" },
                    new string[] { "Biscuits & Cookies", "Gluco Biscuits", "Parle-G 800g", "BISC01", "Parle", "PACK", "65", "85", "5", "80.75", "18", "1905", "120", "0", "finished", "TRUE", "FALSE", "TRUE", "Grocery Central", "Kirana Row 1", "Original glucose biscuit" },
                    new string[] { "Spices & Masalas", "Turmeric Powder", "Catch Haldi 200g", "SPICE01", "Catch", "PACK", "45", "65", "10", "58.5", "5", "0910", "80", "0", "finished", "TRUE", "FALSE", "TRUE", "Kirana Wholesale Hub", "Traditional Herbs A", "Pure turmeric" },
                    new string[] { "Atta & Flours", "Wheat Atta", "Aashirvaad Atta 10kg", "ATTA01", "ITC", "BAG", "420", "550", "5", "522.5", "5", "1101", "30", "0", "finished", "TRUE", "FALSE", "TRUE", "Grocery Central", "Grains B-10", "MP Lokwan wheat" },
                    new string[] { "Rice & Rice Products", "Basmati Rice", "India Gate Basmati 5kg", "RICE01", "India Gate", "BAG", "550", "950", "20", "760", "5", "1006", "15", "0", "finished", "TRUE", "FALSE", "TRUE", "Grocery Central", "Grains B-10", "Premium aged rice" },
                    new string[] { "Appliances", "Electric Kettle", "Pigeon Amaze 1.5L", "APP001", "Pigeon", "PIECE", "450", "1200", "50", "600", "18", "8516", "25", "0", "finished", "TRUE", "FALSE", "TRUE", "Main Hub", "Rack A-01", "Stainless steel kettle" },
                    new string[] { "Appliances", "Iron", "Bajaj Majesty DX6", "APP002", "Bajaj", "PIECE", "550", "950", "15", "807.5", "18", "8516", "40", "0", "finished", "TRUE", "FALSE", "TRUE", "Main Hub", "Rack A-01", "Dry iron light weight" }
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
