using System;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Shared.Contracts;
using Customers.Application.DTOs;
using Customers.Application.Features.Finance.Commands;
using Microsoft.Extensions.Logging;

namespace Customers.API.Consumers
{
    public class CustomerSaleCreatedConsumer : IConsumer<CustomerSaleCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CustomerSaleCreatedConsumer> _logger;

        public CustomerSaleCreatedConsumer(IMediator mediator, ILogger<CustomerSaleCreatedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CustomerSaleCreatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation(">>> RabbitMQ: Consuming CustomerSaleCreatedEvent for Customer: {CustomerId}, Ref: {ReferenceId}", msg.CustomerId, msg.ReferenceId);

            var dto = new CustomerSaleDto
            {
                CustomerId = msg.CustomerId,
                Amount = msg.Amount,
                ReferenceId = msg.ReferenceId,
                Description = msg.Description,
                TransactionDate = msg.TransactionDate,
                CreatedBy = msg.CreatedBy,
                BranchId = msg.BranchId,
                CompanyId = msg.CompanyId
            };

            var command = new RecordCustomerSaleCommand(dto);
            await _mediator.Send(command);
        }
    }

    public class CustomerReceiptCreatedConsumer : IConsumer<CustomerReceiptCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CustomerReceiptCreatedConsumer> _logger;

        public CustomerReceiptCreatedConsumer(IMediator mediator, ILogger<CustomerReceiptCreatedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CustomerReceiptCreatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation(">>> RabbitMQ: Consuming CustomerReceiptCreatedEvent for Customer: {CustomerId}, Ref: {ReferenceNumber}", msg.CustomerId, msg.ReferenceNumber);

            var dto = new CustomerReceiptDto
            {
                CustomerId = msg.CustomerId,
                Amount = msg.Amount,
                ReceiptDate = msg.PaymentDate,
                ReceiptMode = msg.PaymentMode,
                ReferenceNumber = msg.ReferenceNumber,
                Remarks = msg.Remarks,
                CreatedBy = msg.CreatedBy,
                BranchId = msg.BranchId,
                CompanyId = msg.CompanyId
            };

            var command = new RecordCustomerReceiptCommand(dto);
            await _mediator.Send(command);
        }
    }
}
