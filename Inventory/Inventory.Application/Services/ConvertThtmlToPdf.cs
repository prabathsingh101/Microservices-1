using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Services
{
    public static class ConvertThtmlToPdf
    {
        public static string GenerateHtmlTemplate(CreditNotePrintDto data)
        {
            var sb = new StringBuilder();
            
            // Company Info Logic
            string companyHeader = @"
            <div class='header'>
                <h1>CREDIT NOTE</h1>
                <p>Electric Inventory System</p>
            </div>";

            if (data.CompanyInfo != null)
            {
                var comp = data.CompanyInfo;
                var address = comp.Address != null 
                    ? $"{comp.Address.AddressLine1}, {comp.Address.City}, {comp.Address.State} - {comp.Address.PinCode}" 
                    : "";

                companyHeader = $@"
                <div class='header' style='text-align: left; display: flex; justify-content: space-between; align-items: center;'>
                    <div style='float: left;'>
                        {(string.IsNullOrEmpty(comp.LogoUrl) ? "" : $"<img src='{comp.LogoUrl}' style='height: 60px; margin-bottom: 10px;' />")}
                        <h2 style='margin: 0; color: #2563eb;'>{comp.Name}</h2>
                        <p style='margin: 2px 0; font-size: 12px; color: #555;'>{address}</p>
                        <p style='margin: 2px 0; font-size: 12px; color: #555;'>Ph: {comp.PrimaryPhone} | Email: {comp.PrimaryEmail}</p>
                    </div>
                    <div style='float: right; text-align: right;'>
                        <h1 style='margin: 0; color: #333;'>CREDIT NOTE</h1>
                        <p style='margin: 5px 0; font-size: 14px;'>#{data.ReturnNumber}</p>
                    </div>
                    <div style='clear: both;'></div>
                </div>";
            }

            sb.Append($@"
        <html>
        <head>
            <style>
                body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; padding: 20px; }}
                .header {{ border-bottom: 2px solid #eee; padding-bottom: 20px; margin-bottom: 20px; }}
                .info-table {{ width: 100%; margin: 20px 0; border-collapse: collapse; }}
                .info-table td {{ padding: 8px 0; vertical-align: top; }}
                .items-table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
                .items-table th {{ background-color: #f8f9fa; color: #495057; font-weight: 600; text-align: left; padding: 12px 8px; border-bottom: 2px solid #dee2e6; }}
                .items-table td {{ padding: 10px 8px; border-bottom: 1px solid #dee2e6; color: #212529; }}
                .total-section {{ float: right; width: 300px; margin-top: 30px; background-color: #f9f9f9; padding: 15px; border-radius: 5px; }}
                .text-right {{ text-align: right; }}
                h1, h2, h3 {{ font-family: 'Segoe UI', sans-serif; }}
            </style>
        </head>
        <body>
            {companyHeader}
            <table class='info-table'>
                <tr>
                    <td><strong>Customer:</strong> {data.CustomerName}</td>
                    <td class='text-right'><strong>Date:</strong> {data.ReturnDate.ToShortDateString()}</td>
                </tr>
                <tr>
                    <td><strong>Return No:</strong> {data.ReturnNumber}</td>
                    <td class='text-right'><strong>Ref SO:</strong> {data.SONumber}</td>
                </tr>
            </table>
            <table class='items-table'>
                <thead>
                    <tr>
                        <th>Product</th>
                        <th>Qty</th>
                        <th>Rate</th>
                        <th>Disc%</th>
                        <th>Tax%</th>
                        <th>Total</th>
                    </tr>
                </thead>
                <tbody>");

            foreach (var item in data.Items)
            {
                sb.Append($@"
                <tr>
                    <td>{item.ProductName}</td>
                    <td>{item.Qty}</td>
                    <td>{item.Rate:N2}</td>
                    <td>{item.DiscountPercent}%</td>
                    <td>{item.TaxPercent}%</td>
                    <td>{item.Total:N2}</td>
                </tr>");
            }

            sb.Append($@"
                </tbody>
            </table>
            <div class='total-section'>
                <p>Sub-Total: {data.SubTotal:N2}</p>
                <p>Discount: {data.TotalDiscount:N2}</p>
                <p>Tax: {data.TotalTax:N2}</p>
                <hr/>
                <h3>Grand Total: ?{data.GrandTotal:N2}</h3>
            </div>
        </body>
        </html>");

            return sb.ToString();
        }
    }
}
