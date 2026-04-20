using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Commands;
using Customers.Application.DTOs;
using Customers.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class RecordCustomerReceiptHandler : IRequestHandler<RecordCustomerReceiptCommand, Guid>
    {
        private readonly IFinanceRepository _repository;
        private readonly ICurrentUserService _currentUserService;

        public RecordCustomerReceiptHandler(IFinanceRepository repository, ICurrentUserService currentUserService)
        {
            _repository = repository;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(RecordCustomerReceiptCommand request, CancellationToken cancellationToken)
        {
            var receiptDto = request.ReceiptData;

            if (!string.IsNullOrWhiteSpace(receiptDto.ReferenceNumber))
            {
                var (isUnique, existingSource) = await _repository.IsReferenceUniqueWithSourceAsync(receiptDto.ReferenceNumber);
                if (!isUnique)
                {
                    throw new InvalidOperationException($"Reference '{receiptDto.ReferenceNumber}' already exists in {existingSource}. Please refresh or use a different reference.");
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
                CompanyId = _currentUserService.CompanyId
            };

            await _repository.AddReceiptAsync(customerReceipt);

            var lastLedger = await _repository.GetLastLedgerEntryAsync(receiptDto.CustomerId);
            decimal currentBalance = (lastLedger?.Balance ?? 0) - receiptDto.Amount;

            var ledgerEntry = new CustomerLedger
            {
                CustomerId = receiptDto.CustomerId,
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
                CompanyId = _currentUserService.CompanyId
            };

            await _repository.AddLedgerEntryAsync(ledgerEntry);
            await _repository.SaveChangesAsync();

            return customerReceipt.Id; 
        }
    }
}
