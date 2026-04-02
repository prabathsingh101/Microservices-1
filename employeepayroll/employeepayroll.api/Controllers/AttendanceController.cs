using employeepayroll.Application.Attendances.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace employeepayroll.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Guid>> SubmitAttendance([FromBody] SubmitAttendanceCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(id);
    }
}
