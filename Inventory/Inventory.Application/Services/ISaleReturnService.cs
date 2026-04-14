using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Services
{
    public interface ISaleReturnService
    {
  

        Task<bool> SaveReturnAsync(CreateSaleReturnDto dto);
        Task<CreditNotePrintDto?> GetPrintDataAsync(Guid id);

        Task<byte[]> GenerateExcelExportAsync(DateTime? fromDate, DateTime? toDate);
    }
}
