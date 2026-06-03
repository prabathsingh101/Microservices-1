using MediatR;

namespace Inventory.Application.DeliveryChallans.Commands
{
    public class CancelDeliveryChallanCommand : IRequest<object>
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }

        public CancelDeliveryChallanCommand(Guid id, string? reason = null)
        {
            Id = id;
            Reason = reason;
        }
    }
}
