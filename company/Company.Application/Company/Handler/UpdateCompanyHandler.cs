using Company.Application.Common.Interfaces;
using Company.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Hosting; // IWebHostEnvironment ke liye

namespace Company.Application.Company.Commands.Update.Handler
{
    public class UpdateCompanyHandler : IRequestHandler<UpdateCompanyCommand, Guid>
    {
        private readonly ICompanyRepository _repo;
        private readonly IWebHostEnvironment _environment; // wwwroot access ke liye

        public UpdateCompanyHandler(ICompanyRepository repo, IWebHostEnvironment environment)
        {
            _repo = repo; //
            _environment = environment;
        }

        public async Task<Guid> Handle(UpdateCompanyCommand cmd, CancellationToken ct)
        {
            // Pehle existing profile load karte hain with related data
            var profile = await _repo.GetByIdAsync(cmd.Id); // Fix: Use cmd.Id instead of GetCompanyProfileAsync

            if (profile == null) return Guid.Empty;

            // --- Logo Update Logic ---
            if (!string.IsNullOrEmpty(cmd.Request.LogoUrl))
            {
                if (cmd.Request.LogoUrl.Contains("base64"))
                {
                    // 1. Purani file delete karein
                    if (!string.IsNullOrEmpty(profile.LogoUrl))
                    {
                        var oldPath = Path.Combine(_environment.WebRootPath, profile.LogoUrl.TrimStart('/'));
                        if (File.Exists(oldPath)) File.Delete(oldPath);
                    }

                    // 2. Nayi file save karein
                    string folderPath = Path.Combine(_environment.WebRootPath, "uploads", "logos");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    string fileName = $"logo_{Guid.NewGuid()}.png";
                    string fullPath = Path.Combine(folderPath, fileName);

                    var base64Data = cmd.Request.LogoUrl.Split(',')[1];
                    byte[] imageBytes = Convert.FromBase64String(base64Data);
                    await File.WriteAllBytesAsync(fullPath, imageBytes);

                    profile.LogoUrl = $"/uploads/logos/{fileName}";
                }
                else
                {
                    profile.LogoUrl = cmd.Request.LogoUrl;
                }
            }

            // 1. Main Profile Fields Update
            profile.CompanyCode = cmd.Request.CompanyCode;
            profile.Name = cmd.Request.Name;
            profile.Tagline = cmd.Request.Tagline;
            profile.RegistrationNumber = cmd.Request.RegistrationNumber;
            profile.Gstin = cmd.Request.Gstin; // Max 15 chars
            profile.PrimaryEmail = cmd.Request.PrimaryEmail;
            profile.Email = cmd.Request.Email;
            profile.SmtpEmail = cmd.Request.SmtpEmail;
            profile.SmtpPassword = cmd.Request.SmtpPassword;
            profile.SmtpHost = cmd.Request.SmtpHost;
            profile.SmtpPort = cmd.Request.SmtpPort;
            profile.SmtpUseSsl = cmd.Request.SmtpUseSsl ?? true;
            profile.PrimaryPhone = cmd.Request.PrimaryPhone;
            profile.Website = cmd.Request.Website;
            profile.Message = cmd.Request.Message;
            profile.DriverWhatsAppMessage = cmd.Request.DriverWhatsAppMessage;
            profile.SaleReturnWindowValue = cmd.Request.SaleReturnWindowValue;
            profile.SaleReturnWindowUnit = cmd.Request.SaleReturnWindowUnit;
            profile.SaleReturnPolicyDisclaimer = cmd.Request.SaleReturnPolicyDisclaimer;
            profile.PurchaseReturnWindowValue = cmd.Request.PurchaseReturnWindowValue;
            profile.PurchaseReturnWindowUnit = cmd.Request.PurchaseReturnWindowUnit;
            profile.PurchaseReturnPolicyDisclaimer = cmd.Request.PurchaseReturnPolicyDisclaimer;
            profile.InvoiceFooterMessage = cmd.Request.InvoiceFooterMessage;
            profile.EstimateFooterMessage = cmd.Request.EstimateFooterMessage;
            profile.PurchaseOrderFooterMessage = cmd.Request.PurchaseOrderFooterMessage;
            profile.SaleOrderFooterMessage = cmd.Request.SaleOrderFooterMessage;
            profile.PurchaseOrderCreationMessage = cmd.Request.PurchaseOrderCreationMessage;
            profile.PurchaseOrderStatusUpdateMessage = cmd.Request.PurchaseOrderStatusUpdateMessage;
            profile.SaleOrderCreationMessage = cmd.Request.SaleOrderCreationMessage;
            profile.SaleOrderConfirmationMessage = cmd.Request.SaleOrderConfirmationMessage;

            // 2. Branch Update (Multi-location sync)
            foreach (var addrDto in cmd.Request.Addresses)
            {
                int.TryParse(addrDto.Id?.ToString(), out var addrId);
                var existingAddr = profile.Addresses.FirstOrDefault(a => a.Id == addrId && addrId != 0);

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
                    profile.Addresses.Add(new Address
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

            // 3. Bank Information Update
            var bank = profile.BankDetails.FirstOrDefault();
            if (bank != null)
            {
                bank.BankName = cmd.Request.BankInfo.BankName;
                bank.BranchName = cmd.Request.BankInfo.BranchName;
                bank.AccountNumber = cmd.Request.BankInfo.AccountNumber;
                bank.IfscCode = cmd.Request.BankInfo.IfscCode;
                bank.AccountType = cmd.Request.BankInfo.AccountType;
                bank.Email = cmd.Request.BankInfo.Email;
            }
            else
            {
                profile.BankDetails.Add(new BankDetail
                {
                    BankName = cmd.Request.BankInfo.BankName,
                    BranchName = cmd.Request.BankInfo.BranchName,
                    AccountNumber = cmd.Request.BankInfo.AccountNumber,
                    IfscCode = cmd.Request.BankInfo.IfscCode,
                    AccountType = cmd.Request.BankInfo.AccountType,
                    Email = cmd.Request.BankInfo.Email
                });
            }

            // 4. Authorized Signatories Update
            if (cmd.Request.AuthorizedSignatories != null)
            {
                // Remove signatories not in the request
                var requestIds = cmd.Request.AuthorizedSignatories
                    .Select(s => int.TryParse(s.Id?.ToString(), out var sid) ? sid : 0)
                    .Where(id => id > 0)
                    .ToList();
                var toRemove = profile.AuthorizedSignatories.Where(s => !requestIds.Contains(s.Id)).ToList();
                foreach (var s in toRemove) profile.AuthorizedSignatories.Remove(s);

                // Add or Update
                foreach (var sDto in cmd.Request.AuthorizedSignatories)
                {
                    string signaturePath = sDto.SignatureImageUrl ?? string.Empty;
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

                    int sId = int.TryParse(sDto.Id?.ToString(), out var sid) ? sid : 0;
                    var existing = profile.AuthorizedSignatories.FirstOrDefault(x => x.Id == sId && sId != 0);
                    if (existing != null)
                    {
                        existing.PersonName = sDto.PersonName;
                        existing.Designation = sDto.Designation;
                        existing.SignatureImageUrl = signaturePath;
                        existing.Email = sDto.Email;
                        existing.IsDefault = sDto.IsDefault;
                    }
                    else
                    {
                        profile.AuthorizedSignatories.Add(new AuthorizedSignatory
                        {
                            PersonName = sDto.PersonName,
                            Designation = sDto.Designation,
                            SignatureImageUrl = signaturePath,
                            Email = sDto.Email,
                            IsDefault = sDto.IsDefault
                        });
                    }
                }

            }

            // Database mein changes save karte hain

            return await _repo.UpsertCompanyProfileAsync(profile);
        }
    }
}