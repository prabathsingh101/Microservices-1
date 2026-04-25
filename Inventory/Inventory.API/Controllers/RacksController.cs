using Inventory.Application.Locations.Racks.Commands.CreateRack;
using Inventory.Application.Locations.Racks.Commands.DeleteRack;
using Inventory.Application.Locations.Racks.Commands.UpdateRack;
using Inventory.Application.Locations.Racks.Queries.GetRacks;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.API.Common;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;
using ClosedXML.Excel;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/racks")]
public sealed class RacksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRackRepository _rackRepository;
    private readonly InventoryDbContext _context;

    public RacksController(IMediator mediator, IRackRepository rackRepository, InventoryDbContext context)
    {
        _mediator = mediator;
        _rackRepository = rackRepository;
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Create(CreateRackCommand command)
    {
        var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
        var branchId = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            command = command with { CompanyId = companyId, BranchId = branchId };
        }

        var id = await _mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Rack created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateRackCommand command)
    {
        if (id != command.Id)
            return BadRequest(ApiResponse<string>.Fail("Id mismatch"));

        var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
        var branchId = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            command = command with { CompanyId = companyId, BranchId = branchId };
        }

        await _mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Rack updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteRackCommand(id));
        return Ok(ApiResponse<bool>.Ok(true, "Rack deleted successfully"));
    }

    [HttpGet]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetRacksQuery());
        return Ok(result);
    }

    [HttpGet("warehouse/{warehouseId}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetByWarehouse(Guid warehouseId)
    {
        var result = await _rackRepository.GetByWarehouseIdAsync(warehouseId);
        return Ok(ApiResponse<List<Rack>>.Ok(result));
    }

    [HttpPost("upload-excel")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> UploadExcel(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse<string>.Fail("Please upload an excel file."));

        var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase))?.Value;
        var branchId = User.Claims.FirstOrDefault(c => c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase))?.Value;
        if (!Guid.TryParse(companyIdClaim, out var companyId))
        {
            return BadRequest(ApiResponse<string>.Fail("Invalid session: CompanyId not found"));
        }

        var result = await _rackRepository.UploadRacksAsync(file, companyId, branchId);

        return Ok(new
        {
            message = $"{result.successCount} Racks processed successfully.",
            errors = result.errors
        });
    }

    [HttpGet("download-template")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public IActionResult DownloadTemplate()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Templates", "rack_template.csv");
        if (!System.IO.File.Exists(filePath)) 
        {
             filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "rack_template.csv");
        }
        if (!System.IO.File.Exists(filePath)) return NotFound("Template file not found.");

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Racks");
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
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Rack_Template.xlsx");
            }
        }
    }
}
