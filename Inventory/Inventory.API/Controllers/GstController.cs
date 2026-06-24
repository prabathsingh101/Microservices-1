using Inventory.Application.Gst.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GstController : ControllerBase
    {
        private readonly IGstService _gstService;

        public GstController(IGstService gstService)
        {
            _gstService = gstService;
        }

        [HttpGet("gstr1")]
        [Authorize(Roles = "Super Admin, Admin, Manager, User")]
        public async Task<IActionResult> GetGstr1([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var companyId = ResolveCompanyId();
            if (companyId == Guid.Empty)
            {
                return BadRequest("CompanyId is required and could not be resolved.");
            }

            try
            {
                var fileBytes = await _gstService.GenerateGstr1ExcelAsync(startDate, endDate, companyId);
                string fileName = $"GSTR1_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating GSTR-1: {ex.Message}");
            }
        }

        [HttpGet("gstr3b")]
        [Authorize(Roles = "Super Admin, Admin, Manager, User")]
        public async Task<IActionResult> GetGstr3b([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var companyId = ResolveCompanyId();
            if (companyId == Guid.Empty)
            {
                return BadRequest("CompanyId is required and could not be resolved.");
            }

            try
            {
                var summary = await _gstService.GetGstr3bSummaryAsync(startDate, endDate, companyId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error compiling GSTR-3B: {ex.Message}");
            }
        }

        [HttpPost("reconcile")]
        [Authorize(Roles = "Super Admin, Admin, Manager, User")]
        public async Task<IActionResult> ReconcileGstr2b(
            [FromForm] IFormFile file, 
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            var companyId = ResolveCompanyId();
            if (companyId == Guid.Empty)
            {
                return BadRequest("CompanyId is required and could not be resolved.");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    
                    var result = await _gstService.ReconcileGstr2bAsync(stream, file.FileName, startDate, endDate, companyId);
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error executing reconciliation: {ex.Message}");
            }
        }

        private Guid ResolveCompanyId()
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();

            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                return companyId;
            }

            return Guid.Empty;
        }
    }
}
