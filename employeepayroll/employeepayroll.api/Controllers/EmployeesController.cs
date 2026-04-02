using employeepayroll.Application.Employees.Commands;
using employeepayroll.Application.Employees.DTOs;
using employeepayroll.Application.Employees.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace employeepayroll.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetEmployees()
    {
        var query = new GetEmployeesQuery();
        var result = await mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateEmployee([FromBody] CreateEmployeeCommand command)
    {
        var id = await mediator.Send(command);
        return Ok(id);
    }
}
