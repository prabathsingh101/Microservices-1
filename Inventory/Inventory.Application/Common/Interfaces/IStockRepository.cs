using Inventory.Application.GRN.DTOs.Stock;
using System;
using System.Collections.Generic;
using System.Text;

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
            Guid? rackId = null
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
    }
}
