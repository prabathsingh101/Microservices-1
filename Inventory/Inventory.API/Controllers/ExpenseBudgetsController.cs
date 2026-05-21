using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/expense-budgets")]
[Authorize]
public class ExpenseBudgetsController : ControllerBase
{
    private readonly IInventoryDbContext _context;

    public ExpenseBudgetsController(IInventoryDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET api/expense-budgets?month=5&year=2025
    /// Returns budgets with computed SpentAmount from ExpenseEntries for that period.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin, User, Manager, Employee, Super Admin")]
    public async Task<IActionResult> GetBudgets([FromQuery] int? month = null, [FromQuery] int? year = null, [FromQuery] string? branchId = null)
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

        Guid companyId;
        if (!Guid.TryParse(companyIdHeader, out companyId) && !Guid.TryParse(companyIdClaim, out companyId))
            return BadRequest("CompanyId is required.");

        string? finalBranchId = !string.IsNullOrEmpty(branchId)
            ? branchId
            : (!string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null"
                ? branchIdHeader
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null));

        int targetMonth = month ?? DateTime.Now.Month;
        int targetYear = year ?? DateTime.Now.Year;

        var budgets = await _context.ExpenseBudgets
            .Include(b => b.ExpenseCategory)
            .Where(b => b.CompanyId == companyId
                && (b.BranchId == null || string.IsNullOrEmpty(finalBranchId) || b.BranchId == finalBranchId)
                && b.Month == targetMonth
                && b.Year == targetYear)
            .ToListAsync();

        // Compute actual spent from ExpenseEntries for same period
        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var spentByCategory = await _context.ExpenseEntries
            .Where(e => e.CompanyId == companyId
                && (e.BranchId == null || string.IsNullOrEmpty(finalBranchId) || e.BranchId == finalBranchId)
                && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, SpentAmount = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.SpentAmount);

        var result = budgets.Select(b => new
        {
            b.Id,
            b.ExpenseCategoryId,
            CategoryName = b.ExpenseCategory?.Name ?? "Unknown",
            b.BudgetAmount,
            SpentAmount = spentByCategory.ContainsKey(b.ExpenseCategoryId) ? spentByCategory[b.ExpenseCategoryId] : 0m,
            b.Month,
            b.Year,
            b.BranchId,
            b.CompanyId
        });

        return Ok(result);
    }

    /// <summary>
    /// GET api/expense-budgets/categories-with-spend?month=5&year=2025
    /// Returns ALL expense categories with their budget (if set) and actual spend for the given period.
    /// </summary>
    [HttpGet("categories-with-spend")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Super Admin")]
    public async Task<IActionResult> GetCategoriesWithSpend([FromQuery] int? month = null, [FromQuery] int? year = null, [FromQuery] string? branchId = null)
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

        Guid companyId;
        if (!Guid.TryParse(companyIdHeader, out companyId) && !Guid.TryParse(companyIdClaim, out companyId))
            return BadRequest("CompanyId is required.");

        string? finalBranchId = !string.IsNullOrEmpty(branchId)
            ? branchId
            : (!string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null"
                ? branchIdHeader
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null));

        int targetMonth = month ?? DateTime.Now.Month;
        int targetYear = year ?? DateTime.Now.Year;

        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var categories = await _context.ExpenseCategories
            .Where(c => c.IsActive)
            .ToListAsync();

        var budgets = await _context.ExpenseBudgets
            .Where(b => b.CompanyId == companyId
                && (b.BranchId == null || string.IsNullOrEmpty(finalBranchId) || b.BranchId == finalBranchId)
                && b.Month == targetMonth && b.Year == targetYear)
            .ToDictionaryAsync(b => b.ExpenseCategoryId);

        var spentByCategory = await _context.ExpenseEntries
            .Where(e => e.CompanyId == companyId
                && (e.BranchId == null || string.IsNullOrEmpty(finalBranchId) || e.BranchId == finalBranchId)
                && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, SpentAmount = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.SpentAmount);

        var result = categories.Select(c => new
        {
            CategoryId = c.Id,
            CategoryName = c.Name,
            BudgetId = budgets.ContainsKey(c.Id) ? budgets[c.Id].Id : (Guid?)null,
            BudgetAmount = budgets.ContainsKey(c.Id) ? budgets[c.Id].BudgetAmount : 0m,
            SpentAmount = spentByCategory.ContainsKey(c.Id) ? spentByCategory[c.Id] : 0m,
            Month = targetMonth,
            Year = targetYear
        });

        return Ok(result);
    }

    /// <summary>
    /// POST api/expense-budgets
    /// Set or update a budget for a category/month/year.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin, Manager, Super Admin")]
    public async Task<IActionResult> SetBudget([FromBody] SetBudgetRequest request)
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

        Guid companyId;
        if (!Guid.TryParse(companyIdHeader, out companyId) && !Guid.TryParse(companyIdClaim, out companyId))
            return BadRequest("CompanyId is required.");

        string? finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null"
            ? branchIdHeader
            : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

        // Check if budget already exists for this category/month/year/branch
        var existing = await _context.ExpenseBudgets
            .FirstOrDefaultAsync(b => b.CompanyId == companyId
                && (b.BranchId == null || string.IsNullOrEmpty(finalBranchId) || b.BranchId == finalBranchId)
                && b.ExpenseCategoryId == request.ExpenseCategoryId
                && b.Month == request.Month
                && b.Year == request.Year);

        if (existing != null)
        {
            existing.BudgetAmount = request.BudgetAmount;
            existing.ModifiedOn = DateTime.UtcNow;
        }
        else
        {
            var budget = new ExpenseBudget
            {
                ExpenseCategoryId = request.ExpenseCategoryId,
                BudgetAmount = request.BudgetAmount,
                Month = request.Month,
                Year = request.Year,
                CompanyId = companyId,
                BranchId = finalBranchId
            };
            _context.ExpenseBudgets.Add(budget);
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Budget saved successfully" });
    }

    /// <summary>
    /// DELETE api/expense-budgets/{id}
    /// Remove a budget entry.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, Manager, Super Admin")]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        var budget = await _context.ExpenseBudgets.FindAsync(id);
        if (budget == null) return NotFound();

        _context.ExpenseBudgets.Remove(budget);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Budget deleted successfully" });
    }
}

public class SetBudgetRequest
{
    public Guid ExpenseCategoryId { get; set; }
    public decimal BudgetAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
