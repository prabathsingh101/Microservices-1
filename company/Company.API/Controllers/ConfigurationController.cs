using Company.Domain.Entities;
using Company.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace Company.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConfigurationController : ControllerBase
    {
        private readonly CompanyDbContext _dbContext;

        public ConfigurationController(CompanyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("by-key/{key}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Configuration>>> GetByKey(string key)
        {
            var values = await _dbContext.Configurations
                .Where(c => c.ConfigKey == key && c.IsActive)
                .ToListAsync();

            if (!values.Any() && key == "CompanyType")
            {
                var defaults = new List<Configuration>
                {
                    new Configuration { ConfigKey = "CompanyType", ConfigValue = "Kirana Store", IsActive = true },
                    new Configuration { ConfigKey = "CompanyType", ConfigValue = "Medico/Pharmacy", IsActive = true },
                    new Configuration { ConfigKey = "CompanyType", ConfigValue = "Furniture Store", IsActive = true },
                    new Configuration { ConfigKey = "CompanyType", ConfigValue = "Electric Shop", IsActive = true },
                    new Configuration { ConfigKey = "CompanyType", ConfigValue = "Hardware Shop", IsActive = true }
                };
                _dbContext.Configurations.AddRange(defaults);
                await _dbContext.SaveChangesAsync();
                values = defaults;
            }
            else if (!values.Any() && key == "SupplierType")
            {
                var defaults = new List<Configuration>
                {
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "General / Kirana", IsActive = true },
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "Pharmacy / Drug", IsActive = true },
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "Hardware", IsActive = true },
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "Electrical / Electronics", IsActive = true },
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "Furniture", IsActive = true },
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "Composite (Both)", IsActive = true }
                };
                _dbContext.Configurations.AddRange(defaults);
                await _dbContext.SaveChangesAsync();
                values = defaults;
            }
            else if (!values.Any() && key == "ProductType")
            {
                var defaults = new List<Configuration>
                {
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Finished", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Goods", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Raw Material", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Sofa/Couch", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Table/Desk", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Chair/Seating", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Bed/Mattress", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Cabinet/Wardrobe", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Generic Item", IsActive = true }
                };
                _dbContext.Configurations.AddRange(defaults);
                await _dbContext.SaveChangesAsync();
                values = defaults;
            }

            return Ok(values);
        }

        [HttpGet("get")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<ActionResult<IEnumerable<Configuration>>> GetAll()
        {
            var all = await _dbContext.Configurations.ToListAsync();
            bool changed = false;

            if (!all.Any(c => c.ConfigKey == "CompanyType"))
            {
                var defaults = new List<Configuration>
                {
                    new Configuration { ConfigKey = "CompanyType", ConfigValue = "Kirana Store", IsActive = true },
                    new Configuration { ConfigKey = "CompanyType", ConfigValue = "Medico/Pharmacy", IsActive = true },
                    new Configuration { ConfigKey = "CompanyType", ConfigValue = "Furniture Store", IsActive = true },
                    new Configuration { ConfigKey = "CompanyType", ConfigValue = "Electric Shop", IsActive = true },
                    new Configuration { ConfigKey = "CompanyType", ConfigValue = "Hardware Shop", IsActive = true }
                };
                _dbContext.Configurations.AddRange(defaults);
                all.AddRange(defaults);
                changed = true;
            }

            if (!all.Any(c => c.ConfigKey == "SupplierType"))
            {
                var defaults = new List<Configuration>
                {
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "General / Kirana", IsActive = true },
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "Pharmacy / Drug", IsActive = true },
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "Hardware", IsActive = true },
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "Electrical / Electronics", IsActive = true },
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "Furniture", IsActive = true },
                    new Configuration { ConfigKey = "SupplierType", ConfigValue = "Composite (Both)", IsActive = true }
                };
                _dbContext.Configurations.AddRange(defaults);
                all.AddRange(defaults);
                changed = true;
            }

            if (!all.Any(c => c.ConfigKey == "ProductType"))
            {
                var defaults = new List<Configuration>
                {
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Finished", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Goods", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Raw Material", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Sofa/Couch", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Table/Desk", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Chair/Seating", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Bed/Mattress", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Cabinet/Wardrobe", IsActive = true },
                    new Configuration { ConfigKey = "ProductType", ConfigValue = "Generic Item", IsActive = true }
                };
                _dbContext.Configurations.AddRange(defaults);
                all.AddRange(defaults);
                changed = true;
            }

            if (changed)
            {
                await _dbContext.SaveChangesAsync();
            }

            return Ok(all);
        }

        [HttpGet("getbyid/{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<ActionResult<Configuration>> GetById(int id)
        {
            var config = await _dbContext.Configurations.FindAsync(id);
            if (config == null) return NotFound();
            return Ok(config);
        }

        [HttpPost("add")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> Add([FromBody] Configuration config)
        {
            _dbContext.Configurations.Add(config);
            await _dbContext.SaveChangesAsync();
            return Ok(config.Id);
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> Update(int id, [FromBody] Configuration config)
        {
            if (id != config.Id) return BadRequest();
            var existing = await _dbContext.Configurations.FindAsync(id);
            if (existing == null) return NotFound();

            existing.ConfigKey = config.ConfigKey;
            existing.ConfigValue = config.ConfigValue;
            existing.IsActive = config.IsActive;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _dbContext.Configurations.AnyAsync(e => e.Id == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> Delete(int id)
        {
            var config = await _dbContext.Configurations.FindAsync(id);
            if (config == null) return NotFound();
            _dbContext.Configurations.Remove(config);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("bulk-delete")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("No IDs provided");
            var configs = await _dbContext.Configurations.Where(c => ids.Contains(c.Id)).ToListAsync();
            _dbContext.Configurations.RemoveRange(configs);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("download-template")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Configurations");
                string[] headers = { "ConfigKey", "ConfigValue", "IsActive" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightCyan;
                }

                // Sample Row
                worksheet.Cell(2, 1).Value = "CompanyType";
                worksheet.Cell(2, 2).Value = "Sample Company Type";
                worksheet.Cell(2, 3).Value = "True";

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Configuration_Template.xlsx");
                }
            }
        }

        [HttpPost("upload-excel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> UploadExcel([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please upload an excel file.");

            int successCount = 0;
            var errors = new List<string>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheets.First();
                    var dataRows = worksheet.RowsUsed().Skip(1); // Skip Header

                    var dbConfigs = await _dbContext.Configurations.ToListAsync();
                    var dbConfigsMap = dbConfigs.GroupBy(c => (c.ConfigKey.ToLower().Trim(), c.ConfigValue.ToLower().Trim()))
                                                .ToDictionary(g => g.Key, g => g.First());

                    foreach (var row in dataRows)
                    {
                        int rowNum = row.RowNumber();
                        try
                        {
                            var key = row.Cell(1).Value.ToString()?.Trim();
                            var val = row.Cell(2).Value.ToString()?.Trim();
                            var activeStatus = row.Cell(3).Value.ToString()?.Trim().ToUpper() ?? "TRUE";
                            bool isActive = activeStatus == "TRUE" || activeStatus == "1" || activeStatus == "ACTIVE" || activeStatus == "YES";

                            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) continue;

                            var mapKey = (key.ToLower().Trim(), val.ToLower().Trim());

                            if (dbConfigsMap.TryGetValue(mapKey, out var existing))
                            {
                                existing.IsActive = isActive;
                                _dbContext.Entry(existing).State = EntityState.Modified;
                            }
                            else
                            {
                                var config = new Configuration
                                {
                                    ConfigKey = key,
                                    ConfigValue = val,
                                    IsActive = isActive
                                };
                                _dbContext.Configurations.Add(config);
                            }
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Row {rowNum}: {ex.Message}");
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                }
            }

            return Ok(new
            {
                message = $"{successCount} Configurations processed successfully.",
                errors = errors
            });
        }
    }
}
