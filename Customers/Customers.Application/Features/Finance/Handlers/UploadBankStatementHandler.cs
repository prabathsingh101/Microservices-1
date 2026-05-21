using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Commands;
using Customers.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class UploadBankStatementHandler : IRequestHandler<UploadBankStatementCommand, Guid>
    {
        private readonly IFinanceRepository _repository;

        public UploadBankStatementHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(UploadBankStatementCommand request, CancellationToken cancellationToken)
        {
            var statement = new BankStatement
            {
                FileName = request.FileName,
                BankName = request.BankName,
                BankAccountNumber = request.BankAccountNumber,
                TotalAmount = request.Lines.Sum(l => l.Deposit - l.Withdrawal)
            };

            var lines = request.Lines.Select(l => new BankStatementLine
            {
                TransactionDate = l.TransactionDate,
                Description = l.Description,
                ReferenceNumber = l.ReferenceNumber,
                Withdrawal = l.Withdrawal,
                Deposit = l.Deposit,
                ReconciliationStatus = "Unmatched"
            }).ToList();

            await _repository.UploadBankStatementAsync(statement, lines);
            return statement.Id;
        }
    }
}
