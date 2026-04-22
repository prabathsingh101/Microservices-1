using System;
using ClosedXML.Excel;

namespace ExcelReader
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] files = {
                @"c:\Projects\ElectricApps\public\assets\templates\category_template.xlsx",
                @"c:\Projects\ElectricApps\public\assets\templates\subcategory_template.xlsx",
                @"c:\Projects\ElectricApps\public\assets\templates\product_template.xlsx"
            };

            foreach (var file in files)
            {
                Console.WriteLine($"\n--- Reading File: {file} ---");
                try {
                    using (var workbook = new XLWorkbook(file))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed();
                        foreach (var row in rows)
                        {
                            var values = new System.Collections.Generic.List<string>();
                            foreach (var cell in row.Cells())
                            {
                                values.Add(cell.Value.ToString());
                            }
                            Console.WriteLine(string.Join("|", values));
                        }
                    }
                } catch(Exception ex) {
                    Console.WriteLine($"Error reading {file}: {ex.Message}");
                }
            }
        }
    }
}
