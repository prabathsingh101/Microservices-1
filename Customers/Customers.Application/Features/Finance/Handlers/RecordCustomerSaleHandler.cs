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

        public RecordCustomerSaleHandler(IFinanceRepository financeRepository)
        {
            _financeRepository = financeRepository;
        }

        public async Task<Guid> Handle(RecordCustomerSaleCommand request, CancellationToken cancellationToken)
        {
            var sale = request.SaleDto;

            var lastEntry = await _financeRepository.GetLastLedgerEntryAsync(sale.CustomerId);
            decimal currentBalance = lastEntry?.Balance ?? 0;
            decimal newBalance = currentBalance + sale.Amount;

            var entry = new CustomerLedger
            {
                CustomerId = sale.CustomerId,
                TransactionDate = sale.TransactionDate,
                TransactionType = "Sale",
                ReferenceId = sale.ReferenceId ?? string.Empty,
                Description = sale.Description,
                Debit = sale.Amount,
                Credit = 0,
                Balance = newBalance,
                CreatedBy = sale.CreatedBy
            };

            await _financeRepository.AddLedgerEntryAsync(entry);
            await _financeRepository.SaveChangesAsync();

            return entry.Id;
        }
    }
}
