using System.Collections.Generic;

namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class PurchaseOrderPagedResponse
    {
        public List<PurchaseOrderDto> Data { get; set; }
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public int TodayCount { get; set; }
        public int MonthCount { get; set; }

        public PurchaseOrderPagedResponse(List<PurchaseOrderDto> data, int totalRecords, decimal totalAmount, int todayCount, int monthCount)
        {
            Data = data;
            TotalRecords = totalRecords;
            TotalAmount = totalAmount;
            TodayCount = todayCount;
            MonthCount = monthCount;
        }
    }
}
