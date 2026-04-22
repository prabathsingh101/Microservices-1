using Inventory.Application.Locations.Warehouses.Commands.CreateWarehouse;
using Inventory.Application.Locations.Warehouses.Commands.DeleteWarehouse;
using Inventory.Application.Locations.Warehouses.Commands.UpdateWarehouse;
using Inventory.Application.Locations.Warehouses.Queries.GetWarehouses;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.API.Common;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/warehouses")]
public sealed class WarehousesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWarehouseRepository _warehouseRepository;

    public WarehousesController(IMediator mediator, IWarehouseRepository warehouseRepository)
    {
        _mediator = mediator;
        _warehouseRepository = warehouseRepository;
    }

    [HttpPost]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin")]
    public async Task<IActionResult> Create(CreateWarehouseCommand command)
    {
        // ... (rest of the create logic stays the same)
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            command = command with { CompanyId = companyId };
        }

        var id = await _mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Warehouse created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateWarehouseCommand command)
    {
        if (id != command.Id)
            return BadRequest(ApiResponse<string>.Fail("Id mismatch"));

        // ... (rest of the update logic stays the same)
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            command = command with { CompanyId = companyId };
        }

        await _mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Warehouse updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteWarehouseCommand(id));
        return Ok(new { success = true, message = "Warehouse deleted successfully" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetWarehousesQuery());
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
            return BadRequest("Invalid session: CompanyId not found");
        }

        var result = await _warehouseRepository.UploadWarehousesAsync(file, companyId);

        return Ok(new
        {
            message = $"{result.successCount} Warehouses processed successfully.",
            errors = result.errors
        });
    }
    [HttpGet("download-template")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public IActionResult DownloadTemplate()
    {
        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Warehouses");
            var headers = new string[] { "Name", "Location", "Description" };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightCyan;
            }

            // Sample Data (Grocery & Electric Mix)
            var warehouseData = new List<(string Name, string Location, string Description)>
            {
                ("Main Warehouse", "New Delhi, Okhla", "Primary distribution center for all units"),
                ("Cable & Wire Warehouse", "Rohini Sector 7", "Main hub for electrical wiring and heavy equipment"),
                ("Grocery Central", "Azadpur Mandi", "Bulk storage for grains, pulses, and dry grocery"),
                ("Downtown Outlet", "Connaught Place", "Fast-moving retail items and display stock"),
                ("Kirana Backup Store", "Chandni Chowk", "Small pack supplies and traditional grocery items"),
                ("East Logi-Park", "Laxmi Nagar", "Secondary transit point for electric parts"),
                ("West Service Ware", "Janakpuri", "Spares and maintenance equipment storage"),
                ("South Storage Wing", "Saket", "Premium product handling and cold storage area"),
                ("Express Warehouse", "Dwarka Sector 10", "Quick delivery dispatch center"),
                ("Industrial Vault", "Mayapuri Industrial Area", "Heavy industrial electric motors and spares"),
                ("Kirana Wholesale Hub", "Sadar Bazar", "Wholesale supply storage for pulses and spices"),
                ("Retail Support Unit", "Rajouri Garden", "Frontend retail support and inventory backup")
            };

            for (int i = 0; i < warehouseData.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = warehouseData[i].Name;
                worksheet.Cell(i + 2, 2).Value = warehouseData[i].Location;
                worksheet.Cell(i + 2, 3).Value = warehouseData[i].Description;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Warehouse_Template.xlsx");
            }
        }
    }
}
