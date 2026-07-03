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

            try 
            {
                // Smart Idempotency: Only skip if this PAYMENT specifically exists in the database
                if (!string.IsNullOrWhiteSpace(paymentDto.ReferenceNumber))
                {
                    var alreadyExists = await _repository.ReferenceExistsAsync(paymentDto.ReferenceNumber);
                    if (alreadyExists)
                    {
                         Console.WriteLine($"[RecordSupplierPaymentHandler] INFO: Payment {paymentDto.ReferenceNumber} already exists. Skipping duplicate save.");
                         return Guid.Empty; 
                    }
                }

                var lastLedger = await _repository.GetLastLedgerEntryAsync(paymentDto.SupplierId);
                decimal roundedAmount = Math.Round(paymentDto.Amount, 2, MidpointRounding.AwayFromZero);
                
                bool isRefund = paymentDto.TransactionType == "Refund";
                
                decimal currentBalance;
                if (isRefund)
                {
                    // Refund received: Increase balance (Credit)
                    currentBalance = (lastLedger?.Balance ?? 0) + roundedAmount;
                }
                else
                {
                    // Payment sent: Decrease balance (Debit)
                    currentBalance = (lastLedger?.Balance ?? 0) - roundedAmount;
                }
                
                currentBalance = Math.Round(currentBalance, 2, MidpointRounding.AwayFromZero);

                var supplierPayment = new SupplierPayment
                {
                    SupplierId = paymentDto.SupplierId,
                    Amount = roundedAmount,
                    PaymentDate = paymentDto.PaymentDate,
                    PaymentMode = paymentDto.PaymentMode ?? "Other",
                    ReferenceNumber = paymentDto.ReferenceNumber,
                    Remarks = paymentDto.Remarks,
                    TransactionType = paymentDto.TransactionType ?? "Payment",
                    CreatedBy = paymentDto.CreatedBy,
                    CompanyId = paymentDto.CompanyId,
                    BranchId = paymentDto.BranchId,
                    BankName = paymentDto.BankName,
                    TransactionId = paymentDto.TransactionId,
                    ChequeNumber = paymentDto.ChequeNumber,
                    ChequeDate = paymentDto.ChequeDate
                };

                await _repository.AddPaymentAsync(supplierPayment);

                string descriptionSuffix = "";
                if (paymentDto.PaymentMode == "Bank" || paymentDto.PaymentMode == "Bank Transfer")
                {
                    descriptionSuffix = $" (Bank: {paymentDto.BankName}, Txn: {paymentDto.TransactionId})";
                }
                else if (paymentDto.PaymentMode == "Cheque")
                {
                    descriptionSuffix = $" (Bank: {paymentDto.BankName}, Chq No: {paymentDto.ChequeNumber}, Date: {paymentDto.ChequeDate?.ToString("dd-MM-yyyy")})";
                }

                var baseDescription = !string.IsNullOrEmpty(paymentDto.Remarks) ? paymentDto.Remarks : (isRefund ? $"Refund Received {paymentDto.ReferenceNumber ?? ""}" : $"Payment for {paymentDto.ReferenceNumber ?? "Invoice"}");
                var fullDescription = baseDescription;
                if (!string.IsNullOrEmpty(descriptionSuffix))
                {
                    fullDescription += descriptionSuffix;
                }

                var supplierLedger = new SupplierLedger
                {
                    SupplierId = paymentDto.SupplierId,
                    TransactionType = isRefund ? "Refund" : "Payment",
                    ReferenceId = !string.IsNullOrEmpty(paymentDto.ReferenceNumber) ? paymentDto.ReferenceNumber : (isRefund ? "REF-" : "PAY-") + Guid.NewGuid().ToString().Substring(0, 8),
                    Debit = isRefund ? 0 : roundedAmount,
                    Credit = isRefund ? roundedAmount : 0,
                    Balance = currentBalance,
                    TransactionDate = paymentDto.PaymentDate,
                    Description = fullDescription,
                    CompanyId = paymentDto.CompanyId,
                    BranchId = paymentDto.BranchId
                };

                await _repository.AddLedgerEntryAsync(supplierLedger);
                await _repository.SaveChangesAsync();

                return supplierPayment.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine("**************************************************");
                Console.WriteLine($"FATAL ERROR in RecordSupplierPaymentHandler: {ex.Message}");
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
