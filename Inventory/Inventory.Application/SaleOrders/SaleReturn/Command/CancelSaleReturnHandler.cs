using Inventory.Application.Common.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.SaleOrders.SaleReturn.Command
{
    public class CancelSaleReturnHandler : IRequestHandler<CancelSaleReturnCommand, bool>
    {
        private readonly ISaleReturnRepository _repo;

        public CancelSaleReturnHandler(ISaleReturnRepository repo)
        {
            _repo = repo;
        }

        public async Task<bool> Handle(CancelSaleReturnCommand request, CancellationToken cancellationToken)
        {
            return await _repo.CancelSaleReturnAsync(request.Id, request.Reason);
        }
    }
}
