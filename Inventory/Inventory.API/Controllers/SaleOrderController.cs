using ClosedXML.Excel;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.DTOs.SaleOrder;
using Inventory.Application.SaleOrders.Commands;
using Inventory.Application.SaleOrders.DTOs;
using Inventory.Application.SaleOrders.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.Application.Clients;
using Inventory.Domain.Entities;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.SignalR;
using Inventory.API.Hubs;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SaleOrderController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISaleOrderRepository _saleRepo;
    private readonly IInventoryDbContext _context;
    private readonly ICustomerClient _customerClient;
    private readonly IHubContext<DeliveryHub> _hubContext;

    public SaleOrderController(IMediator mediator, 
        ISaleOrderRepository stockRepo,
        IInventoryDbContext context,
        ICustomerClient customerClient,
        IHubContext<DeliveryHub> hubContext) 
    {  
        _mediator = mediator;
        _saleRepo = stockRepo;
        _context = context;
        _customerClient = customerClient;
        _hubContext = hubContext;
    }

    [HttpPost("save")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> Save([FromBody] CreateSaleOrderDto dto)
    {
        // 🚀 SMART INJECTION: Get CompanyId & BranchId from Headers or Claims
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
        
        if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
        {
            dto.CompanyId = companyId;
        }

        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

        string? finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
            ? branchIdHeader 
            : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

        if (!string.IsNullOrEmpty(finalBranchId))
        {
            dto.BranchId = finalBranchId;
        }

        // 1. Mediator ab pura object return karega (Id aur SONumber)
        var result = await _mediator.Send(new CreateSaleOrderCommand(dto));

        // 2. Real-time broadcast if this is a HomeDelivery order
        if (dto.DeliveryType == "HomeDelivery" && !string.IsNullOrEmpty(dto.BranchId))
        {
            string initialStatus = !string.IsNullOrEmpty(dto.DeliveryBoyId) ? "Assigned" : "Pending";
            await _hubContext.Clients.Group(dto.BranchId).SendAsync("ReceiveDeliveryUpdate", new { status = initialStatus });
        }

        // 3. Result ko as it is return karein taaki frontend ko result.soNumber mil sake
        return Ok(result);
    }

    [HttpPost("export")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> ExportSaleOrderReport([FromBody] List<Guid> orderIds) // Guid
    {
        // 1. Validation check karein taaki 400 error handle ho sake
        if (orderIds == null || !orderIds.Any())
            return BadRequest("Kripya orders select karein.");

        // 2. Repository se selected integer IDs ka data fetch karein
        var data = await _saleRepo.GetSaleReportDataAsync(orderIds);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Stock Report");

            // Header Row Setup
            var headers = new string[] { "Product Name", "Unit", "Received", "Rejected", "Available Stock" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Data Rows filling
            for (int i = 0; i < data.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = data[i].ProductName;
                worksheet.Cell(i + 2, 2).Value = data[i].Unit;
                worksheet.Cell(i + 2, 3).Value = data[i].TotalReceived;
                worksheet.Cell(i + 2, 4).Value = data[i].TotalRejected;
                worksheet.Cell(i + 2, 5).Value = data[i].AvailableStock;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                // Excel file return logic [cite: 2026-02-03]
                return File(content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Stock_Report_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
    }

    [HttpGet]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> GetSaleOrders(
     [FromQuery] string searchTerm = "",
     [FromQuery] int pageNumber = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] string sortBy = "SODate",
     [FromQuery] string sortOrder = "desc",
     [FromQuery] bool isQuick = false,
     [FromQuery] DateTime? startDate = null,
     [FromQuery] DateTime? endDate = null,
     [FromQuery] string? branchId = null)
    {
        // 1. Repository method call with parameters [cite: 2026-02-03]
        var (orders, totalCount, totalSalesAmount, pendingDispatchCount, unpaidOrdersCount, todayCount, monthCount) = await _saleRepo.GetAllSaleOrdersAsync(
            searchTerm,
            pageNumber,
            pageSize,
            sortBy,
            sortOrder,
            isQuick,
            startDate,
            endDate,
            branchId
        );

        // 2. Return data along with total count and global stats for frontend
        return Ok(new
        {
            data = orders,
            totalCount = totalCount,
            totalSalesAmount = totalSalesAmount,
            pendingDispatchCount = pendingDispatchCount,
            unpaidOrdersCount = unpaidOrdersCount,
            todayCount = todayCount,
            monthCount = monthCount
        });
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] StatusUpdateDto request)
    {
        if (request == null || string.IsNullOrEmpty(request.Status))
            return BadRequest("Status data is missing.");

        var result = await _saleRepo.UpdateSaleOrderStatusAsync(id, request.Status);

        if (result)
        {
            return Ok(new { message = "Order Confirmed! Inventory has been updated." });
        }

        return BadRequest(new { message = "Status update is failed." });
    }

    // Ye DTO binding ke liye zaroori hai
    public class StatusUpdateDto
    {
        public string Status { get; set; } = null!;
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string? reason)
    {
        var result = await _mediator.Send(new DeleteSaleOrderCommand(id, reason));
        if (result)
        {
            return Ok(new { success = true, message = "Sale Order canceled and stock reverted!" });
        }
        return BadRequest(new { success = false, message = "Cancel failed or order not found." });
    }

    // Simple DTO for binding
    public class UpdateStatusRequest
    {
        public string Status { get; set; } = null!;
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<ActionResult<SaleOrderDetailDto>> GetOrder(Guid id)
    {
        var order = await _saleRepo.GetSaleOrderByIdAsync(id);

        if (order == null)
        {
            return NotFound(new { message = "Order nahi mila bhai!" });
        }

        return Ok(order);
    }

    [HttpGet("export-list")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> ExportSaleOrderList()
    {
        // Excel export ke liye hum pagination bypass karenge
        // Hum pageNumber 1 aur pageSize bahut bada (e.g. 1000000) bhejenge taaki sab mil jaye [cite: 2026-02-03]
        var (orders, totalCount, _, _, _, _, _) = await _saleRepo.GetAllSaleOrdersAsync(
            searchTerm: "",
            pageNumber: 1,
            pageSize: 1000000, // Saare records lene ke liye
            sortBy: "SODate",
            sortOrder: "desc"
        );

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Sale Orders");

            // Headers setup [cite: 2026-02-03]
            string[] headers = { "Order #", "Date", "Customer", "Amount", "Status" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#3f51b5"));
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Data fill logic
            int row = 2;
            foreach (var order in orders)
            {
                worksheet.Cell(row, 1).Value = order.SoNumber;
                worksheet.Cell(row, 2).Value = order.SoDate.ToShortDateString();
                worksheet.Cell(row, 3).Value = order.CustomerName;
                worksheet.Cell(row, 4).Value = order.GrandTotal;
                worksheet.Cell(row, 5).Value = order.Status;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                // Excel file return logic [cite: 2026-02-03]
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Sale_Orders.xlsx");
            }
        }
    }


    [HttpGet("orders-by-customer/{customerId}")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> GetOrdersByCustomer(Guid customerId)
    {
        if (customerId == Guid.Empty)
        {
            return BadRequest("Invalid Customer Id");
        }

        // Repository se DTO ki list mangwana [cite: 2026-02-05]
        var orders = await _saleRepo.GetOrdersByCustomerAsync(customerId);

        if (orders == null || !orders.Any())
        {
            return Ok(new List<SaleOrderLookupDto>()); // Empty list if no orders found
        }
        
        return Ok(orders);
    }

    [HttpGet("cancelled-orders-by-customer/{customerId}")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> GetCancelledOrdersByCustomer(Guid customerId)
    {
        if (customerId == Guid.Empty)
        {
            return BadRequest("Invalid Customer Id");
        }

        var orders = await _saleRepo.GetCancelledOrdersByCustomerAsync(customerId);

        if (orders == null || !orders.Any())
        {
            return Ok(new List<SaleOrderLookupDto>());
        }

        return Ok(orders);
    }

    [HttpGet("grid-items/{saleOrderId}")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> GetGridItems(Guid saleOrderId)
    {
        if (saleOrderId == Guid.Empty) return BadRequest("Invalid Sale Order ID");

        // Repository se lightweight DTO list mangwana [cite: 2026-02-05]
        var items = await _saleRepo.GetItemsForGridByOrderIdAsync(saleOrderId);

        if (items == null || !items.Any())
            return Ok(new List<SaleOrderItemGridDto>());

        return Ok(items);
    }

    [HttpGet("pending-sos")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> GetPendingSOs()
    {
        var result = await _mediator.Send(new GetPendingSOQuery());
        return Ok(result);
    }

    [HttpGet("check-phone")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> CheckPhone([FromQuery] string phone)
    {
        if (string.IsNullOrEmpty(phone)) return BadRequest("Phone is required");

        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var companyIdHeader = Request.Headers["X-Company-Id"].ToString();

        if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
        {
            var exists = await _saleRepo.ExistsByPhoneAsync(phone, companyId);
            return Ok(new { exists });
        }

        return BadRequest("Company context missing");
    }

    // --- HOME DELIVERY SYSTEM ENDPOINTS ---

    [HttpGet("delivery-list")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> GetDeliveryList(
        [FromQuery] string? deliveryBoyId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? cashSettled = null,
        [FromQuery] string? branchId = null)
    {
        var activeCompanyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (!Guid.TryParse(activeCompanyIdClaim, out var companyId))
        {
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            Guid.TryParse(companyIdHeader, out companyId);
        }

        var query = _context.SaleOrders
            .Where(x => x.DeliveryType == "HomeDelivery")
            .AsNoTracking();

        if (companyId != Guid.Empty)
        {
            query = query.Where(x => x.CompanyId == companyId);
        }

        if (!string.IsNullOrEmpty(branchId))
        {
            query = query.Where(x => x.BranchId == branchId);
        }

        if (!string.IsNullOrEmpty(deliveryBoyId))
        {
            query = query.Where(x => x.DeliveryBoyId == deliveryBoyId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(x => x.DeliveryStatus == status);
        }

        if (cashSettled.HasValue)
        {
            query = query.Where(x => x.CashSettled == cashSettled.Value);
        }

        var orders = await query
            .OrderByDescending(x => x.SODate)
            .ToListAsync();

        var customerIds = orders
            .Where(x => x.CustomerId.HasValue)
            .Select(x => x.CustomerId!.Value)
            .Distinct()
            .ToList();

        var customerDetails = customerIds.Any()
            ? await _customerClient.GetCustomerDetailsByIdsAsync(customerIds)
            : new Dictionary<Guid, CustomerLookupDto>();

        var result = orders.Select(o => new
        {
            o.Id,
            o.SONumber,
            o.CustomerId,
            CustomerName = o.CustomerId.HasValue && customerDetails.TryGetValue(o.CustomerId.Value, out var c) ? c.CustomerName : null,
            CustomerPhone = o.CustomerId.HasValue && customerDetails.TryGetValue(o.CustomerId.Value, out var c2) ? c2.Phone : null,
            o.PriceListId,
            o.SODate,
            o.ExpectedDeliveryDate,
            o.SubTotal,
            o.TotalTax,
            o.GrandTotal,
            o.TaxType,
            o.Remarks,
            o.Status,
            o.GatePassNo,
            o.IsQuick,
            o.GuestName,
            o.GuestPhone,
            o.CancelReason,
            o.DoctorName,
            o.DoctorRegNo,
            o.DeliveryType,
            o.DeliveryAddress,
            o.DeliverySlot,
            o.DeliveryBoyId,
            o.DeliveryBoyName,
            o.DeliveryCharges,
            o.DeliveryStatus,
            o.CodCollectedAmount,
            o.CodPaymentMode,
            o.CashSettled,
            o.CashSettledDate,
            o.CashSettledBy
        });

        return Ok(result);
    }

    public class AssignDeliveryDto
    {
        public List<Guid> OrderIds { get; set; } = new();
        public string DeliveryBoyId { get; set; } = null!;
        public string DeliveryBoyName { get; set; } = null!;
        public string DeliverySlot { get; set; } = null!;
        public decimal DeliveryCharges { get; set; }
    }

    [HttpPost("assign-delivery")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> AssignDelivery([FromBody] AssignDeliveryDto request)
    {
        if (request == null || request.OrderIds == null || !request.OrderIds.Any())
            return BadRequest("Order IDs are required.");

        var orders = await _context.SaleOrders
            .Where(x => request.OrderIds.Contains(x.Id))
            .ToListAsync();

        foreach (var order in orders)
        {
            order.DeliveryBoyId = request.DeliveryBoyId;
            order.DeliveryBoyName = request.DeliveryBoyName;
            order.DeliverySlot = request.DeliverySlot;
            
            decimal oldCharges = order.DeliveryCharges;
            order.DeliveryCharges = request.DeliveryCharges;
            order.GrandTotal = (order.GrandTotal - oldCharges) + request.DeliveryCharges;
            
            order.DeliveryStatus = "Assigned";
        }

        await _context.SaveChangesAsync();

        foreach (var order in orders)
        {
            if (!string.IsNullOrEmpty(order.BranchId))
            {
                await _hubContext.Clients.Group(order.BranchId).SendAsync("ReceiveDeliveryUpdate", new { orderId = order.Id, status = "Assigned" });
            }
        }

        return Ok(new { success = true, message = "Delivery agent assigned successfully!" });
    }

    public class UpdateDeliveryStatusDto
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; } = null!; // Delivered, Returned, Cancelled, OutForDelivery
        public decimal? CodCollectedAmount { get; set; }
        public string? CodPaymentMode { get; set; } // Cash, UPI
    }

    [HttpPost("update-delivery-status")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> UpdateDeliveryStatus([FromBody] UpdateDeliveryStatusDto request)
    {
        if (request == null)
            return BadRequest("Data is required.");

        var order = await _context.SaleOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId);

        if (order == null)
            return NotFound("Order not found.");

        order.DeliveryStatus = request.Status;

        if (request.Status == "Delivered")
        {
            order.CodCollectedAmount = request.CodCollectedAmount ?? order.GrandTotal;
            order.CodPaymentMode = request.CodPaymentMode ?? "Cash";
            await _context.SaveChangesAsync();
        }
        else if (request.Status == "Returned" || request.Status == "Cancelled")
        {
            order.CodCollectedAmount = 0;
            order.CodPaymentMode = null;

            if (order.Status == "Confirmed" || order.Status == "Delivered" || order.Status == "Completed")
            {
                var cancelledBy = User.Identity?.Name ?? "Delivery Boy";

                await _saleRepo.ExecuteInTransactionAsync(async () =>
                {
                    // 1. Revert Stock
                    foreach (var item in order.Items)
                    {
                        var reversalTx = new InventoryTransaction(
                            item.ProductId,
                            item.Qty, // Positive because it is READDING stock
                            (order.IsQuick ? "QuickSale" : "Sale") + "-DELETED",
                            order.SONumber,
                            item.WarehouseId,
                            item.RackId,
                            item.MfgDate,
                            item.ExpDate,
                            order.CompanyId,
                            order.BranchId
                        );
                        await _context.InventoryTransactions.AddAsync(reversalTx);

                        if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                        {
                            var whStock = await _context.WarehouseStocks
                                .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId);

                            if (whStock != null)
                            {
                                whStock.Quantity += item.Qty;
                            }
                        }
                    }

                    // 2. Ledger Sync (Reverse Sale)
                    if (order.CustomerId.HasValue && order.CustomerId.Value != Guid.Empty)
                    {
                        try
                        {
                            string ledgerNote = $"Sale Order Cancelled/Returned via Delivery Portal. Order: {order.SONumber}";
                            await _customerClient.RecordSaleAsync(
                                order.CustomerId.Value,
                                -order.GrandTotal, // Negative amount
                                order.SONumber,
                                ledgerNote,
                                cancelledBy,
                                order.BranchId,
                                order.CompanyId
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ledger reversion failed in delivery update: {ex.Message}");
                        }
                    }

                    // 3. Update Order Status
                    order.Status = "Cancelled";
                    order.CancelReason = $"Delivery status updated to {request.Status}";

                    await _context.SaveChangesAsync();
                });
            }
            else
            {
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(order.BranchId))
        {
            await _hubContext.Clients.Group(order.BranchId).SendAsync("ReceiveDeliveryUpdate", new { orderId = order.Id, status = request.Status });
        }

        return Ok(new { success = true, message = $"Delivery status updated to {request.Status}." });
    }

    public class SettleDeliveryCashDto
    {
        public List<Guid> OrderIds { get; set; } = new();
    }

    [HttpPost("settle-delivery-cash")]
    [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
    public async Task<IActionResult> SettleDeliveryCash([FromBody] SettleDeliveryCashDto request)
    {
        if (request == null || request.OrderIds == null || !request.OrderIds.Any())
            return BadRequest("Order IDs are required.");

        var settledBy = User.Identity?.Name ?? "Manager";

        var orders = await _context.SaleOrders
            .Where(x => request.OrderIds.Contains(x.Id) && x.DeliveryType == "HomeDelivery" && x.DeliveryStatus == "Delivered" && !x.CashSettled)
            .ToListAsync();

        int settledCount = 0;

        foreach (var order in orders)
        {
            order.CashSettled = true;
            order.CashSettledDate = DateTime.Now;
            order.CashSettledBy = settledBy;
            settledCount++;

            // 1. Permanent Customer: If CustomerId is set and amount collected > 0, post Customer Receipt to credit their ledger
            if (order.CustomerId.HasValue && order.CustomerId.Value != Guid.Empty && (order.CodCollectedAmount ?? 0) > 0)
            {
                try
                {
                    await _customerClient.RecordReceiptAsync(
                        order.CustomerId.Value,
                        order.CodCollectedAmount.Value,
                        order.CodPaymentMode ?? "Cash",
                        $"SETTLE-{order.SONumber}",
                        $"Home Delivery Cash Settle for order: {order.SONumber}",
                        settledBy,
                        order.BranchId,
                        order.CompanyId
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"COD ledger receipt posting failed for order {order.SONumber}: {ex.Message}");
                }
            }
            // 2. Walking Customer: If guest name is present and amount collected > 0, post Customer Receipt to log cash inflow
            else if (!order.CustomerId.HasValue && (order.CodCollectedAmount ?? 0) > 0)
            {
                try
                {
                    await _customerClient.RecordReceiptAsync(
                        null,
                        order.CodCollectedAmount.Value,
                        order.CodPaymentMode ?? "Cash",
                        $"SETTLE-{order.SONumber}",
                        $"Home Delivery Cash Settle (Guest: {order.GuestName}): {order.SONumber}",
                        settledBy,
                        order.BranchId,
                        order.CompanyId
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"COD guest receipt posting failed for order {order.SONumber}: {ex.Message}");
                }
            }
        }

        await _context.SaveChangesAsync();

        foreach (var order in orders)
        {
            if (!string.IsNullOrEmpty(order.BranchId))
            {
                await _hubContext.Clients.Group(order.BranchId).SendAsync("ReceiveDeliveryUpdate", new { orderId = order.Id, status = "Settled" });
            }
        }

        return Ok(new { success = true, message = $"Successfully settled cash for {settledCount} orders." });
    }
}
