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
            Message = $"{result.successCount} Warehouses processed successfully.",
            Errors = result.errors
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

            // Sample Data
            worksheet.Cell(2, 1).Value = "Main Warehouse";
            worksheet.Cell(2, 2).Value = "New Delhi, India";
            worksheet.Cell(2, 3).Value = "Primary storage for electronic items";

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
