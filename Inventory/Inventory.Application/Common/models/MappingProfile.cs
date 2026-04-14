using AutoMapper;

namespace Inventory.Application.Common.models
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<PurchaseOrder, PurchaseOrderItemDto>();
            CreateMap<PurchaseOrderItem, PurchaseOrderItemDto>();
        }
    }
}
