using Inventory.Application.Locations.Racks.Commands.CreateRack;
using Inventory.Application.Locations.Racks.Commands.UpdateRack;
using Inventory.Application.Locations.Racks.Commands.DeleteRack;
using Inventory.Application.Locations.Racks.Queries.GetRacks;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.API.Common;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/racks")]
public sealed class RacksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRackRepository _rackRepository;

    public RacksController(IMediator mediator, IRackRepository rackRepository)
    {
        _mediator = mediator;
        _rackRepository = rackRepository;
    }

    [HttpPost]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Create(CreateRackCommand command)
    {
        // ... (rest of the create logic stays the same)
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            command = command with { CompanyId = companyId };
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

        // ... (rest of the update logic stays the same)
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            command = command with { CompanyId = companyId };
        }

        await _mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Rack updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteRackCommand(id));
        return Ok(new { success = true, message = "Rack deleted successfully" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetRacksQuery());
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

        var result = await _rackRepository.UploadRacksAsync(file, companyId);

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
        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Racks");
            var headers = new string[] { "WarehouseName", "RackName", "Description" };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightSteelBlue;
            }

            // Sample Data (Varied Rack Types)
            var rackData = new List<(string Warehouse, string Name, string Description)>
            {
                ("Main Hub", "Rack A-01", "Primary storage for electronic components"),
                ("Grocery Central", "Kirana Row 1", "Dedicated row for spices and oils"),
                ("Electric Branch - North", "Wire Spool Rack", "Wall-mounted rack for electrical wire spools"),
                ("Grocery Central", "Grains B-10", "Heavy-duty rack for 50kg grain sacks"),
                ("Electric Branch - North", "Circuit Breaker Bin", "Small compartment rack for MCBs and switches"),
                ("Kirana Wholesale Hub", "Traditional Herbs A", "Shelving for medicinal herbs and traditional packs"),
                ("Industial Vault", "Heavy Motor Stand", "Floor reinforced rack for heavy industrial motors"),
                ("South Storage Wing", "Cold Rack 01", "Insulated rack for temperature-sensitive grocery items"),
                ("Main Hub", "Expired Rack", "Designated area for storing expired or damaged items awaiting disposal"),
                ("Electric Branch - North", "Bulb & Tube Section", "Protective rack for fragile lighting equipment"),
                ("Kirana Backup Store", "Oil Container Row", "Bottom level rack for heavy oil containers"),
                ("Downtown Outlet", "Front Display Rack", "Retail shelf for fast-moving items")
            };

            for (int i = 0; i < rackData.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = rackData[i].Warehouse;
                worksheet.Cell(i + 2, 2).Value = rackData[i].Name;
                worksheet.Cell(i + 2, 3).Value = rackData[i].Description;
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
