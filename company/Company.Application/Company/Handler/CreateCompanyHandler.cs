using Company.Application.Common.Interfaces;
using Company.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Json;

namespace Company.Application.Company.Commands.Create.Handler
{
    public class CreateCompanyHandler : IRequestHandler<CreateCompanyCommand, Guid>
    {
        private readonly ICompanyRepository _repo;
        private readonly IWebHostEnvironment _environment; 
        private readonly ICurrentUserService _currentUserService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public CreateCompanyHandler(
            ICompanyRepository repo, 
            IWebHostEnvironment environment, 
            ICurrentUserService currentUserService,
            IHttpClientFactory httpClientFactory,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _repo = repo; 
            _environment = environment;
            _currentUserService = currentUserService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<Guid> Handle(CreateCompanyCommand cmd, CancellationToken ct)
        {
            string logoPath = string.Empty;

            // --- Photo Upload Logic ---
            if (!string.IsNullOrEmpty(cmd.Request.LogoUrl) && cmd.Request.LogoUrl.Contains("base64"))
            {
                // 1. wwwroot ke andar folder path set karein
                string folderPath = Path.Combine(_environment.WebRootPath, "uploads", "logos");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // 2. Unique file name generate karein
                string fileName = $"logo_{Guid.NewGuid()}.png";
                string fullPath = Path.Combine(folderPath, fileName);

                // 3. Base64 string se data nikal kar file save karein
                var base64Data = cmd.Request.LogoUrl.Split(',')[1];
                byte[] imageBytes = Convert.FromBase64String(base64Data);
                await File.WriteAllBytesAsync(fullPath, imageBytes);

                // 4. DB mein relative path save karein
                logoPath = $"/uploads/logos/{fileName}";
            }
            else
            {
                logoPath = cmd.Request.LogoUrl; // Agar simple URL hai toh
            }

            // Mapping to Domain Entity
            var company = new CompanyProfile
            {
                Id = _currentUserService.CompanyId ?? Guid.NewGuid(),
                Name = cmd.Request.Name,
                Tagline = cmd.Request.Tagline,
                RegistrationNumber = cmd.Request.RegistrationNumber,
                Gstin = cmd.Request.Gstin, // Max 15
                LogoUrl = logoPath, // Physical file ka path
                PrimaryEmail = cmd.Request.PrimaryEmail,
                Email = cmd.Request.Email,
                SmtpEmail = cmd.Request.SmtpEmail,
                SmtpPassword = cmd.Request.SmtpPassword,
                SmtpHost = cmd.Request.SmtpHost,
                SmtpPort = cmd.Request.SmtpPort,
                SmtpUseSsl = cmd.Request.SmtpUseSsl ?? true,
                PrimaryPhone = cmd.Request.PrimaryPhone,
                Website = cmd.Request.Website,
                Message = cmd.Request.Message,
                DriverWhatsAppMessage = cmd.Request.DriverWhatsAppMessage,
                SaleReturnWindowValue = cmd.Request.SaleReturnWindowValue,
                SaleReturnWindowUnit = cmd.Request.SaleReturnWindowUnit,
                SaleReturnPolicyDisclaimer = cmd.Request.SaleReturnPolicyDisclaimer,
                PurchaseReturnWindowValue = cmd.Request.PurchaseReturnWindowValue,
                PurchaseReturnWindowUnit = cmd.Request.PurchaseReturnWindowUnit,
                PurchaseReturnPolicyDisclaimer = cmd.Request.PurchaseReturnPolicyDisclaimer,
                IsActive = true,
                InvoiceFooterMessage = cmd.Request.InvoiceFooterMessage,
                EstimateFooterMessage = cmd.Request.EstimateFooterMessage,
                PurchaseOrderFooterMessage = cmd.Request.PurchaseOrderFooterMessage,
                SaleOrderFooterMessage = cmd.Request.SaleOrderFooterMessage,
                PurchaseOrderCreationMessage = cmd.Request.PurchaseOrderCreationMessage,
                PurchaseOrderStatusUpdateMessage = cmd.Request.PurchaseOrderStatusUpdateMessage,
                SaleOrderCreationMessage = cmd.Request.SaleOrderCreationMessage,
                SaleOrderConfirmationMessage = cmd.Request.SaleOrderConfirmationMessage,

                CompanyAddress = new Address
                {
                    AddressLine1 = cmd.Request.Address.AddressLine1,
                    AddressLine2 = cmd.Request.Address.AddressLine2,
                    City = cmd.Request.Address.City,
                    State = cmd.Request.Address.State,
                    StateCode = cmd.Request.Address.StateCode, // Max 2
                    PinCode = cmd.Request.Address.PinCode,
                    Country = cmd.Request.Address.Country ?? "India",
                    Email = cmd.Request.Address.Email
                },

                BankInformation = new BankDetail
                {
                    BankName = cmd.Request.BankInfo.BankName,
                    BranchName = cmd.Request.BankInfo.BranchName,
                    AccountNumber = cmd.Request.BankInfo.AccountNumber,
                    IfscCode = cmd.Request.BankInfo.IfscCode,
                    AccountType = cmd.Request.BankInfo.AccountType ?? "Current",
                    Email = cmd.Request.BankInfo.Email
                },

                AuthorizedSignatories = new List<AuthorizedSignatory>()
            };

            if (cmd.Request.AuthorizedSignatories != null)
            {
                foreach (var sDto in cmd.Request.AuthorizedSignatories)
                {
                    string signaturePath = sDto.SignatureImageUrl;
                    if (!string.IsNullOrEmpty(sDto.SignatureImageUrl) && sDto.SignatureImageUrl.Contains("base64"))
                    {
                        string folderPath = Path.Combine(_environment.WebRootPath, "uploads", "signatures");
                        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                        string fileName = $"sig_{Guid.NewGuid()}.png";
                        string fullPath = Path.Combine(folderPath, fileName);

                        var base64Data = sDto.SignatureImageUrl.Split(',')[1];
                        byte[] imageBytes = Convert.FromBase64String(base64Data);
                        await File.WriteAllBytesAsync(fullPath, imageBytes);
                        signaturePath = $"/uploads/signatures/{fileName}";
                    }

                    company.AuthorizedSignatories.Add(new AuthorizedSignatory
                    {
                        PersonName = sDto.PersonName,
                        Designation = sDto.Designation,
                        SignatureImageUrl = signaturePath,
                        Email = sDto.Email,
                        IsDefault = sDto.IsDefault
                    });
                }
            }

            // 🚀 IDEMPOTENCY CHECK: if company already exists, don't re-insert
            var existing = await _repo.GetByIdAsync(company.Id);
            if (existing != null)
            {
                return existing.Id;
            }

            var resultId = await _repo.InsertCompanyAsync(company);

            // 🚀 CROSS-SERVICE SYNC: Tell Identity Service to create Subscription & Bootstrap Roles
            try
            {
                var identityUrl = _configuration["ServiceUrls:IdentityApi"];
                if (string.IsNullOrEmpty(identityUrl)) identityUrl = "http://identity.api:8080"; // Default for Docker

                var client = _httpClientFactory.CreateClient();
                var onboardDto = new
                {
                    CompanyId = resultId,
                    CompanyName = company.Name,
                    PlanType = "Trial",
                    DurationDays = 30,
                    UserId = _currentUserService.UserId
                };

                // Internal Call to Identity API
                await client.PostAsJsonAsync($"{identityUrl.TrimEnd('/')}/api/admin/subscriptions/onboard", onboardDto);
            }
            catch (Exception ex)
            {
                // Using Console for quick logs in dev, should use ILogger in prod
                Console.WriteLine($"[CRITICAL] Onboarding Sync Failed for Company {resultId}: {ex.Message}");
            }

            return resultId;
        }
    }
}