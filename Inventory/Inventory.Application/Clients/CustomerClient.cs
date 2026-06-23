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

    public CustomerClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
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
        var client = _httpClientFactory.CreateClient("CustomerService");
        AddAuthorizationHeader(client);

        // Batch API call: Customer Microservice ko IDs bhejein
        var response = await client.PostAsJsonAsync("api/customers/get-names", customerIds);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Dictionary<Guid, string>>() ?? new();
        }

        return new Dictionary<Guid, string>();
    }

    public async Task<List<CustomerLookupDto>> GetCustomersForLookupAsync()
    {
        var client = _httpClientFactory.CreateClient("CustomerService");
        AddAuthorizationHeader(client);
        return await client.GetFromJsonAsync<List<CustomerLookupDto>>("api/customers/lookup") ?? new();
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
        var client = _httpClientFactory.CreateClient("CustomerService");
        AddAuthorizationHeader(client);

        var payload = new
        {
            CustomerId = customerId,
            Amount = amount,
            ReferenceId = referenceId,
            Description = description,
            TransactionDate = DateTime.Now,
            CreatedBy = createdBy,
            BranchId = branchId,
            CompanyId = companyId
        };

        var response = await client.PostAsJsonAsync("api/finance/sale", payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to record sale in customer ledger: {error}");
        }
    }

    public async Task RecordReceiptAsync(Guid? customerId, decimal amount, string paymentMode, string referenceNumber, string remarks, string createdBy, string? branchId, Guid? companyId)
    {
        var client = _httpClientFactory.CreateClient("CustomerService");
        AddAuthorizationHeader(client);

        var payload = new
        {
            CustomerId = customerId,
            Amount = amount,
            paymentDate = DateTime.Now,
            paymentMode = paymentMode,
            ReferenceNumber = referenceNumber,
            Remarks = remarks,
            CreatedBy = createdBy,
            BranchId = branchId,
            CompanyId = companyId
        };

        var response = await client.PostAsJsonAsync("api/finance/receipt", payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to record receipt in customer service: {error}");
        }
    }

    public async Task<CustomerLookupDto?> GetCustomerByIdAsync(Guid id)
    {
        var client = _httpClientFactory.CreateClient("CustomerService");
        AddAuthorizationHeader(client);

        var response = await client.GetAsync($"api/customers/{id}");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CustomerLookupDto>();
        }

        return null;
    }
}

