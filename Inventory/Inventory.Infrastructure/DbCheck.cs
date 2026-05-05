using Microsoft.EntityFrameworkCore;
using Inventory.Infrastructure.Persistence;
using Inventory.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Application.Common.Interfaces;

public class DbCheck
{
    public async Task Run(IServiceProvider services)
    {
        var context = services.GetRequiredService<InventoryDbContext>();
        var grnNumber = "GRN-2026-1027";
        
        var rejections = await (from gd in context.GRNDetails
                                join gh in context.GRNHeaders on gd.GRNHeaderId equals gh.Id
                                where gh.GRNNumber == grnNumber && gd.RejectedQty > 0
                                select new { gd.ProductId, gd.RejectedQty, gh.PurchaseOrderId, gd.CompanyId }).ToListAsync();

        Console.WriteLine($"Found {rejections.Count} rejections for {grnNumber}");
        foreach (var rej in rejections)
        {
            Console.WriteLine($"Product: {rej.ProductId}, Qty: {rej.RejectedQty}, Company: {rej.CompanyId}");
        }
    }
}
