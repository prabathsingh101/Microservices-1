using MediatR;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.Features.Suppliers.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class DeleteSupplierHandler : IRequestHandler<DeleteSupplierCommand, bool>
    {
        private readonly ISupplierRepository _repository;

        public DeleteSupplierHandler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
        {
            var supplier = await _repository.GetByIdAsync(request.Id);
            if (supplier == null) return false;

            supplier.Deactivate();
            await _repository.UpdateAsync(supplier);
            return true;
        }
    }
}
