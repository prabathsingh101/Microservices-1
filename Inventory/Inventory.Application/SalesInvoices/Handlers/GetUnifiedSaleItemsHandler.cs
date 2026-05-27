using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.SalesInvoices.DTOs;
using Inventory.Application.SalesInvoices.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.SalesInvoices.Handlers
{
    public class GetUnifiedSaleItemsHandler : IRequestHandler<GetUnifiedSaleItemsQuery, List<UnifiedSaleItemDto>>
    {
        private readonly IInventoryDbContext _context;

        public GetUnifiedSaleItemsHandler(IInventoryDbContext context)
        {
            _context = context;
        }

        public async Task<List<UnifiedSaleItemDto>> Handle(GetUnifiedSaleItemsQuery request, CancellationToken cancellationToken)
        {
            if (request.Source == "QuickSale")
            {
                return await _context.SaleOrderItems
                    .AsNoTracking()
                    .Where(x => x.SaleOrderId == request.Id)
                    .Select(x => new UnifiedSaleItemDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        Qty = x.Qty,
                        Unit = x.Unit ?? string.Empty,
                        Rate = x.Rate,
                        TaxAmount = x.TaxAmount,
                        Total = x.Total,
                        MfgDate = x.MfgDate,
                        ExpDate = x.ExpDate
                    })
                    .ToListAsync(cancellationToken);
            }
            else if (request.Source == "TaxInvoice")
            {
                return await _context.SalesInvoiceItems
                    .AsNoTracking()
                    .Where(x => x.SalesInvoiceId == request.Id)
                    .Select(x => new UnifiedSaleItemDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        Qty = x.Qty,
                        Unit = x.Unit ?? string.Empty,
                        Rate = x.Rate,
                        TaxAmount = x.TaxAmount,
                        Total = x.Total,
                        MfgDate = x.MfgDate,
                        ExpDate = x.ExpDate
                    })
                    .ToListAsync(cancellationToken);
            }

            return new List<UnifiedSaleItemDto>();
        }
    }
}
