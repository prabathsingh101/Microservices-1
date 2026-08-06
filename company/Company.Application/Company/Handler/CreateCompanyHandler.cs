using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Company.Application.Common.Interfaces;
using Company.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Hosting;

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
                string webRoot = !string.IsNullOrEmpty(_environment.WebRootPath)
                    ? _environment.WebRootPath
                    : Path.Combine(_environment.ContentRootPath, "wwwroot");

                // 1. wwwroot ke andar folder path set karein
                string folderPath = Path.Combine(webRoot, "uploads", "logos");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string ext = ".png";
                if (cmd.Request.LogoUrl.Contains("image/svg+xml")) ext = ".svg";
                else if (cmd.Request.LogoUrl.Contains("image/jpeg") || cmd.Request.LogoUrl.Contains("image/jpg")) ext = ".jpg";
                else if (cmd.Request.LogoUrl.Contains("image/webp")) ext = ".webp";

                // 2. Unique file name generate karein
                string fileName = $"logo_{Guid.NewGuid()}{ext}";
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
            // Priority: 1. ID from Request (CompanyId), 2. New Guid
            var targetId = cmd.Request.CompanyId ?? Guid.NewGuid();

            // 🔍 DUPLICATE NAME CHECK:
            var byName = await _repo.GetByNameAsync(cmd.Request.Name);
            if (byName != null && byName.Id != targetId)
            {
                throw new Exception($"Company with name '{cmd.Request.Name}' already exists.");
            }

            // 🔍 DUPLICATE EMAIL CHECK:
            if (!string.IsNullOrEmpty(cmd.Request.PrimaryEmail))
            {
                var byEmail = await _repo.GetByEmailAsync(cmd.Request.PrimaryEmail);
                if (byEmail != null && byEmail.Id != targetId)
                {
                    throw new Exception($"Company with business email '{cmd.Request.PrimaryEmail}' already exists.");
                }
            }

            // 🔍 DUPLICATE PHONE CHECK:
            if (!string.IsNullOrEmpty(cmd.Request.PrimaryPhone) && cmd.Request.PrimaryPhone != "0000000000")
            {
                var byPhone = await _repo.GetByPhoneAsync(cmd.Request.PrimaryPhone);
                if (byPhone != null && byPhone.Id != targetId)
                {
                    throw new Exception($"Company with business phone '{cmd.Request.PrimaryPhone}' already exists.");
                }
            }

            var existing = await _repo.GetByIdAsync(targetId);

            // 🔍 DUPLICATE BANK ACCOUNT CHECK:
            if (cmd.Request.BankInfo != null && !string.IsNullOrWhiteSpace(cmd.Request.BankInfo.AccountNumber) && !string.IsNullOrWhiteSpace(cmd.Request.BankInfo.IfscCode))
            {
                var existingBank = existing?.BankDetails.FirstOrDefault();
                bool bankDetailsChanged = existingBank == null || 
                                          existingBank.AccountNumber != cmd.Request.BankInfo.AccountNumber || 
                                          existingBank.IfscCode != cmd.Request.BankInfo.IfscCode;

                if (bankDetailsChanged)
                {
                    bool isBankDuplicate = await _repo.HasDuplicateBankAccountAsync(
                        cmd.Request.BankInfo.AccountNumber, 
                        cmd.Request.BankInfo.IfscCode, 
                        targetId);
                        
                    if (isBankDuplicate)
                    {
                        throw new Exception($"The bank account details (Account Number: '{cmd.Request.BankInfo.AccountNumber}' and IFSC: '{cmd.Request.BankInfo.IfscCode}') are already registered with another company.");
                    }
                }
            }

            if (existing != null)
            {
                // If exists, perform an UPDATE instead of skip
                existing.CompanyCode = cmd.Request.CompanyCode;
                existing.CompanyType = cmd.Request.CompanyType;
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
                existing.ShowBatchAndExpiry = cmd.Request.ShowBatchAndExpiry ?? true;
                existing.ShowExpiredColumn = cmd.Request.ShowExpiredColumn ?? true;
                existing.Website = cmd.Request.Website;
                existing.Message = cmd.Request.Message;
                existing.DriverWhatsAppMessage = cmd.Request.DriverWhatsAppMessage;
                existing.RazorpayKeyId = cmd.Request.RazorpayKeyId;
                existing.RazorpaySecretKey = cmd.Request.RazorpaySecretKey;
                existing.RazorpayXAccountNumber = cmd.Request.RazorpayXAccountNumber;
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

                // Sync Branches (Addresses)
                foreach (var addrDto in cmd.Request.Addresses)
                {
                    var existingAddr = existing.Addresses.FirstOrDefault(a => a.Id.ToString() == addrDto.Id?.ToString());
                    if (existingAddr != null)
                    {
                        existingAddr.BranchName = addrDto.BranchName;
                        existingAddr.AddressLine1 = addrDto.AddressLine1;
                        existingAddr.AddressLine2 = addrDto.AddressLine2;
                        existingAddr.City = addrDto.City;
                        existingAddr.State = addrDto.State;
                        existingAddr.StateCode = addrDto.StateCode;
                        existingAddr.PinCode = addrDto.PinCode;
                        existingAddr.Country = addrDto.Country ?? "India";
                        existingAddr.Email = addrDto.Email;
                        existingAddr.Phone = addrDto.Phone;
                        existingAddr.ContactPerson = addrDto.ContactPerson;
                        existingAddr.Gstin = addrDto.Gstin;
                        existingAddr.IsHeadOffice = addrDto.IsHeadOffice;
                    }
                    else
                    {
                        existing.Addresses.Add(new Address
                        {
                            BranchName = addrDto.BranchName,
                            AddressLine1 = addrDto.AddressLine1,
                            AddressLine2 = addrDto.AddressLine2,
                            City = addrDto.City,
                            State = addrDto.State,
                            StateCode = addrDto.StateCode,
                            PinCode = addrDto.PinCode,
                            Country = addrDto.Country ?? "India",
                            Email = addrDto.Email,
                            Phone = addrDto.Phone,
                            ContactPerson = addrDto.ContactPerson,
                            Gstin = addrDto.Gstin,
                            IsHeadOffice = addrDto.IsHeadOffice
                        });
                    }
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
                    bank.UpiId = cmd.Request.BankInfo.UpiId;
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
                        Email = cmd.Request.BankInfo.Email,
                        UpiId = cmd.Request.BankInfo.UpiId
                    });
                }

                await _repo.UpsertCompanyProfileAsync(existing);
                return existing.Id;
            }

            // Mapping to Domain Entity for New Insert
            var company = new CompanyProfile
            {
                Id = targetId,
                CompanyCode = cmd.Request.CompanyCode,
                CompanyType = cmd.Request.CompanyType,
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
                RazorpayKeyId = cmd.Request.RazorpayKeyId,
                RazorpaySecretKey = cmd.Request.RazorpaySecretKey,
                RazorpayXAccountNumber = cmd.Request.RazorpayXAccountNumber,
                SaleReturnWindowValue = cmd.Request.SaleReturnWindowValue,
                SaleReturnWindowUnit = cmd.Request.SaleReturnWindowUnit,
                SaleReturnPolicyDisclaimer = cmd.Request.SaleReturnPolicyDisclaimer,
                PurchaseReturnWindowValue = cmd.Request.PurchaseReturnWindowValue,
                PurchaseReturnWindowUnit = cmd.Request.PurchaseReturnWindowUnit,
                PurchaseReturnPolicyDisclaimer = cmd.Request.PurchaseReturnPolicyDisclaimer,
                IsActive = true,
                ShowBatchAndExpiry = cmd.Request.ShowBatchAndExpiry ?? true,
                ShowExpiredColumn = cmd.Request.ShowExpiredColumn ?? true,
                InvoiceFooterMessage = cmd.Request.InvoiceFooterMessage,
                EstimateFooterMessage = cmd.Request.EstimateFooterMessage,
                PurchaseOrderFooterMessage = cmd.Request.PurchaseOrderFooterMessage,
                SaleOrderFooterMessage = cmd.Request.SaleOrderFooterMessage,
                PurchaseOrderCreationMessage = cmd.Request.PurchaseOrderCreationMessage,
                PurchaseOrderStatusUpdateMessage = cmd.Request.PurchaseOrderStatusUpdateMessage,
                SaleOrderCreationMessage = cmd.Request.SaleOrderCreationMessage,
                SaleOrderConfirmationMessage = cmd.Request.SaleOrderConfirmationMessage,

                Addresses = cmd.Request.Addresses.Select(addrDto => new Address
                {
                    BranchName = addrDto.BranchName,
                    AddressLine1 = addrDto.AddressLine1,
                    AddressLine2 = addrDto.AddressLine2,
                    City = addrDto.City,
                    State = addrDto.State,
                    StateCode = addrDto.StateCode,
                    PinCode = addrDto.PinCode,
                    Country = addrDto.Country ?? "India",
                    Email = addrDto.Email,
                    Phone = addrDto.Phone,
                    ContactPerson = addrDto.ContactPerson,
                    Gstin = addrDto.Gstin,
                    IsHeadOffice = addrDto.IsHeadOffice
                }).ToList(),
                BankDetails = new List<BankDetail>
                {
                    new BankDetail
                    {
                        BankName = cmd.Request.BankInfo.BankName,
                        BranchName = cmd.Request.BankInfo.BranchName,
                        AccountNumber = cmd.Request.BankInfo.AccountNumber,
                        IfscCode = cmd.Request.BankInfo.IfscCode,
                        AccountType = cmd.Request.BankInfo.AccountType ?? "Current",
                        Email = cmd.Request.BankInfo.Email,
                        UpiId = cmd.Request.BankInfo.UpiId
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

            // 🚀 AUTOMATIC SUBSCRIPTION BRIDGE:
            // When a company is created manually from side menu, we trigger Identity to create a default subscription
            // so it appears in the management list and the company becomes "Active".
            // FIX: We skip this if the creation is triggered internally (e.g. from Identity Social Login)
            if (!cmd.Request.IsInternalSync.GetValueOrDefault())
            {
                // Fetch configured Identity URL from appsettings.json
                var identityUrl = _configuration["ServiceUrls:IdentityApi"];
                
                if (string.IsNullOrEmpty(identityUrl)) 
                {
                    throw new Exception("[Configuration Error]: 'ServiceUrls:IdentityApi' is missing in appsettings.json. Cannot sync subscription.");
                }
                
                var client = _httpClientFactory.CreateClient();
                var onboardReq = new
                {
                    CompanyId = resultId,
                    CompanyName = company.Name,
                    CompanyCode = company.CompanyCode,
                    PlanType = "Trial",
                    DurationDays = 14
                };

                var response = await client.PostAsJsonAsync($"{identityUrl}/api/admin/subscriptions/onboard", onboardReq);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    throw new Exception($"[Subscription Sync Failed]: Identity service returned {response.StatusCode}. Details: {errorMsg}");
                }
            }

            return resultId;
        }
    }
}