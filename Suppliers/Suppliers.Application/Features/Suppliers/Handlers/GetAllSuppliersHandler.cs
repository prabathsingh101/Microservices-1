using MediatR;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.DTOs;
using Suppliers.Application.Features.Suppliers.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetAllSuppliersHandler : IRequestHandler<GetAllSuppliersQuery, IEnumerable<SupplierDto>>
    {
        private readonly ISupplierRepository _repository;

        public GetAllSuppliersHandler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SupplierDto>> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken)
        {
            var suppliers = await _repository.GetAllAsync();
            return suppliers.Select(s => new SupplierDto(
                s.Id,
                s.Name,
                s.Phone,
                s.GstIn,
                s.Address,
                s.Email,
                s.IsActive,
                s.CreatedBy,
                s.DefaultPriceListId,
                s.CompanyId?.ToString(),
                s.BranchId,
                s.DrugLicenseNo,
                s.SupplierType,
                s.FssaiLicenseNo,
                s.AgriLicenseNo
            ));
        }
    }
}
