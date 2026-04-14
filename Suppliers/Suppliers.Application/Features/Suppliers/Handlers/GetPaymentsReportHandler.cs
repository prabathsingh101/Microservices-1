using MediatR;
using Suppliers.Application.DTOs;
using Suppliers.Application.Features.Suppliers.Queries;
using Suppliers.Application.Common.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetPaymentsReportHandler(IFinanceRepository repository) : IRequestHandler<GetPaymentsReportQuery, PaginatedListDto<PaymentReportDto>>
    {
        private readonly IFinanceRepository _repository = repository;

        public async Task<PaginatedListDto<PaymentReportDto>> Handle(GetPaymentsReportQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetPaymentsReportAsync(request.Request);
        }
    }
}
