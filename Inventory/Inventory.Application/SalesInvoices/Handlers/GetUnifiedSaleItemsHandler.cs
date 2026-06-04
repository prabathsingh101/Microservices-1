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
                return await (from item in _context.SaleOrderItems
                              join wh in _context.Warehouses on item.WarehouseId equals wh.Id into whJoin
                              from wh in whJoin.DefaultIfEmpty()
                              join r in _context.Racks on item.RackId equals r.Id into rJoin
                              from r in rJoin.DefaultIfEmpty()
                              where item.SaleOrderId == request.Id
                              select new UnifiedSaleItemDto
                              {
                                  Id = item.Id,
                                  ProductId = item.ProductId,
                                  ProductName = item.ProductName,
                                  Qty = item.Qty,
                                  Unit = item.Unit ?? string.Empty,
                                  Rate = item.Rate,
                                  TaxAmount = item.TaxAmount,
                                  Total = item.Total,
                                  MfgDate = item.MfgDate,
                                  ExpDate = item.ExpDate,
                                  Location = wh != null ? wh.Name : "NA",
                                  Rack = r != null ? r.Name : "NA",
                                  MRP = item.MRP,
                                  DiscountPercent = item.DiscountPercent,
                                  DiscountAmount = item.DiscountAmount,
                                  GSTPercent = item.GSTPercent,
                                  BatchNumber = item.BatchNumber
                              })
                    .ToListAsync(cancellationToken);
            }
            else if (request.Source == "TaxInvoice")
            {
                return await (from item in _context.SalesInvoiceItems
                              join wh in _context.Warehouses on item.WarehouseId equals wh.Id into whJoin
                              from wh in whJoin.DefaultIfEmpty()
                              join r in _context.Racks on item.RackId equals r.Id into rJoin
                              from r in rJoin.DefaultIfEmpty()
                              where item.SalesInvoiceId == request.Id
                              select new UnifiedSaleItemDto
                              {
                                  Id = item.Id,
                                  ProductId = item.ProductId,
                                  ProductName = item.ProductName,
                                  Qty = item.Qty,
                                  Unit = item.Unit ?? string.Empty,
                                  Rate = item.Rate,
                                  TaxAmount = item.TaxAmount,
                                  Total = item.Total,
                                  MfgDate = item.MfgDate,
                                  ExpDate = item.ExpDate,
                                  Location = wh != null ? wh.Name : "NA",
                                  Rack = r != null ? r.Name : "NA",
                                  MRP = item.MRP,
                                  DiscountPercent = item.DiscountPercent,
                                  DiscountAmount = item.DiscountAmount,
                                  GSTPercent = item.GSTPercent,
                                  BatchNumber = item.BatchNumber
                              })
                    .ToListAsync(cancellationToken);
            }

            return new List<UnifiedSaleItemDto>();
        }
    }
}
