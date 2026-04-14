using Inventory.Application.PriceLists.DTOs;
using MediatR;

namespace Inventory.Application.PriceLists.Commands.UpdatePriceList;

public record UpdatePriceListCommand : IRequest<bool>
{
    public Guid id { get; init; }
    public string name { get; init; } // Lowercase 'n' check karein
    public string code { get; init; }
    public string priceType { get; init; }
    public DateTime validFrom { get; init; }
    public DateTime? validTo { get; init; }
    public bool isActive { get; init; }
    public string remarks { get; init; }
    public List<PriceListItemUpdateDto> priceListItems { get; init; }
}
