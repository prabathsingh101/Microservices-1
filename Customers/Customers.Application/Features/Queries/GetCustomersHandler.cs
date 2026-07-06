using Customers.Application.Common.Interfaces;
using Customers.Application.DTOs;
using Customers.Application.Features.Queries;
using MediatR;

namespace Customers.Application.Features.Handlers;

public class GetCustomersHandler
    : IRequestHandler<GetCustomersQuery, List<CustomerDto>>
{
    private readonly ICustomerRepository _repo;

    public GetCustomersHandler(ICustomerRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<CustomerDto>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var customers = await _repo.GetAllAsync();

        return customers.Select(x => new CustomerDto
        {
            Id = x.Id,
            CustomerName = x.CustomerName,
            CustomerType = x.CustomerType,
            Phone = x.Phone,
            Email = x.Email,
            GstNumber = x.GstNumber,
            CreditLimit = x.CreditLimit,
            BillingAddressLine = x.BillingAddress != null ? x.BillingAddress.AddressLine : null,
            ShippingAddressLine = x.ShippingAddress != null ? x.ShippingAddress.AddressLine : null,
            Status = x.Status,
            DrugLicenseNo = x.DrugLicenseNo,
            LicenseType = x.LicenseType,
            LicenseNo = x.LicenseNo,
            Latitude = x.Latitude,
            Longitude = x.Longitude
        }).ToList();
    }
}
