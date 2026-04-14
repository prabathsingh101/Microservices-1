using ClosedXML.Excel;
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
        public UnitsController(IMediator mediator) => _mediator = mediator;

        [HttpPost("bulk")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> CreateBulk([FromBody] CreateBulkUnitsCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok() : BadRequest("Could not save units");
        }

        [HttpPost("import")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please upload a file");

            var units = new List<UnitRequestDto>();
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                        foreach (var row in rows)
                        {
                            var name = row.Cell(1).GetValue<string>();
                            var description = row.Cell(2).GetValue<string>();

                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                units.Add(new UnitRequestDto { Name = name, Description = description });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error parsing Excel file: {ex.Message}");
            }

            if (units.Count == 0) return BadRequest("No valid data found in Excel");

            var command = new CreateBulkUnitsCommand(units);
            var result = await _mediator.Send(command);

            return result ? Ok(new { message = "Units imported successfully" }) : BadRequest("Could not import units");
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
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
