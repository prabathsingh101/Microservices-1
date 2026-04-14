using MediatR;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.Features.Suppliers.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetGRNPaymentStatusesHandler(IFinanceRepository repository) : IRequestHandler<GetGRNPaymentStatusesQuery, Dictionary<string, decimal>>
    {
        private readonly IFinanceRepository _repository = repository;

        public async Task<Dictionary<string, decimal>> Handle(GetGRNPaymentStatusesQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetGRNPaymentStatusesAsync(request.GrnNumbers);
        }
    }
}
