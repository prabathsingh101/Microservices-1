using MediatR;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.Features.Suppliers.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class DeleteSupplierPaymentHandler(IFinanceRepository repository) : IRequestHandler<DeleteSupplierPaymentCommand, bool>
    {
        private readonly IFinanceRepository _repository = repository;

        public async Task<bool> Handle(DeleteSupplierPaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _repository.DeletePaymentAsync(request.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine("**************************************************");
                Console.WriteLine($"FATAL ERROR in DeleteSupplierPaymentHandler: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine("**************************************************");
                throw;
            }
        }
    }
}
