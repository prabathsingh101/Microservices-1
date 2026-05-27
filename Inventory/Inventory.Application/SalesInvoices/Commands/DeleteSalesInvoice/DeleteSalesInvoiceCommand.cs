using MediatR;
using System;

namespace Inventory.Application.SalesInvoices.Commands.DeleteSalesInvoice
{
    public class DeleteSalesInvoiceCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }

        public DeleteSalesInvoiceCommand(Guid id, string? reason)
        {
            Id = id;
            Reason = reason;
        }
    }
}
