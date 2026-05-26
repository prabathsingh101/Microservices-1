using MediatR;
using System;

namespace Inventory.Application.PurchaseOrders.Commands.MakePayment
{
    public class RecordPurchasePaymentCommand : IRequest<bool>
    {
        public Guid PurchaseOrderId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMode { get; set; }
        public string? Remarks { get; set; }
    }
}
