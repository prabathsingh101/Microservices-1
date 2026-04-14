using Inventory.Application.Clients;
using Inventory.Application.PurchaseReturn;
using Microsoft.AspNetCore.Http; // Added
using System;
using System.Collections.Generic;
using System.Linq; // Added for First/Last
using System.Net.Http;
using System.Net.Http.Headers; // Added
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Clients
{
    public class SupplierClient : ISupplierClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SupplierClient(IHttpClientFactory factory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = factory;
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
                Console.WriteLine($"[SupplierClient] Failed to attach auth token: {ex.Message}");
            }
        }

        public async Task<List<SupplierSelectDto>> GetSuppliersByIdsAsync(List<Guid> supplierIds)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("SupplierServiceClient");
                AddAuthorizationHeader(client);
                var response = await client.PostAsJsonAsync("api/Supplier/get-by-ids", supplierIds);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<SupplierSelectDto>>();
                    return result ?? new List<SupplierSelectDto>();
                }
                return new List<SupplierSelectDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierClient] Error fetching suppliers: {ex.Message}");
                return new List<SupplierSelectDto>();
            }
        }

        public async Task<bool> RecordPurchaseAsync(Guid supplierId, decimal amount, string referenceId, string description, string createdBy)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("SupplierServiceClient");
                AddAuthorizationHeader(client);
                var payload = new 
                {
                    SupplierId = supplierId,
                    Amount = amount,
                    ReferenceId = referenceId, 
                    Description = description,
                    TransactionDate = DateTime.Now,
                    CreatedBy = createdBy
                };

                var response = await client.PostAsJsonAsync("api/finance/purchase-entry", payload);
                if (!response.IsSuccessStatusCode)
                {
                     var content = await response.Content.ReadAsStringAsync();
                     throw new Exception($"Supplier Service Purchase Failed: {response.StatusCode} - {content}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierClient] Error recording purchase: {ex.Message}");
                return false;
            }
        }

        public async Task<Dictionary<string, decimal>> GetGRNPaymentStatusesAsync(List<string> grnNumbers)
        {
            if (grnNumbers == null || grnNumbers.Count == 0) 
                return new Dictionary<string, decimal>();

            var client = _httpClientFactory.CreateClient("SupplierServiceClient");
            AddAuthorizationHeader(client);
            
            var response = await client.PostAsJsonAsync("api/finance/get-grn-statuses", grnNumbers);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, decimal>>();
                return result ?? new Dictionary<string, decimal>();
            }
            
            throw new HttpRequestException($"Supplier Service Request Failed: {response.StatusCode}");
        }

        public async Task<Dictionary<Guid, decimal>> GetSupplierBalancesAsync(List<Guid> supplierIds)
        {
            if (supplierIds == null || !supplierIds.Any()) return new Dictionary<Guid, decimal>();

            try
            {
                var client = _httpClientFactory.CreateClient("SupplierServiceClient");
                AddAuthorizationHeader(client);
                var response = await client.PostAsJsonAsync("api/finance/get-balances", supplierIds);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Dictionary<Guid, decimal>>() ?? new Dictionary<Guid, decimal>();
                }
                return new Dictionary<Guid, decimal>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierClient] Connect Error: {ex.Message}");
                return new Dictionary<Guid, decimal>();
            }
        }

        public async Task<bool> RecordPurchaseReturnAsync(Guid supplierId, decimal amount, string referenceId, string description, string createdBy)
        {
            var payload = new
            {
                SupplierId = supplierId,
                Amount = amount,
                ReferenceId = referenceId,
                Description = description,
                TransactionDate = DateTime.Now,
                CreatedBy = createdBy,
                TransactionType = "DebitNote"
            };

            var client = _httpClientFactory.CreateClient("SupplierServiceClient");
            AddAuthorizationHeader(client);
            
            var response = await client.PostAsJsonAsync("api/finance/purchase-return-entry", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[SupplierClient] Purchase Return Failed: {response.StatusCode} - {content}");
                    return false;
            }
            return true;
        }

        public async Task<List<Guid>> SearchSupplierIdsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<Guid>();

            try
            {
                var client = _httpClientFactory.CreateClient("SupplierServiceClient");
                AddAuthorizationHeader(client);
                var response = await client.GetAsync($"api/Supplier/search-ids?name={Uri.EscapeDataString(name)}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Guid>>() ?? new List<Guid>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[SupplierClient] Search failed for '{name}': {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierClient] Search Error: {ex.Message}");
            }

            return new List<Guid>();
        }

        public async Task<SupplierSelectDto?> GetSupplierByIdAsync(Guid id)
        {
            var client = _httpClientFactory.CreateClient("SupplierServiceClient");
            AddAuthorizationHeader(client);

            var response = await client.GetAsync($"api/Supplier/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SupplierSelectDto>();
            }

            return null;
        }
    }
}


