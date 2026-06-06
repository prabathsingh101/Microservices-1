using ClosedXML.Excel;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace Inventory.Application.Services;

using Inventory.Application.Clients; // Added namespace for ICompanyClient

public class SaleReturnService : ISaleReturnService
{
    private readonly Inventory.Application.Common.Interfaces.ISaleReturnRepository _repository;
    private readonly IInventoryDbContext _context;

    private readonly ICustomerHttpService _customerHttpService;
    private readonly ICompanyClient _companyClient; // New client

    public SaleReturnService(
        ISaleReturnRepository repository,
        IInventoryDbContext context,
        ICustomerHttpService httpService,
        ICompanyClient companyClient // Injected
        )
    {
        _repository = repository;
        _context = context;
        _customerHttpService = httpService;
        _companyClient = companyClient;
    }



    public async Task<bool> SaveReturnAsync(CreateSaleReturnDto dto)
    {
        // Item level calculations pehle kar lete hain
        var returnItems = dto.Items.Where(i => i.ReturnQty > 0).Select(i =>
        {
            var totalAmount = i.TotalAmount; // from frontend
            var taxAmount = totalAmount - (totalAmount * 100m / (100m + i.TaxPercentage));

            return new SaleReturnItem
            {
                ProductId = i.ProductId,
                ReturnQty = i.ReturnQty,
                UnitPrice = i.UnitPrice,
                TaxPercentage = i.TaxPercentage,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                Reason = i.Reason,
                ItemCondition = i.ItemCondition
            };
        }).ToList();

        var entity = new SaleReturnHeader
        {
            ReturnNumber = "SR-" + DateTime.Now.ToString("yyyyMMddHHmm"),
            ReturnDate = dto.ReturnDate,
            SaleOrderId = dto.SaleOrderId,
            CustomerId = dto.CustomerId,
            Remarks = dto.Remarks,
            Status = "Confirmed",

            // Header Level Calculations
            SubTotal = returnItems.Sum(x => x.TotalAmount - x.TaxAmount),
            TaxAmount = returnItems.Sum(x => x.TaxAmount),
            TotalAmount = returnItems.Sum(x => x.TotalAmount), // Final Amount

            ReturnItems = returnItems
        };

        return await _repository.CreateSaleReturnAsync(entity);
    }

    public async Task<CreditNotePrintDto?> GetPrintDataAsync(Guid id)
    {
        // 1. Database se core data fetch karein (SaleOrders ke saath Join karke)
        var data = await _context.SaleReturnHeaders
            .AsNoTracking()
            .Include(h => h.ReturnItems)
            .ThenInclude(i => i.Product)
            .Where(h => h.Id == id)
            .Select(h => new CreditNotePrintDto
            {
                ReturnNumber = h.ReturnNumber,
                ReturnDate = h.ReturnDate,
                CustomerId = h.CustomerId,

                // Fix: Check both SaleOrders and SalesInvoices for reference number
                SONumber = _context.SaleOrders
                            .Where(so => so.Id == h.SaleOrderId)
                            .Select(so => so.SONumber)
                            .FirstOrDefault() ?? 
                           _context.SalesInvoices
                            .Where(si => si.Id == h.SaleOrderId)
                            .Select(si => si.InvoiceNo)
                            .FirstOrDefault() ?? "N/A",

                SubTotal = h.SubTotal,     // Database mapping
                TotalDiscount = h.DiscountAmount, // New Mapping
                TotalTax = h.TaxAmount,    // Database mapping
                GrandTotal = h.TotalAmount, // Database mapping

                Items = h.ReturnItems.Select(i => new ReturnItemPrintDto
                {
                    ProductName = i.Product.Name,
                    Qty = i.ReturnQty,
                    Rate = i.UnitPrice,
                    DiscountPercent = i.DiscountPercent,
                    DiscountAmount = i.DiscountAmount,
                    TaxPercent = i.TaxPercentage,
                    Total = i.TotalAmount,
                    MfgDate = i.MfgDate,
                    ExpDate = i.ExpDate,
                    IsExpiryRequired = i.Product.IsExpiryRequired
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (data == null) return null;

        Console.WriteLine($"[GetPrintDataAsync] ID: {id}, Total: {data.GrandTotal}, TotalDiscount: {data.TotalDiscount}");
        foreach (var item in data.Items)
        {
            Console.WriteLine($"[GetPrintDataAsync] Item: {item.ProductName}, Qty: {item.Qty}, Rate: {item.Rate}, Disc%: {item.DiscountPercent}");
        }

        // 2. Customer Name laane ke liye Helper Method (Dictionary Logic)
        if (data.CustomerId.HasValue && data.CustomerId.Value != Guid.Empty)
        {
            var customerIds = new List<Guid> { data.CustomerId.Value };
            var customerNames = await _customerHttpService.GetCustomerNamesAsync(customerIds);

            if (customerNames != null && customerNames.ContainsKey(data.CustomerId.Value))
            {
                data.CustomerName = customerNames[data.CustomerId.Value];
            }
            else
            {
                data.CustomerName = "Unknown Customer";
            }
        }
        else
        {
            // Walking customer logical mapping from header or default
            data.CustomerName = "Cash Customer";
        }

        // 3. Company Info fetch karein [New Feature]
        try 
        {
            var companyProfile = await _companyClient.GetCompanyProfileAsync();
            data.CompanyInfo = companyProfile;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Company Profile load nahi ho paya: " + ex.Message);
        }

        return data;
    }



    public async Task<byte[]> GenerateExcelExportAsync(DateTime? fromDate, DateTime? toDate)
    {
        var data = await _repository.GetExportDataAsync(fromDate, toDate);

        // Microservice se names laao [cite: 2026-02-06]
        var customerIds = data.Select(x => Guid.TryParse(x.CustomerName, out Guid g) ? g : Guid.Empty)
                              .Where(g => g != Guid.Empty).Distinct().ToList();
        var customerNames = await _customerHttpService.GetCustomerNamesAsync(customerIds);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Sale Returns");

            // Header Row
            worksheet.Cell(1, 1).Value = "Return No.";
            worksheet.Cell(1, 2).Value = "Date";
            worksheet.Cell(1, 3).Value = "Customer";
            worksheet.Cell(1, 4).Value = "SO Ref";
            worksheet.Cell(1, 5).Value = "Total";
            worksheet.Cell(1, 6).Value = "Status";

            int row = 2;
            foreach (var item in data)
            {
                Guid cId = Guid.TryParse(item.CustomerName, out Guid g) ? g : Guid.Empty;
                worksheet.Cell(row, 1).Value = item.ReturnNumber;
                worksheet.Cell(row, 2).Value = item.ReturnDate;
                // Name replace karein [cite: 2026-02-06]
                worksheet.Cell(row, 3).Value = customerNames.GetValueOrDefault(cId, "Unknown");
                worksheet.Cell(row, 4).Value = item.SONumber;
                worksheet.Cell(row, 5).Value = item.TotalAmount;
                worksheet.Cell(row, 6).Value = item.Status;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
    }

}
