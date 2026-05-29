using Inventory.Application.GRN.DTOs;
using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Common.Interfaces
{
    public interface IGRNRepository
    {
        Task<POForGRNDTO?> GetPODataForGRN(string poIds, Guid? grnHeaderId = null, string? gatePassNo = null);
        Task<string> GenerateGRNNumber();
        Task<string> SaveGRNWithStockUpdate(GRNHeader header, List<GRNDetail> details);
        Task<GRNPagedResponseDto> GetGRNPagedListAsync(string search, string sortField, string sortOrder, int pageIndex, int pageSize, bool isQuick = false);

        Task<GrnPrintDto?> GetGrnDetailsByNumberAsync(string grnNumber, Guid? companyId = null);

        Task<bool> CreateBulkGrnFromPoAsync(BulkGrnRequestDto request);
        Task<List<GrnRejectionHistoryDto>> GetGrnRejectionHistoryAsync(string grnNumber);
        Task<bool> CancelGRNWithStockReversal(Guid grnId);
        Task<GRNHeader> GetGrnBasicDetailsAsync(Guid grnId);
    }
}
