using MediatR;
using Microsoft.EntityFrameworkCore;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.Common.Models;
using Suppliers.Application.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Queries;

internal sealed class GetSuppliersPagedQueryHandler
    : IRequestHandler<GetSuppliersPagedQuery, GridResponse<SupplierDto>>
{
    private readonly ISupplierRepository _repository;

    public GetSuppliersPagedQueryHandler(ISupplierRepository repository)
    {
        _repository = repository;
    }

    public async Task<GridResponse<SupplierDto>> Handle(
           GetSuppliersPagedQuery request,
           CancellationToken cancellationToken)
    {
        var query = _repository
            .Query()
            .AsQueryable();

        // Global Search
        if (!string.IsNullOrWhiteSpace(request.Query.Search))
        {
            var search = $"%{request.Query.Search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Name, search) ||
                EF.Functions.Like(x.Phone, search) ||
                EF.Functions.Like(x.GstIn ?? "", search) ||
                EF.Functions.Like(x.Address ?? "", search)
            );
        }

        // Column Filters
        if (request.Query.Filters != null && request.Query.Filters.Any())
        {
            foreach (var filter in request.Query.Filters)
            {
                var value = filter.Value?.Trim();
                if (string.IsNullOrEmpty(value)) continue;

                var likeValue = $"%{value}%";
                query = filter.Key switch
                {
                    "name" => query.Where(x => EF.Functions.Like(x.Name, likeValue)),
                    "phone" => query.Where(x => EF.Functions.Like(x.Phone, likeValue)),
                    "gstIn" => query.Where(x => EF.Functions.Like(x.GstIn ?? "", likeValue)),
                    "isActive" => query.Where(x => x.IsActive == (value == "true" || value == "yes")),
                    _ => query
                };
            }
        }

        // Sorting
        query = request.Query.SortBy switch
        {
            "name" => request.Query.SortDirection == "asc" ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name),
            "phone" => request.Query.SortDirection == "asc" ? query.OrderBy(x => x.Phone) : query.OrderByDescending(x => x.Phone),
            "createdAt" => request.Query.SortDirection == "asc" ? query.OrderBy(x => x.CreatedOn) : query.OrderByDescending(x => x.CreatedOn),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Query.PageNumber - 1) * request.Query.PageSize)
            .Take(request.Query.PageSize)
            .Select(x => new SupplierDto(
                x.Id,
                x.Name,
                x.Phone,
                x.GstIn,
                x.Address,
                x.Email,
                x.IsActive,
                x.CreatedBy,
                x.DefaultPriceListId,
                x.CompanyId.HasValue ? x.CompanyId.ToString() : null,
                x.BranchId,
                x.DrugLicenseNo,
                x.SupplierType,
                x.FssaiLicenseNo,
                x.AgriLicenseNo,
                x.GstFilingFrequency,
                x.GstComplianceScore,
                x.GstFilingStatus,
                x.LastFiledMonth,
                x.LastFilingDate,
                x.BankAccountNumber,
                x.BankIfscCode,
                x.BankAccountName,
                x.BankName,
                x.BankBranchName,
                x.BankAddress
            ))
            .ToListAsync(cancellationToken);

        return new GridResponse<SupplierDto>(items, totalCount);
    }
}
