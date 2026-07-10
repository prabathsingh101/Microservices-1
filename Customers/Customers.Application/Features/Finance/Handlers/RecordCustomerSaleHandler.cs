using MediatR;
using Customers.Application.DTOs;
using Customers.Application.Common.Interfaces;
using Customers.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using System;
using Customers.Application.Features.Finance.Commands;
using Customers.Application.Common;

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

            if (!string.IsNullOrWhiteSpace(sale.ReferenceId))
            {
                bool isDuplicate = await _financeRepository.HasRefundOrAdjustmentAgainstReferenceAsync(sale.CustomerId.Value, sale.ReferenceId);
                if (isDuplicate)
                {
                    throw new InvalidOperationException($"An adjustment or refund has already been processed against the reference number: {sale.ReferenceId}");
                }
            }

            await CustomerLedgerLock.Semaphore.WaitAsync(cancellationToken);
            try
            {
                var lastEntry = await _financeRepository.GetLastLedgerEntryAsync(sale.CustomerId.Value, sale.CompanyId);
                decimal currentBalance = lastEntry?.Balance ?? 0;
                decimal newBalance = currentBalance + sale.Amount;

                var entry = new CustomerLedger
                {
                    CustomerId = sale.CustomerId.Value,
                    TransactionDate = sale.TransactionDate,
                    TransactionType = "Sale",
                    ReferenceId = sale.ReferenceId ?? string.Empty,
                    Description = sale.Description,
                    Debit = sale.Amount >= 0 ? sale.Amount : 0,
                    Credit = sale.Amount < 0 ? Math.Abs(sale.Amount) : 0,
                    Balance = newBalance,
                    CreatedBy = sale.CreatedBy,
                    CompanyId = (sale.CompanyId != null && sale.CompanyId != Guid.Empty) ? sale.CompanyId : _currentUserService.CompanyId,
                    BranchId = string.IsNullOrEmpty(sale.BranchId) ? _currentUserService.BranchId : sale.BranchId
                };

                await _financeRepository.AddLedgerEntryAsync(entry);
                await _financeRepository.SaveChangesAsync();

                return entry.Id;
            }
            finally
            {
                CustomerLedgerLock.Semaphore.Release();
            }
        }
    }
}
