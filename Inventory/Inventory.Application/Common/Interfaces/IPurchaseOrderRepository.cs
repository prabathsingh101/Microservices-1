using Inventory.Application.PurchaseOrders.DTOs;
using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Common.Interfaces
{
    public interface IPurchaseOrderRepository
    {
        Task AddAsync(PurchaseOrder po, CancellationToken ct);
        Task<(IEnumerable<PurchaseOrder> Items, int TotalCount)> GetPagedOrdersAsync(
          int pageIndex,
          int pageSize,
          string sortField,
          string sortOrder,
          string filter);

        Task<(IEnumerable<PurchaseOrder> Data, int Total, decimal TotalAmount, int TodayCount, int MonthCount)> GetDateRangePagedOrdersAsync(GetPurchaseOrdersRequest request);
        Task<PurchaseOrder?> GetByIdWithItemsAsync(Guid id, CancellationToken ct);
        void Update(PurchaseOrder po);
        void RemoveItem(PurchaseOrderItem item);
        Task<bool> DeleteItemAsync(Guid itemId);
        public Task<bool> BulkDeleteItemsAsync(List<Guid> itemIds);
        Task UpdatePOTotalsAsync(Guid poId);
        Task<PurchaseOrder> GetByIdAsync(Guid id);
        Task<PurchaseOrder> GetByIdAsyncForUpdateStatus(Guid id);
        void Delete(PurchaseOrder po);
        Task<List<PurchaseOrder>> GetByIdsAsync(List<Guid> ids);
        Task UpdateAsync(PurchaseOrder po);
        Task<bool> UpdatePOStatusAsync(Guid id, string status);
        Task<bool> SaveChangesAsync();
        
        Task<IEnumerable<PendingPODto>> GetPendingPurchaseOrdersAsync();

        Task<IEnumerable<POItemForGRNDto>> GetPOItemsForGRNAsync(Guid poId);

        Task<POHeaderDetailsDto?> GetPOHeaderAsync(Guid lastPurchaseOrderId);

        Task<ProductPriceDto?> GetPriceListRateAsync(Guid productId, Guid priceListId, string? type = null);

        Task<bool> BulkSentForApprovalAsync(List<Guid> ids);

        Task<List<PurchaseOrderLookupDto>> GetOrdersBySupplierAsync(Guid supplierId);
        Task<List<PurchaseOrderLookupDto>> GetCancelledOrdersBySupplierAsync(Guid supplierId);

        Task<bool> BulkApprovePOsAsync(List<Guid> ids, string approvedBy);

        Task<bool> BulkRejectPOsAsync(List<Guid> ids, string rejectedBy);

        Task<PODocumentDto> GetPODetailsForPrintAsync(Guid id);

        Task<PORepoPrintResponse> GeneratePOReportPdfAsync(Guid id);
        Task<bool> ToggleDispatchStatusAsync(Guid id);
        Task<decimal> GetTotalReturnedQtyAsync(Guid poId);
        Task<bool> ShortCloseOrderAsync(Guid id);
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct);
    }
}
