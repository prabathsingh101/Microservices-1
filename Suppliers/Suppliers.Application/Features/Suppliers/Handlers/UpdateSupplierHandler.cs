using MediatR;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.Features.Suppliers.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class UpdateSupplierHandler : IRequestHandler<UpdateSupplierCommand, bool>
    {
        private readonly ISupplierRepository _repository;

        public UpdateSupplierHandler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
        {
            var supplier = await _repository.GetByIdAsync(request.Id);
            if (supplier == null) return false;

            Guid? companyId = Guid.TryParse(request.SupplierData.companyId, out var cId) ? cId : null;

            supplier.UpdateDetails(
                request.SupplierData.name,
                request.SupplierData.phone,
                request.SupplierData.gstIn,
                request.SupplierData.address,
                request.SupplierData.email,
                request.SupplierData.isActive,
                request.SupplierData.defaultpricelistId,
                companyId,
                request.SupplierData.branchId,
                request.SupplierData.modifiedBy
            );

            await _repository.UpdateAsync(supplier);
            return true;
        }
    }
}
