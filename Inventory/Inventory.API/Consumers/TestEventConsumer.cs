using MassTransit;
using Shared.Contracts;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Inventory.API.Consumers;

public class TestEventConsumer : IConsumer<TestEvent>
{
    private readonly ILogger<TestEventConsumer> _logger;

    public TestEventConsumer(ILogger<TestEventConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TestEvent> context)
    {
        _logger.LogInformation(">>> RabbitMQ Success! Received: '{Message}' at {Timestamp}", 
            context.Message.Message, context.Message.Timestamp);
        await Task.CompletedTask;
    }
}
