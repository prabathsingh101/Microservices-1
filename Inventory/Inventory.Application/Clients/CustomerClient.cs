using Inventory.Application.Clients;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Clients;

public class CustomerClient : ICustomerClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MassTransit.IPublishEndpoint _publishEndpoint;

    public CustomerClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, MassTransit.IPublishEndpoint publishEndpoint)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _publishEndpoint = publishEndpoint;
    }

    private void AddAuthorizationHeader(HttpClient client)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null && context.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomerClient] Failed to attach auth token: {ex.Message}");
        }
    }

    public async Task<Dictionary<Guid, string>> GetCustomerNamesAsync(List<Guid> customerIds)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CustomerService");
            AddAuthorizationHeader(client);

            // Batch API call: Customer Microservice ko IDs bhejein
            var response = await client.PostAsJsonAsync("api/customers/get-names", customerIds);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Dictionary<Guid, string>>() ?? new();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomerClient] Error fetching customer names: {ex.Message}");
        }

        return new Dictionary<Guid, string>();
    }

    public async Task<Dictionary<Guid, CustomerLookupDto>> GetCustomerDetailsByIdsAsync(List<Guid> customerIds)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CustomerService");
            AddAuthorizationHeader(client);

            // Batch API call: Customer Microservice ko IDs bhejein aur details payenge
            var response = await client.PostAsJsonAsync("api/customers/get-details", customerIds);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Dictionary<Guid, CustomerLookupDto>>() ?? new();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomerClient] Error fetching customer details: {ex.Message}");
        }

        return new Dictionary<Guid, CustomerLookupDto>();
    }

    public async Task<List<CustomerLookupDto>> GetCustomersForLookupAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CustomerService");
            AddAuthorizationHeader(client);
            return await client.GetFromJsonAsync<List<CustomerLookupDto>>("api/customers/lookup") ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomerClient] Error fetching customers lookup: {ex.Message}");
            return new List<CustomerLookupDto>();
        }
    }

    public async Task<List<Guid>> SearchCustomerIdsByNameAsync(string searchName)
    {
        if (string.IsNullOrWhiteSpace(searchName)) return new List<Guid>();

        var client = _httpClientFactory.CreateClient("CustomerService");
        AddAuthorizationHeader(client);

        try
        {
            var response = await client.GetAsync($"api/customers/search-ids?name={Uri.EscapeDataString(searchName)}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<Guid>>() ?? new List<Guid>();
            }
        }
        catch (Exception ex)
        {
            // Logging error for better debugging
            Console.WriteLine($"Error in SearchCustomerIdsByNameAsync: {ex.Message}");
        }

        return new List<Guid>();
    }

    public async Task RecordSaleAsync(Guid customerId, decimal amount, string referenceId, string description, string createdBy, string? branchId, Guid? companyId)
    {
        await _publishEndpoint.Publish<Shared.Contracts.CustomerSaleCreatedEvent>(new
        {
            CustomerId = customerId,
            Amount = amount,
            ReferenceId = referenceId,
            Description = description,
            TransactionDate = DateTime.Now,
            CreatedBy = createdBy,
            BranchId = branchId,
            CompanyId = companyId
        });
    }

    public async Task RecordReceiptAsync(Guid? customerId, decimal amount, string paymentMode, string referenceNumber, string remarks, string createdBy, string? branchId, Guid? companyId)
    {
        await _publishEndpoint.Publish<Shared.Contracts.CustomerReceiptCreatedEvent>(new
        {
            CustomerId = customerId,
            Amount = amount,
            PaymentDate = DateTime.Now,
            PaymentMode = paymentMode,
            ReferenceNumber = referenceNumber,
            Remarks = remarks,
            CreatedBy = createdBy,
            BranchId = branchId,
            CompanyId = companyId
        });
    }

    public async Task<CustomerLookupDto?> GetCustomerByIdAsync(Guid id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CustomerService");
            AddAuthorizationHeader(client);

            var response = await client.GetAsync($"api/customers/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CustomerLookupDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomerClient] Error fetching customer by ID: {ex.Message}");
        }

        return null;
    }
}

