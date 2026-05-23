using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Clients;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Inventory.Domain.Entities.SalesInvoice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application.SalesInvoices.Commands
{
    public class CreateSalesInvoiceHandler : IRequestHandler<CreateSalesInvoiceCommand, object>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICustomerClient _customerClient;
        private readonly ICompanyClient _companyClient;
        private readonly IServiceScopeFactory _scopeFactory;

        public CreateSalesInvoiceHandler(
            IInventoryDbContext context,
            ICustomerClient customerClient,
            ICompanyClient companyClient,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _customerClient = customerClient;
            _companyClient = companyClient;
            _scopeFactory = scopeFactory;
        }

        public async Task<object> Handle(CreateSalesInvoiceCommand request, CancellationToken cancellationToken)
        {
            var dto = request.InvoiceDto;

            // 1. Generate Invoice No if missing
            string invoiceNo = dto.InvoiceNo;
            if (string.IsNullOrEmpty(invoiceNo))
            {
                var lastInvoice = await _context.SalesInvoices
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefaultAsync(cancellationToken);
                
                int nextId = lastInvoice == null ? 1 : int.Parse(lastInvoice.InvoiceNo.Split('/').Last()) + 1;
                string fyString = $"{DateTime.Now.Year}-{(DateTime.Now.Year + 1).ToString().Substring(2)}";
                invoiceNo = $"INV/{fyString}/{nextId:D4}";
            }

            // Map DTO to Entity
            var invoice = new SalesInvoice
            {
                Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                InvoiceNo = invoiceNo,
                InvoiceDate = dto.InvoiceDate,
                CustomerId = dto.CustomerId,
                SubTotal = dto.SubTotal,
                TotalTax = dto.TotalTax,
                GrandTotal = dto.GrandTotal,
                TaxType = dto.TaxType,
                IgstAmount = dto.IgstAmount ?? (dto.TaxType?.ToLower() == "interstate" ? dto.TotalTax : 0M),
                CgstAmount = dto.CgstAmount ?? (dto.TaxType?.ToLower() == "local" || string.IsNullOrEmpty(dto.TaxType) ? dto.TotalTax / 2 : 0M),
                SgstAmount = dto.SgstAmount ?? (dto.TaxType?.ToLower() == "local" || string.IsNullOrEmpty(dto.TaxType) ? dto.TotalTax / 2 : 0M),
                Remarks = dto.Remarks ?? "Tax Invoice",
                Status = dto.Status ?? "Confirmed",
                IsQuick = dto.IsQuick,
                GuestName = dto.GuestName,
                GuestPhone = dto.GuestPhone,
                CompanyId = dto.CompanyId ?? Guid.Empty,
                BranchId = dto.BranchId,
                CreatedBy = dto.CreatedBy,
                Items = dto.Items.Select(i => new SalesInvoiceItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Qty = i.Qty,
                    Unit = i.Unit,
                    Rate = i.Rate,
                    MRP = i.MRP,
                    DiscountAmount = i.DiscountAmount,
                    DiscountPercent = i.DiscountPercent,
                    GSTPercent = i.GstPercent,
                    TaxAmount = i.TaxAmount,
                    Total = i.Total,
                    WarehouseId = i.WarehouseId,
                    RackId = i.RackId,
                    BatchNumber = i.BatchNumber,
                    ReferenceNumber = i.ReferenceNumber,
                    MfgDate = i.ManufacturingDate,
                    ExpDate = i.ExpiryDate,
                    CompanyId = dto.CompanyId ?? Guid.Empty,
                    BranchId = dto.BranchId
                }).ToList()
            };

            await _context.SalesInvoices.AddAsync(invoice, cancellationToken);

            // Deduct Stock
            foreach (var item in invoice.Items)
            {
                var saleTx = new InventoryTransaction(
                    item.ProductId,
                    -item.Qty,
                    invoice.IsQuick ? "QuickSaleInvoice" : "SaleInvoice",
                    invoice.InvoiceNo,
                    item.WarehouseId,
                    item.RackId,
                    item.MfgDate,
                    item.ExpDate,
                    invoice.CompanyId,
                    invoice.BranchId,
                    item.ReferenceNumber,
                    item.BatchNumber
                );
                await _context.InventoryTransactions.AddAsync(saleTx, cancellationToken);

                if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                {
                    var whStock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId, cancellationToken);

                    if (whStock != null)
                    {
                        whStock.Quantity -= item.Qty;
                    }
                    else
                    {
                        await _context.WarehouseStocks.AddAsync(new WarehouseStock
                        {
                            ProductId = item.ProductId,
                            WarehouseId = item.WarehouseId.Value,
                            Quantity = -item.Qty,
                            MinStock = 0,
                            CompanyId = invoice.CompanyId,
                            BranchId = invoice.BranchId
                        }, cancellationToken);
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Record in Finance Ledger
            if (invoice.CustomerId.HasValue && invoice.CustomerId.Value != Guid.Empty)
            {
                try
                {
                    await _customerClient.RecordSaleAsync(
                        invoice.CustomerId.Value,
                        invoice.GrandTotal,
                        invoice.InvoiceNo,
                        $"Tax Invoice generated: {invoice.InvoiceNo}",
                        invoice.CreatedBy ?? "System",
                        Guid.TryParse(invoice.BranchId, out var branchId) ? branchId : (Guid?)null,
                        invoice.CompanyId
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ledger sync failed: {ex.Message}");
                }
            }

            return new { Id = invoice.Id, InvoiceNo = invoice.InvoiceNo };
        }
    }
}
