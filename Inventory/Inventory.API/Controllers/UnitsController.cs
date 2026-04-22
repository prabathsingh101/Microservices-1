using ClosedXML.Excel;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Units.Command;
using Inventory.Application.Units.DTOs;
using Inventory.Application.Units.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnitsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUnitRepository _unitRepository;
        
        public UnitsController(IMediator mediator, IUnitRepository unitRepository)
        {
            _mediator = mediator;
            _unitRepository = unitRepository;
        }

        [HttpPost("bulk")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> CreateBulk([FromBody] CreateBulkUnitsCommand command)
        {
            // 🚀 SMART INJECTION: Get CompanyId from Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId };
            }

            var result = await _mediator.Send(command);
            return result ? Ok() : BadRequest("Could not save units");
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

            var result = await _unitRepository.UploadUnitsAsync(file, companyId);

            return Ok(new
            {
                message = $"{result.successCount} Units processed successfully.",
                errors = result.errors
            });
        }

        [HttpGet("download-template")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Units");
                var headers = new string[] { "Name", "Description", "Status (Active/Inactive)" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightCyan;
                }

                var unitsData = new List<(string Name, string Description, string Status)>
                {
                    // Grocery Units
                    ("Kg", "Kilogram - Base unit for solid items (Grain, Sugar)", "Active"),
                    ("Gram", "Base unit for spices and small packs", "Active"),
                    ("Litre", "Base unit for liquid items (Oil, Milk)", "Active"),
                    ("ML", "Millilitre - used for small liquid quantity", "Active"),
                    ("Pkt", "Packet - commonly used for biscuits, snacks", "Active"),
                    ("Pch", "Pouch - used for small detergent/spice packs", "Active"),
                    ("Btl", "Bottle - used for soft drinks, sauces", "Active"),
                    ("Jar", "Jar - used for pickles, jam, honey", "Active"),
                    ("Box", "Box - used for tea bags, chocolates", "Active"),
                    ("Bag", "Bag - secondary unit for grains (5kg, 10kg)", "Active"),
                    ("Sck", "Sack - used for wholesale grain quantities", "Active"),
                    ("Tin", "Tin - used for ghee or edible oils", "Active"),
                    ("Dozen", "Dozen - used for eggs or banana sets", "Active"),
                    ("Pcs", "Pieces - used for individual items", "Active"),
                    
                    // Electric Units
                    ("Nos", "Numbers - primary count for electrical parts", "Active"),
                    ("Mtr", "Meter - used for wires, cables, lighting strips", "Active"),
                    ("Ft", "Foot - alternative measurement for piping/wiring", "Active"),
                    ("Coil", "Coil - standard bundle for long cables/conduits", "Active"),
                    ("Roll", "Roll - used for tapes, foils, or LED strips", "Active"),
                    ("Reel", "Reel - used for industrial grade wiring", "Active"),
                    ("Drum", "Drum - used for bulk cable storage", "Active"),
                    ("Set", "Set - used for combo packs or tools", "Active"),
                    ("Pair", "Pair - used for tools or components", "Active"),
                    ("Unit", "Unit - used for equipment/appliances", "Active"),
                    ("Bundle", "Bundle - used for pipes or sticks", "Active")
                };

                for (int i = 0; i < unitsData.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = unitsData[i].Name;
                    worksheet.Cell(i + 2, 2).Value = unitsData[i].Description;
                    worksheet.Cell(i + 2, 3).Value = unitsData[i].Status;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Unit_Template.xlsx");
                }
            }
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");

            // 🚀 SMART INJECTION: Get CompanyId from Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                command = command with { CompanyId = companyId };
            }

            var result = await _mediator.Send(command);
            return result ? Ok() : BadRequest("Could not update unit");
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(new DeleteUnitCommand(id));
            return result ? Ok() : BadRequest("Could not delete unit");
        }

        [HttpGet("getbyid/{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var units = await _mediator.Send(new GetAllUnitsQuery());
            var unit = units.FirstOrDefault(u => u.Id == id);
            return unit != null ? Ok(unit) : NotFound();
        }

        [HttpGet("get")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetAll()
            => Ok(await _mediator.Send(new GetAllUnitsQuery()));
    }
}
