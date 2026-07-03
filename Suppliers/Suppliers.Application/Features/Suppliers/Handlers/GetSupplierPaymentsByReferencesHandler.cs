using MediatR;
using Suppliers.Application.DTOs;
using Suppliers.Application.Features.Suppliers.Queries;
using Suppliers.Application.Common.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetSupplierPaymentsByReferencesHandler(IFinanceRepository repository) : IRequestHandler<GetSupplierPaymentsByReferencesQuery, List<SupplierPaymentDto>>
    {
        private readonly IFinanceRepository _repository = repository;

        public async Task<List<SupplierPaymentDto>> Handle(GetSupplierPaymentsByReferencesQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetPaymentsByReferencesAsync(request.ReferenceNumbers);
        }
    }
}
