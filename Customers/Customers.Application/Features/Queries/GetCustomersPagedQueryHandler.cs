using Customers.Application.Common.Interfaces;
using Customers.Application.Common.Models;
using Customers.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Queries;

internal sealed class GetCustomersPagedQueryHandler
    : IRequestHandler<GetCustomersPagedQuery, GridResponse<CustomerDto>>
{
    private readonly ICustomerRepository _repository;

    public GetCustomersPagedQueryHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<GridResponse<CustomerDto>> Handle(
           GetCustomersPagedQuery request,
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
                EF.Functions.Like(x.CustomerName ?? "", search) ||
                EF.Functions.Like(x.Phone ?? "", search) ||
                EF.Functions.Like(x.Email ?? "", search) ||
                EF.Functions.Like(x.GstNumber ?? "", search)
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
                    "customerName" => query.Where(x => EF.Functions.Like(x.CustomerName ?? "", likeValue)),
                    "phone" => query.Where(x => EF.Functions.Like(x.Phone ?? "", likeValue)),
                    "customerType" => query.Where(x => EF.Functions.Like(x.CustomerType ?? "", likeValue)),
                    "status" => query.Where(x => EF.Functions.Like(x.Status ?? "", likeValue)),
                    _ => query
                };
            }
        }

        // Sorting
        query = request.Query.SortBy switch
        {
            "customerName" => request.Query.SortDirection == "asc" ? query.OrderBy(x => x.CustomerName) : query.OrderByDescending(x => x.CustomerName),
            "phone" => request.Query.SortDirection == "asc" ? query.OrderBy(x => x.Phone) : query.OrderByDescending(x => x.Phone),
            "createdAt" => request.Query.SortDirection == "asc" ? query.OrderBy(x => x.CreatedOn) : query.OrderByDescending(x => x.CreatedOn),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Query.PageNumber - 1) * request.Query.PageSize)
            .Take(request.Query.PageSize)
            .Select(x => new CustomerDto
            {
                Id = x.Id,
                CustomerName = x.CustomerName,
                CustomerType = x.CustomerType,
                Phone = x.Phone,
                Email = x.Email,
                GstNumber = x.GstNumber,
                CreditLimit = x.CreditLimit,
                Status = x.Status,
                BillingAddressLine = x.BillingAddress != null ? x.BillingAddress.AddressLine : null,
                ShippingAddressLine = x.ShippingAddress != null ? x.ShippingAddress.AddressLine : null,
                DrugLicenseNo = x.DrugLicenseNo
            })
            .ToListAsync(cancellationToken);
              
        return new GridResponse<CustomerDto>(items, totalCount);
    }
}
