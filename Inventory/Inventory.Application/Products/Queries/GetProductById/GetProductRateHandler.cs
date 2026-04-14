
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.DTOs;
using Inventory.Application.Products.Queries.GetProductById;
using MediatR;

public class GetProductRateHandler : IRequestHandler<GetProductRateQuery, ProductRateDto>
{
    private readonly IProductRepository _repository;

    public GetProductRateHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductRateDto> Handle(GetProductRateQuery request, CancellationToken cancellationToken)
    {
        // 1. Repository se poora data fetch karein (PriceList + Master details)
        var result = await _repository.GetProductRateAsync(request.ProductId, request.PriceListId);

        if (result == null)
        {
            throw new Exception("Product data or rate not found.");
        }

        // 2. Direct result return karein, kyunki ye pehle se hi ProductRateDto hai
        return result;
    }
}
