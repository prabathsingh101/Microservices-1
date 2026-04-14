using MediatR;
using Suppliers.Application.DTOs;
using Suppliers.Application.Features.Suppliers.Commands;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class RecordSupplierPaymentHandler(IFinanceRepository repository) : IRequestHandler<RecordSupplierPaymentCommand, Guid>
    {
        private readonly IFinanceRepository _repository = repository;

        public async Task<Guid> Handle(RecordSupplierPaymentCommand request, CancellationToken cancellationToken)
        {
            var paymentDto = request.PaymentData;

            if (!string.IsNullOrWhiteSpace(paymentDto.ReferenceNumber))
            {
                var isUnique = await _repository.IsReferenceUniqueAsync(paymentDto.ReferenceNumber);
                if (!isUnique)
                {
                    throw new InvalidOperationException($"Duplicate Reference: Cheque/Ref No. {paymentDto.ReferenceNumber} already exists in the system.");
                }
            }

            var supplierPayment = new SupplierPayment
            {
                SupplierId = paymentDto.SupplierId,
                Amount = paymentDto.Amount,
                PaymentDate = paymentDto.PaymentDate,
                PaymentMode = paymentDto.PaymentMode ?? "Other",
                ReferenceNumber = paymentDto.ReferenceNumber,
                Remarks = paymentDto.Remarks,
                CreatedBy = paymentDto.CreatedBy
            };

            await _repository.AddPaymentAsync(supplierPayment);

            var lastLedger = await _repository.GetLastLedgerEntryAsync(paymentDto.SupplierId);
            decimal currentBalance = (lastLedger?.Balance ?? 0) - paymentDto.Amount;

            var supplierLedger = new SupplierLedger
            {
                SupplierId = paymentDto.SupplierId,
                TransactionType = "Payment",
                ReferenceId = !string.IsNullOrEmpty(paymentDto.ReferenceNumber) ? paymentDto.ReferenceNumber : "PAY-" + Guid.NewGuid().ToString().Substring(0, 8),
                Debit = paymentDto.Amount,
                Credit = 0,
                Balance = currentBalance,
                TransactionDate = paymentDto.PaymentDate,
                Description = !string.IsNullOrEmpty(paymentDto.Remarks) ? paymentDto.Remarks : $"Payment for {paymentDto.ReferenceNumber ?? "Invoice"}"
            };

            await _repository.AddLedgerEntryAsync(supplierLedger);
            await _repository.SaveChangesAsync();

            return supplierPayment.Id;
        }
    }
}
