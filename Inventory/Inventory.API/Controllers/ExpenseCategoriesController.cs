using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/expense-categories")]
public class ExpenseCategoriesController : ControllerBase
{
    private readonly IInventoryDbContext _context;

    public ExpenseCategoriesController(IInventoryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetAll()
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var branchIdClaim = User.FindFirst("BranchId")?.Value;

        var query = _context.ExpenseCategories
            .Where(x => x.IsActive);

        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            var branchId = branchIdClaim;
            query = query.Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
        }

        var result = await query
            .OrderBy(x => x.Name)
            .ToListAsync();
        return Ok(result);
    }

    [HttpPost("all")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetAllPost()
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var branchIdClaim = User.FindFirst("BranchId")?.Value;

        var query = _context.ExpenseCategories
            .Where(x => x.IsActive);

        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            var branchId = branchIdClaim;
            query = query.Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
        }

        var result = await query
            .OrderBy(x => x.Name)
            .ToListAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _context.ExpenseCategories.FindAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Create(ExpenseCategory category)
    {
        // 🚀 SMART INJECTION: Get CompanyId & BranchId from Claims
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var branchIdClaim = User.FindFirst("BranchId")?.Value;

        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            category.CompanyId = companyId;
        }

        if (!string.IsNullOrEmpty(branchIdClaim))
        {
            category.BranchId = branchIdClaim;
        }

        _context.ExpenseCategories.Add(category);
        await _context.SaveChangesAsync();
        return Ok(category);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Update(Guid id, ExpenseCategory category)
    {
        if (id != category.Id) return BadRequest();
        
        var existing = await _context.ExpenseCategories.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = category.Name;
        existing.Description = category.Description;
        existing.IsActive = category.IsActive;
        existing.ModifiedOn = DateTime.Now;

        // 🚀 SMART INJECTION: Ensure CompanyId & BranchId are safe on update
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var branchIdClaim = User.FindFirst("BranchId")?.Value;

        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            existing.CompanyId = companyId;
        }

        if (!string.IsNullOrEmpty(branchIdClaim))
        {
            existing.BranchId = branchIdClaim;
        }

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _context.ExpenseCategories.FindAsync(id);
        if (category == null) return NotFound();

        // Check if any entries exist
        var hasEntries = await _context.ExpenseEntries.AnyAsync(x => x.CategoryId == id);
        if (hasEntries)
        {
            category.IsActive = false; // Soft delete
        }
        else
        {
            _context.ExpenseCategories.Remove(category);
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Category deleted successfully" });
    }
}
