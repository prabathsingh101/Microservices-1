using Inventory.Application.GRN.DTOs.Stock;
using MediatR;

public record GetCurrentStockCommand(
    string? Search,
    string? SortField,
    string? SortOrder,
    int PageIndex,
    int PageSize,
    DateTime? StartDate, // Added
    DateTime? EndDate,    // Added
    Guid? WarehouseId = null,
    Guid? RackId = null,
    bool ShowPurged = false,
    string? BranchId = null
) : IRequest<StockPagedResponseDto>;
