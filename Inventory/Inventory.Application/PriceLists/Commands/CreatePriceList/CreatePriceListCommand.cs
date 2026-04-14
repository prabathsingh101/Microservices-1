using Inventory.Application.PriceLists.DTOs;
using MediatR;

public record CreatePriceListCommand(
    string name,
    string code,
    string priceType,
    string applicableGroup,
    string currency,
    DateTime validFrom,
    DateTime? validTo,
    string? remarks,
    bool isActive,
    string createdBy,
    List<CreatePriceListItemDto> priceListItems
) : IRequest<Guid>;
