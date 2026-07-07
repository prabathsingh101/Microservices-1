using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Commands;
using Customers.Application.DTOs;
using Customers.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Customers.Application.Common;

namespace Customers.Application.Features.Finance.Handlers
{
    public class RecordCustomerReceiptHandler : IRequestHandler<RecordCustomerReceiptCommand, Guid>
    {
        private readonly IFinanceRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RecordCustomerReceiptHandler> _logger;

        public RecordCustomerReceiptHandler(IFinanceRepository repository, ICurrentUserService currentUserService, ILogger<RecordCustomerReceiptHandler> logger)
        {
            _repository = repository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Guid> Handle(RecordCustomerReceiptCommand request, CancellationToken cancellationToken)
        {
            var receiptDto = request.ReceiptData;

            try
            {
                await CustomerLedgerLock.Semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (!string.IsNullOrWhiteSpace(receiptDto.ReferenceNumber))
                    {
                        var (isUnique, existingSource) = await _repository.IsReferenceUniqueWithSourceAsync(receiptDto.ReferenceNumber);
                        if (!isUnique)
                        {
                            throw new InvalidOperationException($"Reference '{receiptDto.ReferenceNumber}' already exists in {existingSource}. Please refresh or use a different reference.");
                        }
                    }

                    if ((receiptDto.ReceiptMode == "Cheque" || receiptDto.ReceiptMode == "Check") && !string.IsNullOrWhiteSpace(receiptDto.ChequeNumber))
                    {
                        var isDuplicate = await _repository.ChequeNumberExistsAsync(receiptDto.ChequeNumber, receiptDto.BankName ?? "");
                        if (isDuplicate)
                        {
                            throw new InvalidOperationException($"Cheque number '{receiptDto.ChequeNumber}' has already been recorded for bank '{receiptDto.BankName}'.");
                        }
                    }

                    var customerReceipt = new CustomerReceipt
                    {
                        CustomerId = receiptDto.CustomerId,
                        Amount = receiptDto.Amount,
                        ReceiptDate = receiptDto.ReceiptDate,
                        ReceiptMode = receiptDto.ReceiptMode ?? "Other",
                        ReferenceNumber = receiptDto.ReferenceNumber,
                        Remarks = receiptDto.Remarks,
                        CreatedBy = receiptDto.CreatedBy,
                        CompanyId = (receiptDto.CompanyId != null && receiptDto.CompanyId != Guid.Empty) ? receiptDto.CompanyId : _currentUserService.CompanyId,
                        BranchId = receiptDto.BranchId,
                        ChequeNumber = receiptDto.ChequeNumber,
                        ChequeDate = receiptDto.ChequeDate,
                        BankName = receiptDto.BankName,
                        BankBranch = receiptDto.BankBranch,
                        BankAddress = receiptDto.BankAddress
                    };

                    await _repository.AddReceiptAsync(customerReceipt);

                    // Skip Ledger Entry for Walking Customers (Guest Payments)
                    if (receiptDto.CustomerId.HasValue && receiptDto.CustomerId.Value != Guid.Empty)
                    {
                        var lastLedger = await _repository.GetLastLedgerEntryAsync(receiptDto.CustomerId.Value);
                        decimal currentBalance = (lastLedger?.Balance ?? 0) - receiptDto.Amount;

                        var ledgerEntry = new CustomerLedger
                        {
                            CustomerId = receiptDto.CustomerId.Value,
                            TransactionType = "Receipt",
                            ReferenceId = string.IsNullOrWhiteSpace(receiptDto.ReferenceNumber)
                                ? "REC-" + Guid.NewGuid().ToString().Substring(0, 8)
                                : receiptDto.ReferenceNumber,
                            Debit = 0,
                            Credit = receiptDto.Amount,
                            Balance = currentBalance,
                            TransactionDate = receiptDto.ReceiptDate,
                            Description = "Receipt Received: " + receiptDto.ReceiptMode,
                            CreatedBy = receiptDto.CreatedBy,
                            CompanyId = (receiptDto.CompanyId != null && receiptDto.CompanyId != Guid.Empty) ? receiptDto.CompanyId : _currentUserService.CompanyId,
                            BranchId = string.IsNullOrEmpty(receiptDto.BranchId) ? _currentUserService.BranchId : receiptDto.BranchId
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
                _logger.LogError(ex, "Error recording customer receipt for reference {ReferenceNumber}", receiptDto.ReferenceNumber);
                throw;
            }
        }
    }
}
