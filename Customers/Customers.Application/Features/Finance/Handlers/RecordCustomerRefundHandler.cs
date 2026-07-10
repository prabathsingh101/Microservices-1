using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Commands;
using Customers.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Customers.Application.Common;

namespace Customers.Application.Features.Finance.Handlers
{
    public class RecordCustomerRefundHandler : IRequestHandler<RecordCustomerRefundCommand, Guid>
    {
        private readonly IFinanceRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RecordCustomerRefundHandler> _logger;

        public RecordCustomerRefundHandler(IFinanceRepository repository, ICurrentUserService currentUserService, ILogger<RecordCustomerRefundHandler> logger)
        {
            _repository = repository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Guid> Handle(RecordCustomerRefundCommand request, CancellationToken cancellationToken)
        {
            var refundDto = request.RefundData;

            try
            {
                await CustomerLedgerLock.Semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (refundDto.CustomerId.HasValue && refundDto.CustomerId.Value != Guid.Empty && !string.IsNullOrWhiteSpace(refundDto.ReferenceNumber))
                    {
                        bool isDuplicate = await _repository.HasRefundOrAdjustmentAgainstReferenceAsync(refundDto.CustomerId.Value, refundDto.ReferenceNumber);
                        if (isDuplicate)
                        {
                            throw new InvalidOperationException($"A refund or adjustment has already been processed against the reference number: {refundDto.ReferenceNumber}");
                        }
                    }

                    // We create a CustomerReceipt with negative amount, or just positive amount but mark it as Refund.
                    // Let's use CustomerReceipt to store the transaction, but maybe we should just store the Ledger Entry.
                    // We will create a CustomerReceipt with negative amount so it reduces TotalReceipts in reports if needed,
                    // Or maybe we just create a positive CustomerReceipt and mark Remarks as "Refund".
                    // Wait, if we use positive Amount, P&L might count it as income. So we use negative Amount for Receipt.
                    var customerReceipt = new CustomerReceipt
                    {
                        CustomerId = refundDto.CustomerId,
                        Amount = -refundDto.Amount, // Negative to offset receipts
                        ReceiptDate = refundDto.RefundDate,
                        ReceiptMode = refundDto.RefundMode ?? "Other",
                        ReferenceNumber = refundDto.ReferenceNumber,
                        Remarks = "REFUND: " + refundDto.Remarks,
                        CreatedBy = refundDto.CreatedBy,
                        CompanyId = (refundDto.CompanyId != null && refundDto.CompanyId != Guid.Empty) ? refundDto.CompanyId : _currentUserService.CompanyId,
                        BranchId = refundDto.BranchId
                    };

                    await _repository.AddReceiptAsync(customerReceipt);

                    if (refundDto.CustomerId.HasValue && refundDto.CustomerId.Value != Guid.Empty)
                    {
                        var lastLedger = await _repository.GetLastLedgerEntryAsync(refundDto.CustomerId.Value, refundDto.CompanyId);
                        
                        // Customer owes us -> Balance > 0.
                        // Customer pays us (Receipt) -> Balance decreases (Credit).
                        // We pay Customer (Refund) -> Balance increases (Debit).
                        decimal currentBalance = (lastLedger?.Balance ?? 0) + refundDto.Amount;

                        var ledgerEntry = new CustomerLedger
                        {
                            CustomerId = refundDto.CustomerId.Value,
                            TransactionType = "Refund",
                            ReferenceId = string.IsNullOrWhiteSpace(refundDto.ReferenceNumber)
                                ? "REF-" + Guid.NewGuid().ToString().Substring(0, 8)
                                : refundDto.ReferenceNumber,
                            Debit = refundDto.Amount,
                            Credit = 0,
                            Balance = currentBalance,
                            TransactionDate = refundDto.RefundDate,
                            Description = "Refund Paid: " + refundDto.RefundMode,
                            CreatedBy = refundDto.CreatedBy,
                            CompanyId = (refundDto.CompanyId != null && refundDto.CompanyId != Guid.Empty) ? refundDto.CompanyId : _currentUserService.CompanyId,
                            BranchId = string.IsNullOrEmpty(refundDto.BranchId) ? _currentUserService.BranchId : refundDto.BranchId
                        };

                        await _repository.AddLedgerEntryAsync(ledgerEntry);
                    }
                    
                    await _repository.SaveChangesAsync();

                    return customerReceipt.Id;
                }
                finally
                {
                    CustomerLedgerLock.Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording customer refund for reference {ReferenceNumber}", refundDto.ReferenceNumber);
                throw;
            }
        }
    }
}
