using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Commands;
using Customers.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Customers.Application.Features.Finance.Handlers
{
    public class BulkRecordCustomerReceiptHandler : IRequestHandler<BulkRecordCustomerReceiptCommand, bool>
    {
        private readonly IFinanceRepository _repository;
        private readonly ICurrentUserService _currentUserService;

        public BulkRecordCustomerReceiptHandler(IFinanceRepository repository, ICurrentUserService currentUserService)
        {
            _repository = repository;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(BulkRecordCustomerReceiptCommand request, CancellationToken cancellationToken)
        {
            if (request.Receipts == null || !request.Receipts.Any())
                return false;

            var receiptsByCustomer = request.Receipts.GroupBy(r => r.CustomerId);

            foreach (var customerGroup in receiptsByCustomer)
            {
                Guid customerId = customerGroup.Key;
                
                var lastLedger = await _repository.GetLastLedgerEntryAsync(customerId);
                decimal currentBalance = lastLedger?.Balance ?? 0;

                foreach (var receiptDto in customerGroup)
                {
                    var customerReceipt = new CustomerReceipt
                    {
                        CustomerId = customerId,
                        Amount = receiptDto.Amount,
                        ReceiptDate = receiptDto.ReceiptDate,
                        ReceiptMode = receiptDto.ReceiptMode ?? "Other",
                        ReferenceNumber = receiptDto.ReferenceNumber,
                        Remarks = receiptDto.Remarks,
                        CreatedBy = receiptDto.CreatedBy ?? "System",
                        CompanyId = _currentUserService.CompanyId
                    };

                    await _repository.AddReceiptAsync(customerReceipt);

                    currentBalance -= receiptDto.Amount;

                    var ledgerEntry = new CustomerLedger
                    {
                        CustomerId = customerId,
                        TransactionType = "Receipt",
                        ReferenceId = string.IsNullOrWhiteSpace(receiptDto.ReferenceNumber) 
                            ? "REC-" + Guid.NewGuid().ToString().Substring(0, 8) 
                            : receiptDto.ReferenceNumber,
                        Debit = 0,
                        Credit = receiptDto.Amount,
                        Balance = currentBalance,
                        TransactionDate = receiptDto.ReceiptDate,
                        Description = "Receipt Received: " + receiptDto.ReceiptMode,
                        CreatedBy = receiptDto.CreatedBy ?? "System",
                        CompanyId = _currentUserService.CompanyId
                    };

                    await _repository.AddLedgerEntryAsync(ledgerEntry);
                }
            }

            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
