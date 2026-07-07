using Company.Application.Company.Commands.Create;
using Company.Application.Company.Commands.Delete;
using Company.Application.Company.Commands.Update;
using Company.Application.Company.Commands.UploadLogo;
using Company.Application.Company.Queries;
using Company.Application.Common.Models;
using Company.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

namespace Company.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _environment;
        private readonly Company.Infrastructure.Persistence.CompanyDbContext _dbContext;

        public CompanyController(IMediator mediator, IWebHostEnvironment environment, Company.Infrastructure.Persistence.CompanyDbContext dbContext)
        {
            _mediator = mediator;
            _environment = environment;
            _dbContext = dbContext;
        }

        [HttpPost("create")]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] UpsertCompanyRequest req)
        {
            var id = await _mediator.Send(new CreateCompanyCommand(req));
            return Ok(id);
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin, Salesman")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpsertCompanyRequest req)
        {
            var resultId = await _mediator.Send(new UpdateCompanyCommand(id, req));
            return resultId != Guid.Empty ? Ok(resultId) : NotFound();
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _mediator.Send(new GetCompanyProfileQuery());
            return result != null ? Ok(result) : NotFound();
        }

        // 2. Get By ID
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetCompanyByIdQuery(id));
            return result != null ? Ok(result) : NotFound();
        }

        [HttpGet("public-by-code/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicByCode(string code)
        {
            var company = await _dbContext.CompanyProfiles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyCode.ToLower() == code.ToLower());
            if (company == null) return NotFound("Company not found.");
            return Ok(new { 
                id = company.Id,
                name = company.Name, 
                logoUrl = company.LogoUrl,
                companyCode = company.CompanyCode
            });
        }

        [HttpPost("paged")]
        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin, Salesman")]
        public async Task<IActionResult> GetPaged([FromBody] GridRequest request)
        {
            var result = await _mediator.Send(new GetCompaniesPagedQuery(request));
            return Ok(result);
        }

        // 3. Delete Profile
        [HttpDelete("{id}")]

        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin, Salesman")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _mediator.Send(new DeleteCompanyCommand(id));
            return success ? NoContent() : BadRequest("Could not delete profile.");
        }

        [HttpPost("upload-logo/{id}")]
        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin, Salesman")]
        public async Task<IActionResult> UploadLogo(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            string folderPath = Path.Combine(_environment.WebRootPath, "uploads", "logos");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string fileName = $"logo_{id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string fullPath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string logoUrl = $"/uploads/logos/{fileName}";
            var success = await _mediator.Send(new UploadLogoCommand(id, logoUrl));

            return success ? Ok(new { logoUrl }) : BadRequest("Could not update logo URL.");
        }

        [HttpGet("pincode/{pincode}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPincodeDetails(string pincode)
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                try
                {
                    var response = await client.GetAsync($"https://api.postalpincode.in/pincode/{pincode}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        return Content(content, "application/json");
                    }
                    return StatusCode((int)response.StatusCode);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }

        [HttpGet("states")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStates()
        {
            try
            {
                var states = await _dbContext.States
                    .OrderBy(s => s.Name)
                    .Select(s => new {
                        s.Name,
                        s.Code,
                        DefaultCity = s.DefaultCity,
                        DefaultPinCode = s.DefaultPinCode
                    })
                    .ToListAsync();
                return Ok(states);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("check-duplicate")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string field, [FromQuery] string value, [FromQuery] Guid? excludeId, [FromQuery] string? additionalValue = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(value))
                {
                    return BadRequest("Field and value are required.");
                }

                var query = _dbContext.CompanyProfiles.IgnoreQueryFilters().AsNoTracking().AsQueryable();
                
                if (excludeId.HasValue && excludeId.Value != Guid.Empty)
                {
                    query = query.Where(c => c.Id != excludeId.Value);
                }

                bool exists = false;
                switch (field.ToLower())
                {
                    case "bankaccount":
                        if (string.IsNullOrWhiteSpace(additionalValue))
                        {
                            return BadRequest("IFSC Code (additionalValue) is required for bank account duplicate check.");
                        }

                        // If the company being edited already has these exact bank details, we don't treat it as a duplicate.
                        if (excludeId.HasValue && excludeId.Value != Guid.Empty)
                        {
                            var currentCompanyBank = await _dbContext.BankDetails
                                .AsNoTracking()
                                .FirstOrDefaultAsync(b => b.CompanyProfileId == excludeId.Value);

                            if (currentCompanyBank != null && 
                                currentCompanyBank.AccountNumber == value && 
                                currentCompanyBank.IfscCode != null && 
                                currentCompanyBank.IfscCode.ToUpper() == additionalValue.ToUpper())
                            {
                                exists = false;
                                break;
                            }
                        }

                        var bankQuery = _dbContext.BankDetails.AsNoTracking().AsQueryable();
                        if (excludeId.HasValue && excludeId.Value != Guid.Empty)
                        {
                            bankQuery = bankQuery.Where(b => b.CompanyProfileId != excludeId.Value);
                        }
                        exists = await bankQuery.AnyAsync(b => b.AccountNumber == value && b.IfscCode != null && b.IfscCode.ToUpper() == additionalValue.ToUpper());
                        break;
                    case "companycode":
                        exists = await query.AnyAsync(c => c.CompanyCode == value);
                        break;
                    case "name":
                        exists = await query.AnyAsync(c => c.Name == value);
                        break;
                    case "primaryemail":
                        exists = await query.AnyAsync(c => c.PrimaryEmail == value || c.Email == value);
                        break;
                    case "email":
                        exists = await query.AnyAsync(c => c.Email == value || c.PrimaryEmail == value);
                        break;
                    case "primaryphone":
                        exists = await query.AnyAsync(c => c.PrimaryPhone == value);
                        break;
                    case "gstin":
                        exists = await query.AnyAsync(c => c.Gstin == value);
                        break;
                    case "registrationnumber":
                        exists = await query.AnyAsync(c => c.RegistrationNumber == value);
                        break;
                    default:
                        return BadRequest("Invalid field specified.");
                }

                return Ok(new { isDuplicate = exists });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("verify-bank-account")]
        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin, Salesman")]
        public async Task<IActionResult> VerifyBankAccount([FromBody] VerifyBankAccountRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.AccountNumber) || string.IsNullOrWhiteSpace(req.Ifsc))
            {
                return BadRequest("Account number and IFSC code are required.");
            }

            try
            {
                var profile = await _mediator.Send(new GetCompanyProfileQuery());
                if (profile == null)
                {
                    return BadRequest("Company profile not found.");
                }

                if (string.IsNullOrWhiteSpace(profile.RazorpayKeyId) || string.IsNullOrWhiteSpace(profile.RazorpaySecretKey))
                {
                    return BadRequest("Razorpay API Keys are not configured in your Company Profile.");
                }

                // If it's a test key, mock the response so they can test the workflow without a real RazorpayX account.
                if (profile.RazorpayKeyId.Trim().StartsWith("rzp_test", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(1000); // Simulate network call latency
                    
                    string mockName = "TEST ACCOUNT HOLDER";
                    string cleanAcc = (req.AccountNumber ?? "").Replace(" ", "").Trim();
                    
                    if (cleanAcc == "50220006188827" || cleanAcc.Contains("50220006188827") || cleanAcc.EndsWith("6188827"))
                    {
                        mockName = "NIKKI KUMARI";
                    }
                    else if (cleanAcc == "071601522524" || cleanAcc.Contains("071601522524") || cleanAcc.EndsWith("522524"))
                    {
                        mockName = "PAPPU KUMAR SINGH";
                    }

                    return Ok(new { 
                        registeredName = mockName, 
                        status = "completed" 
                    });
                }

                if (string.IsNullOrWhiteSpace(profile.RazorpayXAccountNumber))
                {
                    return BadRequest("RazorpayX Payout Account Number is required for live verification. Please update it in your Company Profile.");
                }

                using var client = new System.Net.Http.HttpClient();
                var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{profile.RazorpayKeyId}:{profile.RazorpaySecretKey}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

                var payload = new
                {
                    account_number = profile.RazorpayXAccountNumber,
                    fund_account = new
                    {
                        account_type = "bank_account",
                        bank_account = new
                        {
                            name = "Verification Temp Name",
                            ifsc = req.Ifsc.Trim().ToUpper(),
                            account_number = req.AccountNumber.Trim()
                        }
                    }
                };

                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new System.Net.Http.StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.razorpay.com/v1/fund_accounts/validations", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                        if (jsonDoc.RootElement.TryGetProperty("error", out var errorEl) && errorEl.TryGetProperty("description", out var descEl))
                        {
                            return BadRequest($"Razorpay Error: {descEl.GetString()}");
                        }
                    }
                    catch { }
                    return BadRequest($"Razorpay validation failed with status {response.StatusCode}.");
                }

                using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                string registeredName = "";
                if (root.TryGetProperty("results", out var resultsEl) && resultsEl.TryGetProperty("registered_name", out var regNameEl))
                {
                    registeredName = regNameEl.GetString() ?? "";
                }

                string status = "";
                if (root.TryGetProperty("status", out var statusEl))
                {
                    status = statusEl.GetString() ?? "";
                }

                if (status == "failed")
                {
                    return BadRequest("Bank account validation failed. Please check the account number and IFSC code.");
                }

                return Ok(new { registeredName, status });
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred during verification: {ex.Message}");
            }
        }
    }

    public record VerifyBankAccountRequest(string AccountNumber, string Ifsc);
}

