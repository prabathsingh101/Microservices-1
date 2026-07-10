using System;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Shared.Contracts;
using Suppliers.Application.DTOs;
using Suppliers.Application.Features.Suppliers.Commands;
using Microsoft.Extensions.Logging;

namespace Suppliers.API.Consumers
{
    public class SupplierPurchaseCreatedConsumer : IConsumer<SupplierPurchaseCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SupplierPurchaseCreatedConsumer> _logger;

        public SupplierPurchaseCreatedConsumer(IMediator mediator, ILogger<SupplierPurchaseCreatedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SupplierPurchaseCreatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation(">>> RabbitMQ: Consuming SupplierPurchaseCreatedEvent for Supplier: {SupplierId}, Ref: {ReferenceId}", msg.SupplierId, msg.ReferenceId);

            var dto = new SupplierPurchaseDto
            {
                SupplierId = msg.SupplierId,
                CompanyId = msg.CompanyId ?? Guid.Empty,
                BranchId = msg.BranchId,
                Amount = msg.Amount,
                ReferenceId = msg.ReferenceId,
                Description = msg.Description,
                TransactionDate = msg.TransactionDate,
                CreatedBy = msg.CreatedBy,
                TransactionType = "Purchase"
            };

            var command = new RecordSupplierPurchaseCommand(dto);
            await _mediator.Send(command);
        }
    }

    public class SupplierPurchaseReturnCreatedConsumer : IConsumer<SupplierPurchaseReturnCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SupplierPurchaseReturnCreatedConsumer> _logger;

        public SupplierPurchaseReturnCreatedConsumer(IMediator mediator, ILogger<SupplierPurchaseReturnCreatedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SupplierPurchaseReturnCreatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation(">>> RabbitMQ: Consuming SupplierPurchaseReturnCreatedEvent for Supplier: {SupplierId}, Ref: {ReferenceId}", msg.SupplierId, msg.ReferenceId);

            var dto = new SupplierPurchaseDto
            {
                SupplierId = msg.SupplierId,
                CompanyId = msg.CompanyId ?? Guid.Empty,
                BranchId = msg.BranchId,
                Amount = msg.Amount,
                ReferenceId = msg.ReferenceId,
                Description = msg.Description,
                TransactionDate = msg.TransactionDate,
                CreatedBy = msg.CreatedBy,
                TransactionType = "DebitNote"
            };

            var command = new RecordSupplierPurchaseCommand(dto);
            await _mediator.Send(command);
        }
    }

    public class SupplierPaymentCreatedConsumer : IConsumer<SupplierPaymentCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SupplierPaymentCreatedConsumer> _logger;

        public SupplierPaymentCreatedConsumer(IMediator mediator, ILogger<SupplierPaymentCreatedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SupplierPaymentCreatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation(">>> RabbitMQ: Consuming SupplierPaymentCreatedEvent for Supplier: {SupplierId}, Ref: {ReferenceNumber}", msg.SupplierId, msg.ReferenceNumber);

            var dto = new SupplierPaymentDto
            {
                SupplierId = msg.SupplierId,
                CompanyId = msg.CompanyId ?? Guid.Empty,
                BranchId = msg.BranchId,
                Amount = msg.Amount,
                PaymentDate = msg.PaymentDate,
                PaymentMode = msg.PaymentMode,
                ReferenceNumber = msg.ReferenceNumber,
                Remarks = msg.Remarks,
                CreatedBy = msg.CreatedBy,
                TransactionType = msg.TransactionType ?? "Payment"
            };

            var command = new RecordSupplierPaymentCommand(dto);
            await _mediator.Send(command);
        }
    }
}
