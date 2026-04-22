using ClosedXML.Excel;
using DinkToPdf;
using DinkToPdf.Contracts;
using Inventory.API.Common;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Application.Products.DTOs;
using Inventory.Application.Products.Queries.GetProductById;
using Inventory.Application.Products.Queries.GetProducts;
using Inventory.Application.Products.Queries.GetProductTransactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;

        private readonly IProductRepository _productRepository;
        private readonly IConverter _converter;

        public ProductsController(IMediator mediator, 
            IProductRepository productRepository,
            IConverter converter)
        {
            _mediator = mediator;
            _productRepository = productRepository;
            _converter = converter;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Create(CreateProductCommand command)
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
               "Product is created successfully"
           ));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Update(
        Guid id,
        UpdateProductCommand command)
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
                    "Product updated successfully"
                )
            );
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(
                new DeleteProductCommand(id));

            return Ok(
                ApiResponse<Guid>.Ok(
                    result,
                    "Product deleted successfully"
                )
            );
        }


        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetProductByIdQuery(id));
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetProductsQuery());
            return Ok(result);
        }

        [HttpGet("paged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] GridRequest request)
        {
            var result = await _mediator.Send(
                new GetProductsPagedQuery(request)
            );

            return Ok(result);
        }

        [HttpPost("bulk-delete")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids)
        {
            await _mediator.Send(new BulkDeleteProductCommand(ids));

            return Ok(new
            {
                success = true,
                message = "Product list deleted successfully"
            });
        }

        [HttpGet("search")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Search([FromQuery] string term)
        {
            // Mediator query ko Handler tak pahuchayega
            var result = await _mediator.Send(new GetProductSearchQuery(term));
            return Ok(result);
        }

        [HttpGet("{id}/transactions")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetTransactions(Guid id)
        {
            var result = await _mediator.Send(new GetProductTransactionsQuery(id));
            return Ok(result);
        }

        [HttpGet("rate")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetRate([FromQuery] Guid productId, [FromQuery] Guid priceListId)
        {

            var query = new GetProductRateQuery(productId, priceListId);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("low-stock")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<ActionResult<IEnumerable<LowStockProductDto>>> GetLowStock()
        {
            var products = await _productRepository.GetLowStockProductsAsync();

            if (products == null || !products.Any())
            {
                return Ok(new List<LowStockProductDto>()); // Empty list agar sab khairiyat hai
            }

            return Ok(products);
        }


        /// <summary>
        /// Export to excel
        /// </summary>
        /// <returns></returns>
        [HttpGet("export-low-stock")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> ExportLowStock()
        {
            var data = await _productRepository.GetLowStockExportDataAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Low Stock Report");

                // Headers setup
                worksheet.Cell(1, 1).Value = "Product Name";
                worksheet.Cell(1, 2).Value = "SKU";
                worksheet.Cell(1, 3).Value = "Category";
                worksheet.Cell(1, 4).Value = "Min Stock";
                worksheet.Cell(1, 5).Value = "Current Stock";
                worksheet.Cell(1, 6).Value = "Discount";
                worksheet.Cell(1, 7).Value = "Unit";
                worksheet.Cell(1, 8).Value = "Warehouse";
                worksheet.Cell(1, 9).Value = "Rack";
                worksheet.Cell(1, 10).Value = "Expiry";

                // Styling: Bold headers and background color
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

                // Data insertion
                for (int i = 0; i < data.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cell(row, 1).Value = data[i].ProductName;
                    worksheet.Cell(row, 2).Value = data[i].SKU;
                    worksheet.Cell(row, 3).Value = data[i].Category;
                    worksheet.Cell(row, 4).Value = data[i].MinStock;
                    worksheet.Cell(row, 5).Value = data[i].CurrentStock;
                    worksheet.Cell(row, 6).Value = data[i].Discount;
                    worksheet.Cell(row, 7).Value = data[i].Unit;
                    worksheet.Cell(row, 8).Value = data[i].Warehouse;
                    worksheet.Cell(row, 9).Value = data[i].Rack;
                    worksheet.Cell(row, 10).Value = data[i].IsExpiryRequired ? "Yes" : "No";

                    // Low stock indication (Optional highlighting)
                    worksheet.Cell(row, 5).Style.Font.FontColor = XLColor.Red;
                }

                worksheet.Columns().AdjustToContents(); // Auto-fit columns

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    // Return as File
                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"LowStockReport_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }


        /// <summary>
        /// export to pdf
        /// </summary>
        /// <returns></returns>
        [HttpGet("export-low-stock-pdf")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> ExportLowStockPdf()
        {
            // 1. Excel wala hi Repository method call karein
            var data = await _productRepository.GetLowStockExportDataAsync();

            // 2. HTML Template design karein
            var htmlContent = $@"
        <html>
            <head>
                <style>
                    body {{ font-family: 'Segoe UI', Arial; padding: 20px; }}
                    .header {{ text-align: center; color: #2c3e50; margin-bottom: 30px; border-bottom: 2px solid #3498db; padding-bottom: 10px; }}
                    table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
                    th {{ background-color: #3498db; color: white; padding: 12px; text-align: left; }}
                    td {{ border: 1px solid #ddd; padding: 10px; }}
                    tr:nth-child(even) {{ background-color: #f9f9f9; }}
                    .low-stock-alert {{ color: #e74c3c; font-weight: bold; }}
                    .footer {{ margin-top: 30px; font-size: 10px; text-align: right; color: #7f8c8d; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1>Electric Inventory System</h1>
                    <h3>Low Stock Report</h3>
                    <p>Generated Date: {DateTime.Now:dd MMM yyyy HH:mm}</p>
                </div>
                <table>
                    <thead>
                        <tr>
                            <th>Product Name</th>
                            <th>SKU</th>
                            <th>Category</th>
                            <th>Discount</th>
                            <th>Current Stock</th>
                            <th>Min Stock</th>
                            <th>Unit</th>
                        </tr>
                    </thead>
                    <tbody>";

            foreach (var item in data)
            {
                htmlContent += $@"
                <tr>
                    <td>{item.ProductName}</td>
                    <td>{item.SKU}</td>
                    <td>{item.Category}</td>
                    <td>{item.Discount}</td>
                    <td class='low-stock-alert'>{item.CurrentStock}</td>
                    <td>{item.MinStock}</td>
                    <td>{item.Unit}</td>
                </tr>";
            }

            htmlContent += $@"
                    </tbody>
                </table>
                <div class='footer'>
                    This is a computer generated inventory report.
                </div>
            </body>
        </html>";

            // 3. DinkToPdf Settings
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = htmlContent,
                WebSettings = { DefaultEncoding = "utf-8" },
                HeaderSettings = { FontName = "Arial", FontSize = 9, Right = "Page [page] of [toPage]", Line = true },
                FooterSettings = { FontName = "Arial", FontSize = 9, Center = "Inventory Management System", Line = true }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            // 4. Conversion aur File return
            var file = _converter.Convert(pdf);
            return File(file, "application/pdf", $"LowStockReport_{DateTime.Now:yyyyMMdd}.pdf");
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

            var result = await _productRepository.UploadProductsAsync(file, companyId);

            return Ok(new
            {
                Message = $"{result.successCount} Products uploaded successfully.",
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

            var exists = await _productRepository.ExistsByNameAsync(name, companyId, excludeId);

            return Ok(new
            {
                exists = exists,
                message = exists ? $"The product name '{name}' is already used by another active product." : string.Empty
            });
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

                // Original Sample Data from Angular Files
                var samples = new List<string[]>
                {
                    new string[] { "Smart Electrical", "Fans", "Ceiling Fan", "ELEC001", "Havells", "PIECE", "1800", "2500", "10", "2200", "18", "8414", "10", "0", "finished", "TRUE", "FALSE", "TRUE", "Main Warehouse", "Rack A3", "High speed decorative fan" },
                    new string[] { "Smart Electrical", "Lights", "LED Bulb 9W", "ELEC002", "Philips", "PIECE", "60", "120", "15", "100", "12", "8539", "50", "0", "finished", "TRUE", "FALSE", "TRUE", "Main Warehouse", "Rack R7", "Cool day light LED" },
                    new string[] { "Smart Electrical", "Wires", "Copper Wire 2.5mm", "ELEC004", "Polycab", "ROLL", "900", "1300", "10", "1150", "18", "8544", "20", "0", "finished", "TRUE", "FALSE", "TRUE", "Cable & Wire Warehouse", "Rack C2", "FR PVC insulated wire" }
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
