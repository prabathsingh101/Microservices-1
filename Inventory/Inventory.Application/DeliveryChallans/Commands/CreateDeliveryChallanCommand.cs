using MediatR;
using Inventory.Application.DeliveryChallans.DTOs;

namespace Inventory.Application.DeliveryChallans.Commands
{
    public class CreateDeliveryChallanCommand : IRequest<object>
    {
        public DeliveryChallanDto ChallanDto { get; set; }
        public CreateDeliveryChallanCommand(DeliveryChallanDto challanDto)
        {
            ChallanDto = challanDto;
        }
    }
}
