using MediatR;

public record GetPurchaseOrdersQuery(
    int PageIndex,
    int PageSize,
    string? SortField, // ? add karein
    string? SortOrder, // ? add karein
    string? Filter     // ? add karein
) : IRequest<PagedResponse<PurchaseOrderDto>>;
