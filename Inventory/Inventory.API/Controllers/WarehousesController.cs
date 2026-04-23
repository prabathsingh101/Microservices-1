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
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/warehouses")]
public sealed class WarehousesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly InventoryDbContext _context;

    public WarehousesController(IMediator mediator, IWarehouseRepository warehouseRepository, InventoryDbContext context)
    {
        _mediator = mediator;
        _warehouseRepository = warehouseRepository;
        _context = context;
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
        var filePath = Path.Combine(AppContext.BaseDirectory, "Templates", "warehouse_template.csv");
        if (!System.IO.File.Exists(filePath)) return NotFound("Template file not found.");

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Warehouses");
            var csvLines = System.IO.File.ReadAllLines(filePath);
            
            if (csvLines.Length > 0)
            {
                // 1. Process Header
                var headers = csvLines[0].Split(',');
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightCyan;
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
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Warehouse_Template.xlsx");
            }
        }
    }
    [HttpGet("debug-info")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> DebugInfo()
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        Guid.TryParse(companyIdClaim, out var companyId);

        var count = await _context.Warehouses.CountAsync(x => x.CompanyId == companyId);
        var allCount = await _context.Warehouses.CountAsync();

        return Ok(new
        {
            CurrentCompanyId = companyId,
            WarehousesForThisCompany = count,
            TotalWarehousesInDb = allCount,
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }

    [HttpGet("seed-sample")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> SeedSample()
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (!Guid.TryParse(companyIdClaim, out var companyId))
        {
            return BadRequest(new { success = false, message = "CompanyId not found in claims." });
        }

        var existingWarehouses = await _context.Warehouses.Where(x => x.CompanyId == companyId).ToListAsync();
        if (existingWarehouses.Any()) 
        {
            // 🔥 If they exist, let's make sure they are active
            foreach (var w in existingWarehouses)
            {
                w.Update(w.Name, w.City, w.Description, true, w.CompanyId);
            }
            
            var existingRacks = await _context.Racks.Where(x => x.CompanyId == companyId).ToListAsync();
            foreach (var r in existingRacks)
            {
                r.Update(r.WarehouseId, r.Name, r.Description, true, r.CompanyId);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Existing Warehouses and Racks have been activated." });
        }

        var mainWarehouse = new Warehouse("Main Warehouse", "New Delhi", "Primary distribution center", true, companyId);
        await _context.Warehouses.AddAsync(mainWarehouse);
        
        var rackA = new Rack(mainWarehouse.Id, "Rack A1", "Ground Floor", true, companyId);
        var rackB = new Rack(mainWarehouse.Id, "Rack B2", "First Floor", true, companyId);
        await _context.Racks.AddRangeAsync(rackA, rackB);

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Sample Warehouse and Racks seeded successfully." });
    }
}
