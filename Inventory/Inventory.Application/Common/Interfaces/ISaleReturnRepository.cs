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
    Task<decimal> GetRemainingReturnableQtyAsync(Guid saleOrderId, Guid productId, DateTime? mfgDate = null, DateTime? expDate = null);

    Task<List<SaleReturnExportDto>> GetExportDataAsync(DateTime? fromDate, DateTime? toDate);

    Task<SaleReturnSummaryDto> GetDashboardSummaryAsync(bool isQuick = false);
    Task<List<PendingSRDto>> GetPendingSaleReturnsAsync();
    Task<SaleReturnHeader?> GetSaleReturnByIdAsync(Guid id);
    Task<bool> BulkInwardAsync(List<Guid> ids);
    Task<bool> CancelSaleReturnAsync(Guid id, string? reason);
    Task<bool> MarkAsRefundedAsync(string returnNumber);

}
