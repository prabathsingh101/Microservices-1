using MediatR;
using Microsoft.EntityFrameworkCore;
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

            // 🚨 DUPLICATE CHECKS within same Company/Tenant
            // 1. Phone number check (Required field)
            if (!string.IsNullOrWhiteSpace(request.SupplierData.phone))
            {
                bool isPhoneDuplicate = await _repository.Query()
                    .AnyAsync(s => s.CompanyId == companyId && s.Phone == request.SupplierData.phone, cancellationToken);
                if (isPhoneDuplicate)
                    throw new InvalidOperationException("A supplier with this phone number already exists.");
            }

            // 2. GST Number check (Optional field)
            if (!string.IsNullOrWhiteSpace(request.SupplierData.gstIn))
            {
                bool isGstDuplicate = await _repository.Query()
                    .AnyAsync(s => s.CompanyId == companyId && s.GstIn == request.SupplierData.gstIn, cancellationToken);
                if (isGstDuplicate)
                    throw new InvalidOperationException("A supplier with this GST number already exists.");
            }

            // 3. Email check (Optional field)
            if (!string.IsNullOrWhiteSpace(request.SupplierData.email))
            {
                bool isEmailDuplicate = await _repository.Query()
                    .AnyAsync(s => s.CompanyId == companyId && s.Email == request.SupplierData.email, cancellationToken);
                if (isEmailDuplicate)
                    throw new InvalidOperationException("A supplier with this email address already exists.");
            }

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
