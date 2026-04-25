using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suppliers.Application.Common.Models;
using Suppliers.Application.DTOs;
using Suppliers.Application.Features.Suppliers.Queries;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.Features.Suppliers.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Suppliers.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ISupplierRepository _supplierRepository;

        public SupplierController(IMediator mediator, ISupplierRepository repository)
        {
            _mediator = mediator;
            _supplierRepository = repository;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
        {
            // 🚀 SMART INJECTION: Get CompanyId & BranchId from Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var userEmail = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "System";

            if (!string.IsNullOrEmpty(companyIdClaim))
            {
                dto = dto with { companyId = companyIdClaim };
            }

            if (!string.IsNullOrEmpty(branchIdClaim))
            {
                dto = dto with { branchId = branchIdClaim };
            }

            dto = dto with { createdBy = userEmail };

            var command = new CreateSupplierCommand(dto);
            var id = await _mediator.Send(command);
            return Ok(id);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetAll()
        {
            var query = new GetAllSuppliersQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateSupplierDto dto)
        {
            // 🚀 SMART INJECTION: Get CompanyId & BranchId from Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var userEmail = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "System";

            if (!string.IsNullOrEmpty(companyIdClaim))
            {
                dto = dto with { companyId = companyIdClaim };
            }

            if (!string.IsNullOrEmpty(branchIdClaim))
            {
                dto = dto with { branchId = branchIdClaim };
            }

            dto = dto with { modifiedBy = userEmail };

            var result = await _mediator.Send(new UpdateSupplierCommand(id, dto));
            return result ? Ok(result) : NotFound();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(new DeleteSupplierCommand(id));
            return result ? Ok(result) : NotFound();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetSupplierByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(new { message = "Supplier not found" });
            }

            return Ok(result);
        }

        [HttpPost("paged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetSuppliers([FromBody] GridRequest query)
        {
            var result = await _mediator.Send(new GetSuppliersPagedQuery(query));
            return Ok(result);
        }

        [HttpPost("get-by-ids")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSuppliersByIds([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return Ok(new List<SupplierSelectDto>());
            }

            try
            {
                var suppliers = await _supplierRepository.GetSuppliersByIdsAsync(ids);
                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error in Supplier Microservice.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("search-ids")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<ActionResult<List<Guid>>> SearchIdsByName([FromQuery] string name)
        {
            var ids = await _supplierRepository.GetIdsByNameAsync(name);
            return Ok(ids);
        }
    }
}
