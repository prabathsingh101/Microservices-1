using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Commands;
using Customers.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class RecordContraEntryHandler : IRequestHandler<RecordContraEntryCommand, Guid>
    {
        private readonly IFinanceRepository _repository;

        public RecordContraEntryHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(RecordContraEntryCommand request, CancellationToken cancellationToken)
        {
            var contra = new ContraEntry
            {
                SourceType = request.ContraData.SourceType,
                SourceAccount = request.ContraData.SourceAccount,
                DestinationType = request.ContraData.DestinationType,
                DestinationAccount = request.ContraData.DestinationAccount,
                Amount = request.ContraData.Amount,
                ReferenceNumber = request.ContraData.ReferenceNumber,
                Remarks = request.ContraData.Remarks
            };

            await _repository.RecordContraEntryAsync(contra);
            return contra.Id;
        }
    }
}
