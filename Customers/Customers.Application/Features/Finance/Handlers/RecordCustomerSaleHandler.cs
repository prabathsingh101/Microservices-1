using MediatR;
using Customers.Application.DTOs;
using Customers.Application.Common.Interfaces;
using Customers.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using System;
using Customers.Application.Features.Finance.Commands;

namespace Customers.Application.Features.Finance.Handlers
{
    public class RecordCustomerSaleHandler : IRequestHandler<RecordCustomerSaleCommand, Guid>
    {
        private readonly IFinanceRepository _financeRepository;
        private readonly ICurrentUserService _currentUserService;

        public RecordCustomerSaleHandler(IFinanceRepository financeRepository, ICurrentUserService currentUserService)
        {
            _financeRepository = financeRepository;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(RecordCustomerSaleCommand request, CancellationToken cancellationToken)
        {
            var sale = request.SaleDto;

            if (sale.CustomerId == null || sale.CustomerId == Guid.Empty)
            {
                return Guid.Empty; // Placeholder for walking customers
            }

            var lastEntry = await _financeRepository.GetLastLedgerEntryAsync(sale.CustomerId.Value);
            decimal currentBalance = lastEntry?.Balance ?? 0;
            decimal newBalance = currentBalance + sale.Amount;

            var entry = new CustomerLedger
            {
                CustomerId = sale.CustomerId.Value,
                TransactionDate = sale.TransactionDate,
                TransactionType = "Sale",
                ReferenceId = sale.ReferenceId ?? string.Empty,
                Description = sale.Description,
                Debit = sale.Amount,
                Credit = 0,
                Balance = newBalance,
                CreatedBy = sale.CreatedBy,
                CompanyId = _currentUserService.CompanyId
            };

            await _financeRepository.AddLedgerEntryAsync(entry);
            await _financeRepository.SaveChangesAsync();

            return entry.Id;
        }
    }
}
