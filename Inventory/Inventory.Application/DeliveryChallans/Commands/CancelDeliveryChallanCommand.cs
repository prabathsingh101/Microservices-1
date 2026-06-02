using MediatR;

namespace Inventory.Application.DeliveryChallans.Commands
{
    public class CancelDeliveryChallanCommand : IRequest<object>
    {
        public Guid Id { get; set; }

        public CancelDeliveryChallanCommand(Guid id)
        {
            Id = id;
        }
    }
}
