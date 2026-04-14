using Inventory.Application.DTOs.SaleOrder;
using Inventory.Application.GRN.DTOs.Stock;
using Inventory.Application.SaleOrders.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Domain.Entities.SO;

namespace Inventory.Application.Common.Interfaces
{
    public interface ISaleOrderRepository
    {
        Task<Guid> SaveAsync(SaleOrder order);
        Task UpdateAsync(SaleOrder order);
        Task<string> GetLastSONumberAsync();


        Task<decimal> GetAvailableStockAsync(Guid productId);
        Task UpdateProductStockAsync(Guid productId, decimal adjustmentQty);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task ExecuteInTransactionAsync(Func<Task> action);

        Task<List<StockExportDto>> GetSaleReportDataAsync(List<Guid> orderIds);

        // Naya signature jo parameters ke saath total count aur Global Stats bhi return karega
        Task<(List<SaleOrderListDto> Data, int TotalCount, decimal TotalSalesAmount, int PendingDispatchCount, int UnpaidOrdersCount, int TodayCount, int MonthCount)> GetAllSaleOrdersAsync(
            string searchTerm,
            int pageNumber,
            int pageSize,
            string sortBy,
            string sortOrder,
            bool isQuick = false);

        Task<bool> UpdateSaleOrderStatusAsync(Guid id, string status);

        Task<SaleOrderDetailDto?> GetSaleOrderByIdAsync(Guid id);

        Task<List<SaleOrderLookupDto>> GetOrdersByCustomerAsync(Guid customerId);

        Task<List<SaleOrderItemGridDto>> GetItemsForGridByOrderIdAsync(Guid saleOrderId);
        Task<bool> DeleteAsync(Guid id);
        Task<List<PendingSODto>> GetPendingSaleOrdersAsync();
    }
}
