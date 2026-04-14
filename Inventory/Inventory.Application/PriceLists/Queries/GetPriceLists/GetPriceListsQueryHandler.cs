using MediatR;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.PriceLists.DTOs;

namespace Inventory.Application.PriceLists.Queries.GetPriceLists;

public sealed class GetPriceListsQueryHandler
    : IRequestHandler<GetPriceListsQuery, List<PriceListDto>>
{
    private readonly IPriceListRepository _repository;

    public GetPriceListsQueryHandler(IPriceListRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PriceListDto>> Handle(
        GetPriceListsQuery request,
        CancellationToken cancellationToken)
    {
        var lists = await _repository.GetAllAsync();

        return lists.Select(pl => new PriceListDto
        {
            id = pl.Id,
            name = pl.Name,
            code = pl.Code,
            priceType=pl.PriceType,   
            validFrom = pl.ValidFrom,
            validTo = pl.ValidTo,
            isActive = pl.IsActive,
            currency = pl.Currency,
            applicableGroup = pl.ApplicableGroup,
            remarks = pl.Remarks,
            createdOn = pl.CreatedOn,
            createdBy = pl.CreatedBy,
            ModifiedOn = pl.ModifiedOn,
            ModifiedBy = pl.ModifiedBy,
          
        }).ToList();
    }
}
