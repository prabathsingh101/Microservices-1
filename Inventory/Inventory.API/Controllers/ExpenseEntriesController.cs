using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/expense-entries")]
public class ExpenseEntriesController : ControllerBase
{
    private readonly IInventoryDbContext _context;

    public ExpenseEntriesController(IInventoryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null)
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

        string? branchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
            ? branchIdHeader 
            : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

        var query = _context.ExpenseEntries
            .Include(x => x.Category)
            .AsQueryable();

        if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
        {
            query = query.Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x => (x.Category != null && x.Category.Name.Contains(search)) || 
                                     (x.Remarks != null && x.Remarks.Contains(search)) ||
                                     (x.ReferenceNo != null && x.ReferenceNo.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.ExpenseDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { items, totalCount });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

        string? branchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
            ? branchIdHeader 
            : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

        var query = _context.ExpenseEntries
            .Include(x => x.Category)
            .AsQueryable();

        if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
        {
            query = query.Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
        }

        var result = await query.FirstOrDefaultAsync(x => x.Id == id);
        
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Create(ExpenseEntry entry)
    {
        // 🚀 SMART INJECTION: Get CompanyId & BranchId from Headers or Claims
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
        
        if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
        {
            entry.CompanyId = companyId;
        }

        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

        string? finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
            ? branchIdHeader 
            : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

        if (!string.IsNullOrEmpty(finalBranchId))
        {
            entry.BranchId = finalBranchId;
        }

        _context.ExpenseEntries.Add(entry);
        await _context.SaveChangesAsync();
        return Ok(entry);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Update(Guid id, ExpenseEntry entry)
    {
        if (id != entry.Id) return BadRequest();

        var existing = await _context.ExpenseEntries.FindAsync(id);
        if (existing == null) return NotFound();

        existing.CategoryId = entry.CategoryId;
        existing.Amount = entry.Amount;
        existing.ExpenseDate = entry.ExpenseDate;
        existing.PaymentMode = entry.PaymentMode;
        existing.ReferenceNo = entry.ReferenceNo;
        existing.Remarks = entry.Remarks;
        existing.AttachmentPath = entry.AttachmentPath;
        existing.ModifiedOn = DateTime.Now;

        // 🚀 SMART INJECTION: Ensure CompanyId & BranchId are safe on update
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
        
        if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
        {
            existing.CompanyId = companyId;
        }

        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

        string? finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
            ? branchIdHeader 
            : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

        if (!string.IsNullOrEmpty(finalBranchId))
        {
            existing.BranchId = finalBranchId;
        }

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entry = await _context.ExpenseEntries.FindAsync(id);
        if (entry == null) return NotFound();

        _context.ExpenseEntries.Remove(entry);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Expense entry deleted successfully" });
    }

    [HttpPost("chart-data")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetChartData([FromBody] DashboardFilter filters)
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

        string? branchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
            ? branchIdHeader 
            : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

        var query = _context.ExpenseEntries
            .Include(x => x.Category)
            .AsQueryable();

        if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
        {
            query = query.Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
        }

        if (filters.StartDate.HasValue)
            query = query.Where(x => x.ExpenseDate >= filters.StartDate.Value);
        
        if (filters.EndDate.HasValue)
            query = query.Where(x => x.ExpenseDate <= filters.EndDate.Value);

        var data = await query
            .GroupBy(x => x.Category!.Name)
            .Select(g => new
            {
                Category = g.Key,
                Amount = g.Sum(x => x.Amount)
            })
            .ToListAsync();
        
        return Ok(data);
    }

    [HttpGet("monthly-totals")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetMonthlyTotals([FromQuery] int months = 6)
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
        Guid? companyId = null;
        if (Guid.TryParse(companyIdHeader, out var cidH)) companyId = cidH;
        else if (Guid.TryParse(companyIdClaim, out var cidC)) companyId = cidC;

        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();
        string? branchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
            ? branchIdHeader 
            : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

        var startDate = DateTime.Today.AddMonths(-(months - 1));
        startDate = new DateTime(startDate.Year, startDate.Month, 1);

        var expenseQuery = _context.ExpenseEntries.AsQueryable();
        var purchaseQuery = _context.PurchaseOrders.AsQueryable();

        if (companyId.HasValue)
        {
            expenseQuery = expenseQuery.Where(x => x.CompanyId == companyId.Value && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
            purchaseQuery = purchaseQuery.Where(x => x.CompanyId == companyId.Value && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
        }

        var expenseData = await expenseQuery
            .Where(x => x.ExpenseDate >= startDate)
            .ToListAsync();

        var purchaseData = await purchaseQuery
            .Where(x => x.PoDate >= startDate)
            .Select(x => new { x.PoDate, x.GrandTotal })
            .ToListAsync();

        var trend = new List<object>();
        for (int i = 0; i < months; i++)
        {
            var date = DateTime.Today.AddMonths(-i);
            var monthLabel = new DateTime(date.Year, date.Month, 1).ToString("MMM yyyy");
            
            var monthExpense = expenseData
                .Where(x => x.ExpenseDate.Year == date.Year && x.ExpenseDate.Month == date.Month)
                .Sum(x => x.Amount);
                
            var monthPurchase = purchaseData
                .Where(x => x.PoDate.Year == date.Year && x.PoDate.Month == date.Month)
                .Sum(x => x.GrandTotal);

            trend.Add(new
            {
                Month = monthLabel,
                Amount = monthExpense + monthPurchase
            });
        }

        return Ok(trend.OrderBy(t => DateTime.Parse(((dynamic)t).Month)).ToList());
    }
}

public class DashboardFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
