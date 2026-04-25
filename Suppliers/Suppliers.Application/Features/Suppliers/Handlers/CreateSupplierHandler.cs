using MediatR;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Suppliers.Application.Features.Suppliers.Commands;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class CreateSupplierHandler : IRequestHandler<CreateSupplierCommand, Guid>
    {
        private readonly ISupplierRepository _repository;

        public CreateSupplierHandler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
        {
            Guid? companyId = Guid.TryParse(request.SupplierData.companyId, out var cId) ? cId : null;

            var supplier = new Supplier(
                request.SupplierData.name,
                request.SupplierData.phone,
                request.SupplierData.gstIn,
                request.SupplierData.address,
                request.SupplierData.email,
                request.SupplierData.createdBy,
                request.SupplierData.isActive,
                companyId,
                request.SupplierData.branchId,
                request.SupplierData.defaultpricelistId
            );

            await _repository.AddAsync(supplier);
            return supplier.Id;
        }
    }
}
