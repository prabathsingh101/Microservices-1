using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using Inventory.Application.Common.Interfaces;

namespace Inventory.Application.Services
{
    public class CustomerHttpService : ICustomerHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly ISaleReturnRepository _saleReturnRepository;

        public CustomerHttpService(HttpClient httpClient, 
            IHttpClientFactory httpClientFactory, ISaleReturnRepository returnRepository)
        {
            if (httpClientFactory == null) throw new ArgumentNullException(nameof(httpClientFactory));

            _httpClient = httpClientFactory.CreateClient("CustomerService");

            _saleReturnRepository = returnRepository;
        }

        public async Task<Dictionary<Guid, string>> GetCustomerNamesAsync(List<Guid> customerIds)
        {
            if (customerIds == null || !customerIds.Any())
                return new Dictionary<Guid, string>();

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/customers/get-names", customerIds);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<Dictionary<Guid, string>>();
                    return data ?? new Dictionary<Guid, string>();
                }
            }
            catch (Exception ex)
            {
             
                Console.WriteLine($"Customer Microservice call failed: {ex.Message}");
            }

            return new Dictionary<Guid, string>();
        }

        
    }
}
