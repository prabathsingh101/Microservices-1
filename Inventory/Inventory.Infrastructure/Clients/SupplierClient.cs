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
        private readonly MassTransit.IPublishEndpoint _publishEndpoint;

        public SupplierClient(IHttpClientFactory factory, IHttpContextAccessor httpContextAccessor, MassTransit.IPublishEndpoint publishEndpoint)
        {
            _httpClientFactory = factory;
            _httpContextAccessor = httpContextAccessor;
            _publishEndpoint = publishEndpoint;
        }

        private void AddAuthorizationHeader(HttpClient client)
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    // 1. Propagate Authorization Header (JWT)
                    if (context.Request.Headers.ContainsKey("Authorization"))
                    {
                        var authHeader = context.Request.Headers["Authorization"].ToString();
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            var token = authHeader.Substring("Bearer ".Length).Trim();
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        }
                    }

                    // 2. Propagate Tenant Headers (Crucial for Multi-tenancy)
                    if (context.Request.Headers.ContainsKey("X-Company-Id"))
                    {
                        client.DefaultRequestHeaders.Add("X-Company-Id", context.Request.Headers["X-Company-Id"].ToString());
                    }

                    if (context.Request.Headers.ContainsKey("X-Branch-Id"))
                    {
                        client.DefaultRequestHeaders.Add("X-Branch-Id", context.Request.Headers["X-Branch-Id"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierClient] Failed to propagate headers: {ex.Message}");
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
                Guid? companyId = null;
                string? branchId = null;
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    if (context.Request.Headers.TryGetValue("X-Company-Id", out var compHeader) && Guid.TryParse(compHeader, out var compId))
                    {
                        companyId = compId;
                    }
                    if (context.Request.Headers.TryGetValue("X-Branch-Id", out var brHeader))
                    {
                        branchId = brHeader.ToString();
                    }
                }

                await _publishEndpoint.Publish<Shared.Contracts.SupplierPurchaseCreatedEvent>(new
                {
                    SupplierId = supplierId,
                    Amount = amount,
                    ReferenceId = referenceId,
                    Description = description,
                    TransactionDate = DateTime.Now,
                    CreatedBy = createdBy,
                    CompanyId = companyId,
                    BranchId = branchId
                });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierClient] Error publishing purchase created event: {ex.Message}");
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
            try
            {
                Guid? companyId = null;
                string? branchId = null;
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    if (context.Request.Headers.TryGetValue("X-Company-Id", out var compHeader) && Guid.TryParse(compHeader, out var compId))
                    {
                        companyId = compId;
                    }
                    if (context.Request.Headers.TryGetValue("X-Branch-Id", out var brHeader))
                    {
                        branchId = brHeader.ToString();
                    }
                }

                await _publishEndpoint.Publish<Shared.Contracts.SupplierPurchaseReturnCreatedEvent>(new
                {
                    SupplierId = supplierId,
                    Amount = amount,
                    ReferenceId = referenceId,
                    Description = description,
                    TransactionDate = DateTime.Now,
                    CreatedBy = createdBy,
                    CompanyId = companyId,
                    BranchId = branchId
                });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierClient] Error publishing purchase return created event: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RecordPaymentAsync(Guid supplierId, decimal amount, string referenceNumber, string remarks, string paymentMode, string createdBy)
        {
            try
            {
                Guid? companyId = null;
                string? branchId = null;
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    if (context.Request.Headers.TryGetValue("X-Company-Id", out var compHeader) && Guid.TryParse(compHeader, out var compId))
                    {
                        companyId = compId;
                    }
                    if (context.Request.Headers.TryGetValue("X-Branch-Id", out var brHeader))
                    {
                        branchId = brHeader.ToString();
                    }
                }

                await _publishEndpoint.Publish<Shared.Contracts.SupplierPaymentCreatedEvent>(new
                {
                    SupplierId = supplierId,
                    Amount = amount,
                    ReferenceNumber = referenceNumber,
                    Remarks = remarks,
                    PaymentMode = paymentMode,
                    PaymentDate = DateTime.Now,
                    CreatedBy = createdBy,
                    CompanyId = companyId,
                    BranchId = branchId
                });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupplierClient] Error publishing supplier payment created event: {ex.Message}");
                throw;
            }
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


