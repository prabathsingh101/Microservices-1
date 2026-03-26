using Inventory.Application.GRN.DTOs.Stock;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Application.Common.Interfaces
{
    public interface IStockRepository
    {
        Task<StockPagedResponseDto> GetCurrentStockAsync(
            string? search,
            string? sortField,
            string? sortOrder,
            int pageIndex,
            int pageSize,
            DateTime? startDate,
            DateTime? endDate,
            Guid? warehouseId = null,
            Guid? rackId = null,
            bool showPurged = false
        );

        Task<StockPagedResponseDto> GetDisposedStockAsync(
            string? search,
            string? sortField,
            string? sortOrder,
            int pageIndex,
            int pageSize,
            DateTime? startDate,
            DateTime? endDate,
            Guid? warehouseId = null,
            Guid? rackId = null
        );

        Task<StockRefillDetailsDto> GetRefillDetailsAsync(Guid productId);

        Task<byte[]> GenerateStockExcel(List<Guid> productIds);

        Task<List<BatchTransactionDto>> GetBatchTransactionsAsync(
            Guid productId,
            Guid warehouseId,
            Guid rackId,
            DateTime? mfgDate,
            DateTime? expDate);
    }
}
