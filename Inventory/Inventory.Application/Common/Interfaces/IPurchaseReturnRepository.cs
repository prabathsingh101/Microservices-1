using Inventory.Application.PurchaseReturn;
using Inventory.Application.PurchaseReturn.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inventory.Application.Common.Interfaces;

public interface IPurchaseReturnRepository
{
    // 1. Supplier select karte hi uske rejected items lane ke liye
    Task<List<RejectedItemDto>> GetRejectedItemsBySupplierAsync(Guid supplierId);

    Task<List<SupplierSelectDto>> GetSuppliersForPurchaseReturnAsync();

    // 1.2 Received stock return ke liye items fetch karna
    Task<List<ReceivedStockDto>> GetReceivedStockBySupplierAsync(Guid supplierId);

    // 2. Form se pura data save karne aur stock update karne ke liye [cite: 2026-02-03]
    Task<bool> CreatePurchaseReturnAsync(Inventory.Domain.Entities.PurchaseReturn returnData);

    Task<PurchaseReturnPagedResponse> GetPurchaseReturnsAsync(
       string? search,
       int pageIndex,
       int pageSize,
       DateTime? fromDate = null,
       DateTime? toDate = null,
       string? status = null,
       string? sortField = "ReturnDate",
       string? sortOrder = "desc",
       bool isQuick = false);

    Task<PurchaseReturnDetailDto?> GetPurchaseReturnByIdAsync(Guid id);

    Task<byte[]> ExportPurchaseReturnsToExcelAsync(DateTime? fromDate, DateTime? toDate);

    Task<List<PendingPRDto>> GetPendingPurchaseReturnsAsync();
    Task<PurchaseReturnSummaryDto> GetPurchaseReturnSummaryAsync(bool isQuick = false);
    Task<bool> BulkOutwardAsync(List<Guid> ids);
}
