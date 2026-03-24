using Inventory.Application.SaleOrders.DTOs;
using Inventory.Application.SaleOrders.SaleReturn.DTOs;
using Inventory.Domain.Entities;

namespace Inventory.Application.Common.Interfaces;

public interface ISaleReturnRepository
{
    Task<SaleReturnPagedResponse> GetSaleReturnsAsync(
       string? search,
       string? status, // Parameter added
       int pageIndex,
       int pageSize,
       DateTime? fromDate,
       DateTime? toDate,
       string sortField,
       string sortOrder,
       bool isQuick = false);

    Task<bool> CreateSaleReturnAsync(SaleReturnHeader returnHeader);
    Task<decimal> GetRemainingReturnableQtyAsync(int saleOrderId, Guid productId, DateTime? mfgDate = null, DateTime? expDate = null);

    Task<List<SaleReturnExportDto>> GetExportDataAsync(DateTime? fromDate, DateTime? toDate);

    Task<SaleReturnSummaryDto> GetDashboardSummaryAsync();
    Task<List<PendingSRDto>> GetPendingSaleReturnsAsync();
    Task<SaleReturnHeader?> GetSaleReturnByIdAsync(int id);
    Task<bool> BulkInwardAsync(List<int> ids);
}
