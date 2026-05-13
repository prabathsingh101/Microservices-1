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
        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> Create([FromBody] UpsertCompanyRequest req)
        {
            var id = await _mediator.Send(new CreateCompanyCommand(req));
            return Ok(id);
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpsertCompanyRequest req)
        {
            var resultId = await _mediator.Send(new UpdateCompanyCommand(id, req));
            return resultId != Guid.Empty ? Ok(resultId) : NotFound();
        }

        [HttpGet("profile")]
        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _mediator.Send(new GetCompanyProfileQuery());
            return result != null ? Ok(result) : NotFound();
        }

        // 2. Get By ID
        [HttpGet("{id}")]
        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetCompanyByIdQuery(id));
            return result != null ? Ok(result) : NotFound();
        }

        [HttpPost("paged")]
        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> GetPaged([FromBody] GridRequest request)
        {
            var result = await _mediator.Send(new GetCompaniesPagedQuery(request));
            return Ok(result);
        }

        // 3. Delete Profile
        [HttpDelete("{id}")]

        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _mediator.Send(new DeleteCompanyCommand(id));
            return success ? NoContent() : BadRequest("Could not delete profile.");
        }

        [HttpPost("upload-logo/{id}")]
        [Authorize(Roles = "Default Admin, Admin, User, Manager, Employee, Warehouse,Super Admin")]
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
    }
}

