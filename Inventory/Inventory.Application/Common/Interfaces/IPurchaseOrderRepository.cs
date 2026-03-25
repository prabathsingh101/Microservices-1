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
        Task<PurchaseOrder?> GetByIdWithItemsAsync(int id, CancellationToken ct);
        void Update(PurchaseOrder po);
        void RemoveItem(PurchaseOrderItem item);
        Task<bool> DeleteItemAsync(int itemId);
        public Task<bool> BulkDeleteItemsAsync(List<int> itemIds);
        Task UpdatePOTotalsAsync(int poId);
        Task<PurchaseOrder> GetByIdAsync(int id);
        Task<PurchaseOrder> GetByIdAsyncForUpdateStatus(int id);
        void Delete(PurchaseOrder po);
        Task<List<PurchaseOrder>> GetByIdsAsync(List<int> ids);
        Task UpdateAsync(PurchaseOrder po);
        Task<bool> UpdatePOStatusAsync(int id, string status);
        Task<bool> SaveChangesAsync();
        
        Task<IEnumerable<PendingPODto>> GetPendingPurchaseOrdersAsync();

        Task<IEnumerable<POItemForGRNDto>> GetPOItemsForGRNAsync(int poId);

        Task<POHeaderDetailsDto?> GetPOHeaderAsync(int lastPurchaseOrderId);

        Task<ProductPriceDto?> GetPriceListRateAsync( Guid productId, Guid priceListId);

        Task<bool> BulkSentForApprovalAsync(List<long> ids);

        Task<bool> BulkApprovePOsAsync(List<long> ids, string approvedBy);

        Task<bool> BulkRejectPOsAsync(List<long> ids, string rejectedBy);

        Task<PODocumentDto> GetPODetailsForPrintAsync(long id);

        Task<PORepoPrintResponse> GeneratePOReportPdfAsync(long id);
        Task<decimal> GetTotalReturnedQtyAsync(int poId);
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct);
    }
}
