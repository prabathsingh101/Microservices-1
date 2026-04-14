using MediatR;
using Suppliers.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetSupplierBalancesHandler : IRequestHandler<Queries.GetSupplierBalancesQuery, Dictionary<Guid, decimal>>
    {
        private readonly IFinanceRepository _repository;

        public GetSupplierBalancesHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<Dictionary<Guid, decimal>> Handle(Queries.GetSupplierBalancesQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetSupplierBalancesAsync(request.SupplierIds);
        }
    }
}
