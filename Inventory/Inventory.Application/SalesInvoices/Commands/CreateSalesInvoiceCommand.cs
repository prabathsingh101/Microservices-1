using MediatR;
using Inventory.Application.SalesInvoices.DTOs;

namespace Inventory.Application.SalesInvoices.Commands
{
    public class CreateSalesInvoiceCommand : IRequest<object>
    {
        public CreateSalesInvoiceDto InvoiceDto { get; set; }

        public CreateSalesInvoiceCommand(CreateSalesInvoiceDto invoiceDto)
        {
            InvoiceDto = invoiceDto;
        }
    }
}
