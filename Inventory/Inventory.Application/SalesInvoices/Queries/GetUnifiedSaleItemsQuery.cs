using System;
using System.Collections.Generic;
using Inventory.Application.SalesInvoices.DTOs;
using MediatR;

namespace Inventory.Application.SalesInvoices.Queries
{
    public class GetUnifiedSaleItemsQuery : IRequest<List<UnifiedSaleItemDto>>
    {
        public Guid Id { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
