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
    [Route("api/units")]
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
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "unit_template.csv");
            if (!System.IO.File.Exists(filePath)) return NotFound("Template file not found.");

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Units");
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
