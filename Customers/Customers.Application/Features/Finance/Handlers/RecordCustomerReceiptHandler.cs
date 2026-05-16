using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Commands;
using Customers.Application.DTOs;
using Customers.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
                if (!string.IsNullOrWhiteSpace(receiptDto.ReferenceNumber))
                {
                    var (isUnique, existingSource) = await _repository.IsReferenceUniqueWithSourceAsync(receiptDto.ReferenceNumber);
                    if (!isUnique)
                    {
                        throw new InvalidOperationException($"Reference '{receiptDto.ReferenceNumber}' already exists in {existingSource}. Please refresh or use a different reference.");
                    }
                }

                Guid? branchGuid = null;
                if (!string.IsNullOrEmpty(receiptDto.BranchId))
                {
                    if (Guid.TryParse(receiptDto.BranchId, out var parsedGuid))
                    {
                        branchGuid = parsedGuid;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid BranchId format: {BranchId}. Proceeding with null BranchId.", receiptDto.BranchId);
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
                    CompanyId = _currentUserService.CompanyId,
                    BranchId = branchGuid
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
                        CompanyId = _currentUserService.CompanyId,
                        BranchId = string.IsNullOrEmpty(receiptDto.BranchId) ? _currentUserService.BranchId : Guid.Parse(receiptDto.BranchId)
                    };

                    await _repository.AddLedgerEntryAsync(ledgerEntry);
                }
                
                await _repository.SaveChangesAsync();

                return customerReceipt.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording customer receipt for reference {ReferenceNumber}", receiptDto.ReferenceNumber);
                throw;
            }
        }
    }
}
