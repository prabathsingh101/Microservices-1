using MediatR;
using Suppliers.Application.Features.Suppliers.Queries;
using System;
using System.Collections.Generic;
using System.Text;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetSupplierByIdHandler : IRequestHandler<GetSupplierByIdQuery, SupplierDto?>
    {
        private readonly ISupplierRepository _repository;

        public GetSupplierByIdHandler(ISupplierRepository repository)
        {
            _repository = repository; // Fix for CS8618
        }

        public async Task<SupplierDto?> Handle(GetSupplierByIdQuery request, CancellationToken ct)
        {
            var supplier = await _repository.GetByIdAsync(request.Id);

            if (supplier == null) return null;

            // Manual mapping to SupplierDto
            return new SupplierDto(
                supplier.Id,
                supplier.Name,
                supplier.Phone,
                supplier.GstIn,
                supplier.Address,
                supplier.Email,
                supplier.IsActive,
                supplier.CreatedBy,
                supplier.DefaultPriceListId // Ye value ab frontend console mein dikhegi
            );
        }
    }
}
