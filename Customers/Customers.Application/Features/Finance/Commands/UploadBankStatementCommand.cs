using MediatR;
using Customers.Application.DTOs;
using System;
using System.Collections.Generic;

namespace Customers.Application.Features.Finance.Commands
{
    public record UploadBankStatementCommand(
        string FileName, 
        string BankName, 
        string BankAccountNumber, 
        List<BankStatementLineDto> Lines) : IRequest<Guid>;
}
