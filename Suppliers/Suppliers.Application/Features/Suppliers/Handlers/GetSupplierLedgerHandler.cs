using MediatR;
using Suppliers.Application.DTOs;
using Suppliers.Application.Features.Suppliers.Queries;
using Suppliers.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetSupplierLedgerHandler(IFinanceRepository financeRepository) : IRequestHandler<GetSupplierLedgerQuery, SupplierLedgerPagedResultDto>
    {
        private readonly IFinanceRepository _financeRepository = financeRepository;

        public async Task<SupplierLedgerPagedResultDto> Handle(GetSupplierLedgerQuery request, CancellationToken cancellationToken)
        {
            return await _financeRepository.GetLedgerAsync(request.Request);
        }
    }
}
