using MediatR;
using Suppliers.Application.DTOs;
using System;
using System.Collections.Generic;

namespace Suppliers.Application.Features.Suppliers.Queries
{
    public record GetSupplierLedgerQuery(SupplierLedgerRequestDto Request) : IRequest<SupplierLedgerPagedResultDto>;

    public record GetPendingDuesQuery(string? BranchId = null, string? CompanyId = null) : IRequest<List<PendingDueDto>>;

    public record GetTotalPaymentsQuery(DateRangeDto DateRange) : IRequest<decimal>;

    public record GetTotalAdjustmentsQuery(DateRangeDto DateRange) : IRequest<AdjustmentsSummaryDto>;
    
    public record GetGRNPaymentStatusesQuery(List<string> GrnNumbers) : IRequest<Dictionary<string, decimal>>;

    public record GetPaymentsReportQuery(PaymentReportRequestDto Request) : IRequest<PaginatedListDto<PaymentReportDto>>;
    public record GetTotalPendingDuesQuery(string? BranchId = null, string? CompanyId = null) : IRequest<decimal>;
    public record GetMonthlyPaymentsTrendQuery(int Months, string? BranchId = null, string? CompanyId = null) : IRequest<List<MonthlyTrendDto>>;
}
