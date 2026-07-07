using Microsoft.AspNetCore.Mvc;
using MassTransit;
using Shared.Contracts;
using System;
using System.Threading.Tasks;

namespace Customers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestRabbitMqController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public TestRabbitMqController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet("publish")]
    public async Task<IActionResult> PublishTestMessage([FromQuery] string message = "Hello from RabbitMQ!")
    {
        await _publishEndpoint.Publish<TestEvent>(new
        {
            Message = message,
            Timestamp = DateTime.UtcNow
        });

        return Ok(new { Status = "Success", MessageSent = message });
    }
}
