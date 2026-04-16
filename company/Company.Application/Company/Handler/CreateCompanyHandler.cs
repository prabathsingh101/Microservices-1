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
                logoPath = cmd.Request.LogoUrl ?? string.Empty; // Agar simple URL hai toh
            }

            // 🚀 ID/IDEMPOTENCY LOGIC:
            // Priority: 1. ID from Request (CompanyId), 2. ID from Token, 3. New Guid
            var targetId = cmd.Request.CompanyId ?? _currentUserService.CompanyId ?? Guid.NewGuid();
            var existing = await _repo.GetByIdAsync(targetId);

            if (existing != null)
            {
                // If exists, perform an UPDATE instead of skip
                existing.Name = cmd.Request.Name;
                existing.Tagline = cmd.Request.Tagline;
                existing.RegistrationNumber = cmd.Request.RegistrationNumber;
                existing.Gstin = cmd.Request.Gstin;
                if (!string.IsNullOrEmpty(logoPath)) existing.LogoUrl = logoPath;
                existing.PrimaryEmail = cmd.Request.PrimaryEmail;
                existing.Email = cmd.Request.Email;
                existing.SmtpEmail = cmd.Request.SmtpEmail;
                existing.SmtpPassword = cmd.Request.SmtpPassword;
                existing.SmtpHost = cmd.Request.SmtpHost;
                existing.SmtpPort = cmd.Request.SmtpPort;
                existing.SmtpUseSsl = cmd.Request.SmtpUseSsl ?? true;
                existing.PrimaryPhone = cmd.Request.PrimaryPhone;
                existing.Website = cmd.Request.Website;
                existing.Message = cmd.Request.Message;
                existing.DriverWhatsAppMessage = cmd.Request.DriverWhatsAppMessage;
                existing.SaleReturnWindowValue = cmd.Request.SaleReturnWindowValue;
                existing.SaleReturnWindowUnit = cmd.Request.SaleReturnWindowUnit;
                existing.SaleReturnPolicyDisclaimer = cmd.Request.SaleReturnPolicyDisclaimer;
                existing.PurchaseReturnWindowValue = cmd.Request.PurchaseReturnWindowValue;
                existing.PurchaseReturnWindowUnit = cmd.Request.PurchaseReturnWindowUnit;
                existing.PurchaseReturnPolicyDisclaimer = cmd.Request.PurchaseReturnPolicyDisclaimer;
                existing.InvoiceFooterMessage = cmd.Request.InvoiceFooterMessage;
                existing.EstimateFooterMessage = cmd.Request.EstimateFooterMessage;
                existing.PurchaseOrderFooterMessage = cmd.Request.PurchaseOrderFooterMessage;
                existing.SaleOrderFooterMessage = cmd.Request.SaleOrderFooterMessage;
                existing.PurchaseOrderCreationMessage = cmd.Request.PurchaseOrderCreationMessage;
                existing.PurchaseOrderStatusUpdateMessage = cmd.Request.PurchaseOrderStatusUpdateMessage;
                existing.SaleOrderCreationMessage = cmd.Request.SaleOrderCreationMessage;
                existing.SaleOrderConfirmationMessage = cmd.Request.SaleOrderConfirmationMessage;
                existing.IsActive = true; 

                // Sync Address
                var addr = existing.Addresses.FirstOrDefault();
                if (addr != null)
                {
                    addr.AddressLine1 = cmd.Request.Address.AddressLine1;
                    addr.AddressLine2 = cmd.Request.Address.AddressLine2;
                    addr.City = cmd.Request.Address.City;
                    addr.State = cmd.Request.Address.State;
                    addr.StateCode = cmd.Request.Address.StateCode;
                    addr.PinCode = cmd.Request.Address.PinCode;
                    addr.Country = cmd.Request.Address.Country ?? "India";
                    addr.Email = cmd.Request.Address.Email;
                }
                else
                {
                    existing.Addresses.Add(new Address
                    {
                        AddressLine1 = cmd.Request.Address.AddressLine1,
                        AddressLine2 = cmd.Request.Address.AddressLine2,
                        City = cmd.Request.Address.City,
                        State = cmd.Request.Address.State,
                        StateCode = cmd.Request.Address.StateCode,
                        PinCode = cmd.Request.Address.PinCode,
                        Country = cmd.Request.Address.Country ?? "India",
                        Email = cmd.Request.Address.Email
                    });
                }

                // Sync Bank
                var bank = existing.BankDetails.FirstOrDefault();
                if (bank != null)
                {
                    bank.BankName = cmd.Request.BankInfo.BankName;
                    bank.BranchName = cmd.Request.BankInfo.BranchName;
                    bank.AccountNumber = cmd.Request.BankInfo.AccountNumber;
                    bank.IfscCode = cmd.Request.BankInfo.IfscCode;
                    bank.AccountType = cmd.Request.BankInfo.AccountType ?? "Current";
                    bank.Email = cmd.Request.BankInfo.Email;
                }
                else
                {
                    existing.BankDetails.Add(new BankDetail
                    {
                        BankName = cmd.Request.BankInfo.BankName,
                        BranchName = cmd.Request.BankInfo.BranchName,
                        AccountNumber = cmd.Request.BankInfo.AccountNumber,
                        IfscCode = cmd.Request.BankInfo.IfscCode,
                        AccountType = cmd.Request.BankInfo.AccountType ?? "Current",
                        Email = cmd.Request.BankInfo.Email
                    });
                }

                await _repo.UpsertCompanyProfileAsync(existing);
                return existing.Id;
            }

            // Mapping to Domain Entity for New Insert
            var company = new CompanyProfile
            {
                Id = targetId,
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

                Addresses = new List<Address>
                {
                    new Address
                    {
                        AddressLine1 = cmd.Request.Address.AddressLine1,
                        AddressLine2 = cmd.Request.Address.AddressLine2,
                        City = cmd.Request.Address.City,
                        State = cmd.Request.Address.State,
                        StateCode = cmd.Request.Address.StateCode, // Max 2
                        PinCode = cmd.Request.Address.PinCode,
                        Country = cmd.Request.Address.Country ?? "India",
                        Email = cmd.Request.Address.Email
                    }
                },
                BankDetails = new List<BankDetail>
                {
                    new BankDetail
                    {
                        BankName = cmd.Request.BankInfo.BankName,
                        BranchName = cmd.Request.BankInfo.BranchName,
                        AccountNumber = cmd.Request.BankInfo.AccountNumber,
                        IfscCode = cmd.Request.BankInfo.IfscCode,
                        AccountType = cmd.Request.BankInfo.AccountType ?? "Current",
                        Email = cmd.Request.BankInfo.Email
                    }
                },

                AuthorizedSignatories = new List<AuthorizedSignatory>()
            };

            if (cmd.Request.AuthorizedSignatories != null)
            {
                foreach (var sDto in cmd.Request.AuthorizedSignatories)
                {
                    string signaturePath = sDto.SignatureImageUrl ?? string.Empty;
                    if (!string.IsNullOrEmpty(sDto.SignatureImageUrl) && sDto.SignatureImageUrl.Contains("base64"))
                    {
                        string sFolderPath = Path.Combine(_environment.WebRootPath, "uploads", "signatures");
                        if (!Directory.Exists(sFolderPath)) Directory.CreateDirectory(sFolderPath);

                        string fileName = $"sig_{Guid.NewGuid()}.png";
                        string fullPath = Path.Combine(sFolderPath, fileName);

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