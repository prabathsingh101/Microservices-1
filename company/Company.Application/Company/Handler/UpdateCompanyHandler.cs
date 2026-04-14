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
            if (!string.IsNullOrEmpty(cmd.Request.LogoUrl) && cmd.Request.LogoUrl.Contains("base64"))
            {
                // 1. Purani file delete karein agar exist karti hai
                if (!string.IsNullOrEmpty(profile.LogoUrl))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, profile.LogoUrl.TrimStart('/'));
                    if (File.Exists(oldPath))
                    {
                        File.Delete(oldPath);
                    }
                }

                // 2. Nayi file save karein
                string folderPath = Path.Combine(_environment.WebRootPath, "uploads", "logos");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string fileName = $"logo_{Guid.NewGuid()}.png";
                string fullPath = Path.Combine(folderPath, fileName);

                var base64Data = cmd.Request.LogoUrl.Split(',')[1];
                byte[] imageBytes = Convert.FromBase64String(base64Data);
                await File.WriteAllBytesAsync(fullPath, imageBytes);

                profile.LogoUrl = $"/uploads/logos/{fileName}"; // Relative path update
            }
            else
            {
                profile.LogoUrl = cmd.Request.LogoUrl; // Fix: Keep existing logo URL if simple path
            }

            // 1. Main Profile Fields Update
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
            profile.SmtpUseSsl = cmd.Request.SmtpUseSsl;
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

            // 2. Address Update
            if (profile.CompanyAddress != null)
            {
                profile.CompanyAddress.AddressLine1 = cmd.Request.Address.AddressLine1;
                profile.CompanyAddress.AddressLine2 = cmd.Request.Address.AddressLine2;
                profile.CompanyAddress.City = cmd.Request.Address.City;
                profile.CompanyAddress.State = cmd.Request.Address.State;
                profile.CompanyAddress.StateCode = cmd.Request.Address.StateCode; // Max 2 chars
                profile.CompanyAddress.PinCode = cmd.Request.Address.PinCode;
                profile.CompanyAddress.Country = cmd.Request.Address.Country;
                profile.CompanyAddress.Email = cmd.Request.Address.Email;
            }

            // 3. Bank Information Update
            if (profile.BankInformation != null)
            {
                profile.BankInformation.BankName = cmd.Request.BankInfo.BankName;
                profile.BankInformation.BranchName = cmd.Request.BankInfo.BranchName;
                profile.BankInformation.AccountNumber = cmd.Request.BankInfo.AccountNumber;
                profile.BankInformation.IfscCode = cmd.Request.BankInfo.IfscCode;
                profile.BankInformation.AccountType = cmd.Request.BankInfo.AccountType;
                profile.BankInformation.Email = cmd.Request.BankInfo.Email;
            }

            // 4. Authorized Signatories Update
            if (cmd.Request.AuthorizedSignatories != null)
            {
                // Remove signatories not in the request
                var requestIds = cmd.Request.AuthorizedSignatories.Select(s => s.Id).ToList();
                var toRemove = profile.AuthorizedSignatories.Where(s => !requestIds.Contains(s.Id)).ToList();
                foreach (var s in toRemove) profile.AuthorizedSignatories.Remove(s);

                // Add or Update
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

                    var existing = profile.AuthorizedSignatories.FirstOrDefault(x => x.Id == sDto.Id && x.Id != 0);
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