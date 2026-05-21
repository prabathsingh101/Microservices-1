using MediatR;
using Customers.Application.DTOs;
using System.Collections.Generic;

namespace Customers.Application.Features.Finance.Queries
{
    public record GetCustomerLedgerQuery(CustomerLedgerRequestDto Request) : IRequest<CustomerLedgerPagedResultDto>;
    public record GetOutstandingQuery(OutstandingRequestDto Request) : IRequest<OutstandingPagedResultDto>;
    public record GetTotalReceiptsQuery(DateRangeDto DateRange) : IRequest<decimal>;
    public record GetTotalAdjustmentsQuery(DateRangeDto DateRange) : IRequest<AdjustmentsSummaryDto>;
    public record GetTotalOutstandingQuery(string? BranchId = null, string? CompanyId = null) : IRequest<decimal>;
    public record GetPendingDuesQuery(string? BranchId = null, string? CompanyId = null) : IRequest<List<OutstandingDto>>;
    public record GetMonthlyReceiptsTrendQuery(int Months, string? BranchId = null, string? CompanyId = null) : IRequest<List<MonthlyTrendDto>>;
    public record GetReceiptsReportQuery(ReceiptReportRequestDto Request) : IRequest<PaginatedListDto<ReceiptReportDto>>;
}
