using MediatR;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.DTOs;
using Suppliers.Application.Features.Suppliers.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetSupplierByIdHandler : IRequestHandler<GetSupplierByIdQuery, SupplierDto?>
    {
        private readonly ISupplierRepository _repository;

        public GetSupplierByIdHandler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<SupplierDto?> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
        {
            var s = await _repository.GetByIdAsync(request.Id);
            if (s == null) return null;

            return new SupplierDto(
                s.Id,
                s.Name,
                s.Phone,
                s.GstIn,
                s.Address,
                s.Email,
                s.IsActive,
                s.CreatedBy,
                s.DefaultPriceListId,
                s.CompanyId
            );
        }
    }
}
