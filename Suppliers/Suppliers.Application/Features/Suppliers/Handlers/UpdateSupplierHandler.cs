using MediatR;
using Microsoft.EntityFrameworkCore;
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

            // 🚨 DUPLICATE CHECKS within same Company/Tenant (excluding this supplier)
            // 1. Phone number check
            if (!string.IsNullOrWhiteSpace(request.SupplierData.phone))
            {
                bool isPhoneDuplicate = await _repository.Query()
                    .AnyAsync(s => s.Id != request.Id && s.CompanyId == companyId && s.Phone == request.SupplierData.phone, cancellationToken);
                if (isPhoneDuplicate)
                    throw new InvalidOperationException("A supplier with this phone number already exists.");
            }

            // 2. GST Number check
            if (!string.IsNullOrWhiteSpace(request.SupplierData.gstIn))
            {
                bool isGstDuplicate = await _repository.Query()
                    .AnyAsync(s => s.Id != request.Id && s.CompanyId == companyId && s.GstIn == request.SupplierData.gstIn, cancellationToken);
                if (isGstDuplicate)
                    throw new InvalidOperationException("A supplier with this GST number already exists.");
            }

            // 3. Email check
            if (!string.IsNullOrWhiteSpace(request.SupplierData.email))
            {
                bool isEmailDuplicate = await _repository.Query()
                    .AnyAsync(s => s.Id != request.Id && s.CompanyId == companyId && s.Email == request.SupplierData.email, cancellationToken);
                if (isEmailDuplicate)
                    throw new InvalidOperationException("A supplier with this email address already exists.");
            }

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
